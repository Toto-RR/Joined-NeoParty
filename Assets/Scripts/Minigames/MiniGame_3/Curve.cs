using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;

public class Curve : MonoBehaviour
{
    public enum CurveDirection { Left, Right }

    [Header("Curva")]
    public CurveDirection direction = CurveDirection.Left;
    [Tooltip("Velocidad máxima permitida en la curva (misma unidad que MaxSpeed)")]
    public float speedLimit = 100f;
    [Tooltip("Ángulo máximo de giro visual al llegar al límite")]
    public float maxTurnAngle = 30f;
    [Tooltip("Suavizado del giro (0 = sin suavizado)")]
    public float rotationSmooth = 10f;

    [Header("Penalización")]
    [Tooltip("Cuánto baja por segundo el SpeedMultiplier del CarController")]
    public float decelMultiplierPerSec = 250f;
    [Tooltip("Velocidad de giro MÁX del modelo durante penalización (grados/seg)")]
    public float spinSpeed = 360f;
    [Tooltip("Umbral de velocidad para considerar que MaxSpeed ya es 0")]
    public float stopEpsilon = 0.01f;

    // NUEVO: parámetros para que el giro se detenga y se alinee al final
    [Tooltip("Por debajo de esta velocidad angular (deg/s) consideramos que ya no 'gira'")]
    public float spinStopEpsilon = 5f;
    [Tooltip("Suavizado para alinear el modelo a su rotación base al final")]
    public float recoverSmooth = 12f;

    private class CarData
    {
        public Transform root;
        public Transform model;
        public Quaternion baseLocalRot;

        public CarController controller;
        public SplineAnimate spline;

        public float baseMultiplier;   // multiplicador original a restaurar
        public float smoothedYaw;
        public bool inPenalty;

        // NUEVO: velocidad angular actual del spin (deg/s)
        public float spinVel;
    }

    private readonly Dictionary<Transform, CarData> cars = new();

    private void Reset()
    {
        var col = GetComponent<Collider>();
        if (col) col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        var carRoot = FindCarRoot(other.transform);
        if (!carRoot || cars.ContainsKey(carRoot)) return;

        var ctrl = carRoot.GetComponent<CarController>();
        var spline = carRoot.GetComponent<SplineAnimate>();
        if (!ctrl || !spline) return; // necesitamos ambos

        var model = (carRoot.childCount > 0) ? carRoot.GetChild(0) : carRoot;

        cars[carRoot] = new CarData
        {
            root = carRoot,
            model = model,
            baseLocalRot = model.localRotation,
            controller = ctrl,
            spline = spline,
            baseMultiplier = ctrl.SpeedMultiplier, // guardamos el multiplicador inicial
            smoothedYaw = 0f,
            inPenalty = false,
            spinVel = 0f // NUEVO
        };

        if (cars.TryGetValue(carRoot, out var d) && d.controller)
        {
            d.controller.SetDrifting(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        var carRoot = FindCarRoot(other.transform);
        if (!carRoot) return;

        if (cars.TryGetValue(carRoot, out var d))
        {
            // Si aún está penalizando, devuelve el multiplicador
            if (d.inPenalty && d.controller)
                d.controller.SpeedMultiplier = Mathf.Max(d.controller.SpeedMultiplier, d.baseMultiplier);

            d.model.localRotation = d.baseLocalRot;
            d.inPenalty = false;
            d.spinVel = 0f; // NUEVO

            if (d.controller)
                d.controller.SetDrifting(false);

            cars.Remove(carRoot);
        }
    }

    private void Update()
    {
        if (cars.Count == 0) return;

        var keys = new List<Transform>(cars.Keys);
        foreach (var tr in keys)
        {
            if (!tr) { cars.Remove(tr); continue; }
            var d = cars[tr];
            if (!d.controller || !d.spline) { cars.Remove(tr); continue; }

            float currentSpeed = Mathf.Max(0f, d.spline.MaxSpeed);
            float factor = Mathf.Clamp01(speedLimit > 0f ? currentSpeed / speedLimit : 0f);
            float targetYaw = maxTurnAngle * factor * ((direction == CurveDirection.Left) ? -1f : 1f);

            if (!d.inPenalty)
            {
                // Giro visual normal
                if (rotationSmooth > 0f)
                {
                    d.smoothedYaw = Mathf.Lerp(d.smoothedYaw, targetYaw, 1f - Mathf.Exp(-rotationSmooth * Time.deltaTime));
                    d.model.localRotation = Quaternion.Euler(0f, d.smoothedYaw, 0f);
                }
                else
                {
                    d.model.localRotation = Quaternion.Euler(0f, targetYaw, 0f);
                }

                if (currentSpeed > speedLimit)
                {
                    d.inPenalty = true;
                    d.spinVel = spinSpeed;
                }
            }
            else
            {
                // --- Penalización ---
                // El giro se escala con el ratio del multiplicador (baja a cero junto con la frenada)
                float ratio = (d.baseMultiplier > 0f) ? Mathf.Clamp01(d.controller.SpeedMultiplier / d.baseMultiplier) : 0f;
                d.spinVel = spinSpeed * ratio;

                // Dentro del bloque de penalización (else { ... }) en Update()
                float dirSign = (direction == CurveDirection.Left) ? -1f : 1f;

                // El giro escala con el ratio y respeta la dirección de la curva
                d.spinVel = spinSpeed * ratio;

                // Aplicar rotación acumulada este frame usando el signo de la curva
                if (d.spinVel > 0f)
                    d.model.Rotate(0f, dirSign * d.spinVel * Time.deltaTime, 0f, Space.Self);


                // Alinear suavemente hacia la rotación base cuando el spin ya es muy bajo
                if (d.spinVel <= spinStopEpsilon)
                {
                    d.model.localRotation = Quaternion.Slerp(
                        d.model.localRotation,
                        d.baseLocalRot,
                        1f - Mathf.Exp(-recoverSmooth * Time.deltaTime)
                    );
                }

                // Salir de penalización cuando la velocidad efectiva ya es casi 0
                if (currentSpeed <= stopEpsilon)
                {
                    d.inPenalty = false;
                    d.model.localRotation = d.baseLocalRot; // queda perfecto
                    d.smoothedYaw = 0f;
                    d.spinVel = 0f;

                    // Restaurar el multiplicador original para recuperar el control
                    d.controller.SpeedMultiplier = d.baseMultiplier;
                }
            }
        }
    }

    private void LateUpdate()
    {
        // Reducimos el SpeedMultiplier DESPUÉS de que CarController lo haya usado en Update()
        foreach (var kv in cars)
        {
            var d = kv.Value;
            if (!d.inPenalty || d.controller == null) continue;

            float m = d.controller.SpeedMultiplier;
            if (m > 0f)
            {
                m = Mathf.Max(0f, m - decelMultiplierPerSec * Time.deltaTime);
                d.controller.SpeedMultiplier = m;
            }
        }
    }

    private Transform FindCarRoot(Transform t)
    {
        Transform cur = t;
        while (cur != null)
        {
            if (cur.CompareTag("Player")) return cur;
            if (cur.GetComponent<CarController>() != null) return cur;
            cur = cur.parent;
        }
        return null;
    }
}
