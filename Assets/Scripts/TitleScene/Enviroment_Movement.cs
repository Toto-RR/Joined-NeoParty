using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;
using System.Reflection; // para intentar fijar NormalizedTime si tu versión de Splines lo soporta

public class Enviroment_Movement : MonoBehaviour
{
    [System.Serializable]
    public class CustomSpline
    {
        public bool grounded;                 // true = suelo, false = aéreo
        public SplineContainer spline;        // contenedor de la(s) spline(s)
    }

    [System.Serializable]
    public class CustomCar
    {
        public bool grounded;                 // true = coche de suelo, false = volador
        public GameObject car;                // PREFAB del coche
    }

    [Header("Listas")]
    public List<Material> materials;          // materiales aleatorios
    public List<CustomCar> cars;              // prefabs de coches
    public List<CustomSpline> splines;        // splines disponibles (marca grounded)

    [Header("Spawning")]
    public int maxAlive = 40;                 // límite de coches simultáneos
    public Vector2 spawnIntervalRange = new Vector2(0.4f, 1.2f); // intervalo aleatorio
    public bool startOnPlay = true;

    [Header("Velocidad (realmente Duración en SplineAnimate)")]
    public Vector2 groundDurationRange = new Vector2(8f, 14f); // s para recorrer la spline
    public Vector2 airDurationRange = new Vector2(8f, 14f);

    int alive;
    Coroutine loop;

    void Start()
    {
        if (startOnPlay) loop = StartCoroutine(SpawnLoop());
    }

    IEnumerator SpawnLoop()
    {
        while (true)
        {
            if (alive < maxAlive)
                TrySpawnOne();

            // jitter de spawn para “ciudad viva”
            float wait = Random.Range(spawnIntervalRange.x, spawnIntervalRange.y);
            yield return new WaitForSeconds(wait);
        }
    }

    // Intenta spawnear un coche (si hay spline y prefab compatibles)
    void TrySpawnOne()
    {
        if (splines == null || splines.Count == 0 || cars == null || cars.Count == 0)
            return;

        // Elige una spline aleatoria
        CustomSpline lane = splines[Random.Range(0, splines.Count)];
        if (lane == null || lane.spline == null || lane.spline.Splines.Count == 0)
            return;

        bool wantGrounded = lane.grounded;

        // Elige un coche compatible
        var candidates = cars.FindAll(c => c != null && c.car != null && c.grounded == wantGrounded);
        if (candidates.Count == 0) return;
        CustomCar choice = candidates[Random.Range(0, candidates.Count)];

        // Duración aleatoria (equivale a “velocidad” inversa en SplineAnimate)
        float duration = wantGrounded
            ? Random.Range(groundDurationRange.x, groundDurationRange.y)
            : Random.Range(airDurationRange.x, airDurationRange.y);

        // Material aleatorio
        if (materials != null && materials.Count > 0)
        {
            var m = materials[Random.Range(0, materials.Count)];
                choice.car.GetComponentInChildren<Renderer>().material = m;
        }

        // Instancia y configura
        GameObject go = Instantiate(choice.car, lane.spline.gameObject.transform.position, lane.spline.gameObject.transform.rotation);
        alive++;

        // Asigna contenedor y lanza
        var anim = go.GetComponent<SplineAnimate>();
        if (!anim) anim = go.AddComponent<SplineAnimate>();
        SetupAndPlaySplineAnimate(anim, lane.spline, duration);
    }

    void SetupAndPlaySplineAnimate(SplineAnimate anim, SplineContainer container, float duration)
    {
        anim.Container = container;
        anim.Duration = Mathf.Max(0.01f, duration);
        anim.Loop = SplineAnimate.LoopMode.Once;

        anim.Play();
        
        // Cuando termine el recorrido, destruir y respawnear
        StartCoroutine(WaitForSplineEnd(anim.gameObject, anim));
    }

    IEnumerator WaitForSplineEnd(GameObject go, SplineAnimate anim)
    {
        // Espera hasta que haya llegado al final del recorrido
        while (anim.NormalizedTime < 1f)
        {
            yield return null;
        }

        if (go) Destroy(go);
        alive = Mathf.Max(0, alive - 1);

        if (alive < maxAlive)
            TrySpawnOne();
    }
}
