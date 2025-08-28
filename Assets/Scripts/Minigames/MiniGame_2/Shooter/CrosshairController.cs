using System.Collections;
using System.Drawing;
using System.Reflection;
using System.Xml.Serialization;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Color = UnityEngine.Color;

[RequireComponent(typeof(RectTransform))]
public class CrosshairController : MonoBehaviour
{
    // --- AIM ---
    [Header("Movimiento")]
    [Tooltip("Velocidad en píxeles/segundo sobre el Canvas.")]
    public float speed = 900f;
    [Tooltip("Margen para no salir de la pantalla.")]
    public Vector2 padding = new(16, 16);

    [Header("Refs UI")]
    public Image crosshairImage;

    private Vector2 currentPointer;
    public float smoothSpeed = 10f;

    // NUEVOS: usa speed (px/seg) y quita pointerRange de la ecuación
    private Vector2 filteredInput;

    private string schema;
    private RectTransform rt;
    public Canvas canvas;
    public Camera cam;

    // Input por-jugador
    private PlayerInput playerInput;
    private InputAction moveAction;

    // --- SHOOT ---
    [Header("Disparo")]
    [Tooltip("Capas que pueden recibir impactos (enemigos/targets).")]
    public LayerMask hitMask;                   // Asigna, por ejemplo, Layer 'Target'
    [Tooltip("Distancia máxima del raycast.")]
    public float maxDistance = 200f;
    [Tooltip("Tiempo mínimo entre disparos (segundos).")]
    public float fireCooldown = 0.1f;
    [Tooltip("Daño por impacto (si usas EnemyTarget con vidas).")]
    public int damage = 1;

    private float nextFireTime = 0f;
    private InputAction shootAction;

    private PlayerChoices.PlayerColor playerColor;

    /// Llama a esto al instanciar el crosshair de cada jugador
    public void Init(string schema_, PlayerChoices.PlayerColor color)
    {
        playerColor = color;
        schema = schema_;
        speed = schema_ switch
        {
            "Keyboard&Mouse" => 100f,
            "Gamepad" => 1500f,
            "Joystick" => 2500f,
            _ => 2000f,
        };

        rt = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();

        playerInput = GetComponent<PlayerInput>();
    }

    private void Awake()
    {
        this.enabled = false;
    }

    private void OnEnable()
    {
        centerAnchored = rt.anchoredPosition;

        moveAction = playerInput.actions["Aim"];
        shootAction = playerInput.actions["Shoot"];

        moveAction?.Enable();
        shootAction?.Enable();

        StartCoroutine(Calibrate());
    }

    private void OnDisable()
    {
        shootAction?.Disable();
    }

    private void OnDestroy()
    {
        // Limpia el PlayerInput creado para este crosshair
        if (playerInput) Destroy(playerInput.gameObject);
    }

    [Header("Arduino Comfort Tuning")]
    public float deadzone = 0.06f;         // zona muerta circular
    public float dzHysteresis = 0.02f;     // histéresis para no entrar/salir haciendo clics
    public float expo = 0.55f;             // curva de respuesta (0.45–0.7)
    public float minSpeed = 180f;          // velocidad mínima cuando sales de DZ
    public float maxSpeed = 1800f;         // velocidad máxima a tope
    public float accel = 9000f;            // límite de aceleración px/s^2
    public float posSmoothing = 0f;        // 0–10: ligero suavizado de posición (opcional)
    private Vector2 rawCenter;             // lo llenas con tu Calibrate()
    private Vector2 vel;                   // velocidad actual px/s
    private bool wasOutside;               // para histéresis de DZ
    // Añade estos campos al script
    public bool useAbsoluteAimForArduino = true;
    public Vector2 maxOffsetPixels = new Vector2(600f, 350f); // radio desde el centro
    private Vector2 centerAnchored;   // centro visual (se toma en OnEnable)

    // Llama al empezar la ronda cuando el puntero está quieto (p. ej. tras el countdown)
    public IEnumerator Calibrate(float seconds = 0.3f)
    {
        Vector2 sum = Vector2.zero; int n = 0; float end = Time.unscaledTime + seconds;
        while (Time.unscaledTime < end || n < 10) { sum += moveAction.ReadValue<Vector2>(); n++; yield return null; }
        rawCenter = (n > 0) ? (sum / n) : Vector2.zero;
    }

    private void Update()
    {
        if (!moveAction.enabled) return;

        Vector2 rawInput = moveAction.ReadValue<Vector2>(); // [-1..1] o rango HID

        // --- MODO ABSOLUTO PARA ARDUINO/HID ---
        if (schema != "Keyboard&Mouse") // Arduino/HID aquí
        {
            Vector2 raw = moveAction.ReadValue<Vector2>();   // Vector2 crudo del dispositivo
            Vector2 off = raw - rawCenter;                   // offset desde el centro calibrado

            // 1) Deadzone circular con histéresis
            float m = off.magnitude;
            float dzEnter = deadzone;
            float dzExit = Mathf.Max(0f, deadzone - dzHysteresis);
            bool outside = wasOutside ? (m > dzExit) : (m > dzEnter);
            wasOutside = outside;

            if (!outside)
            {
                // Dentro de DZ: para en seco
                vel = Vector2.zero;
            }
            else
            {
                // 2) Dirección + magnitud normalizada [0..1] fuera de DZ
                Vector2 dir = off.normalized;
                float norm = Mathf.InverseLerp(deadzone, 1f, m);  // cuánto fuera de la DZ

                // 3) Curva de respuesta (expo): fino cerca del centro, punta arriba al final
                float gain = Mathf.Pow(norm, expo);

                // 4) Velocidad objetivo con piso mínimo (para que no “se frene” al borde de la DZ)
                float targetSpeed = Mathf.Lerp(minSpeed, maxSpeed, gain);
                Vector2 targetVel = dir * targetSpeed;

                // 5) Límite de aceleración (quita tirones)
                float dt = Time.unscaledDeltaTime;
                vel.x = Mathf.MoveTowards(vel.x, targetVel.x, accel * dt);
                vel.y = Mathf.MoveTowards(vel.y, targetVel.y, accel * dt);
            }

            // 6) Integración + (opcional) pequeño suavizado de posición
            Vector2 nextPos = rt.anchoredPosition + vel * Time.unscaledDeltaTime;
            if (posSmoothing > 0f)
            {
                float a = 1f - Mathf.Exp(-posSmoothing * Time.unscaledDeltaTime);
                rt.anchoredPosition = Vector2.Lerp(rt.anchoredPosition, nextPos, a);
            }
            else
            {
                rt.anchoredPosition = nextPos;
            }

            ClampToScreen();
        }
        else
        {
            // --- MODO DELTA (mouse/teclado) como ya tenías ---
            float t = 1f - Mathf.Exp(-smoothSpeed * Time.unscaledDeltaTime);
            filteredInput = Vector2.Lerp(filteredInput, rawInput, t);

            Vector2 delta = filteredInput * speed * Time.unscaledDeltaTime;
            currentPointer += delta;

            rt.anchoredPosition = currentPointer;
            ClampToScreen();
        }

        if (shootAction != null && shootAction.WasPerformedThisFrame())
            TryFire();
    }

    private void ClampToScreen()
    {
        var canvasRT = canvas.transform as RectTransform;
        Vector2 half = (canvasRT.rect.size * 0.5f) - padding;

        Vector2 p = rt.anchoredPosition;
        p.x = Mathf.Clamp(p.x, -half.x, half.x);
        p.y = Mathf.Clamp(p.y, -half.y, half.y);

        rt.anchoredPosition = p;
        currentPointer = p; // <- mantener el acumulador dentro de los límites
    }

    private void TryFire()
    {
        if (Time.time < nextFireTime) return;
        nextFireTime = Time.time + fireCooldown;

        Debug.Log("SHOOT");

        if (!rt)
        {
            Debug.LogWarning("[Crosshair] RectTransform no asignado.");
            return;
        }
        if (!cam) cam = Camera.main;
        if (!cam)
        {
            Debug.LogWarning("[Crosshair] No hay cámara asignada ni Camera.main.");
            return;
        }

        Vector3 screenPoint = SafeGetCrosshairScreenPoint();
        Ray ray = cam.ScreenPointToRay(screenPoint);

        if (Physics.Raycast(ray, out var hit, maxDistance, hitMask, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider.TryGetComponent(out Enemy enemy))
                enemy.Hit(playerColor);
            Debug.DrawLine(ray.origin, hit.point, Color.green, 0.1f);
        }
        else
        {
            Debug.DrawRay(ray.origin, ray.direction * maxDistance, Color.red, 0.1f);
        }
    }
    private Vector3 SafeGetCrosshairScreenPoint()
    {
        // Si hay canvas y es Overlay, la posición del RT ya está en pantalla
        if (canvas && canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            return rt.position;

        // Si hay canvas con cámara, úsala; si no, usa la de gameplay
        Camera uiCam = (canvas && canvas.worldCamera) ? canvas.worldCamera : cam;
        return RectTransformUtility.WorldToScreenPoint(uiCam, rt.position);
    }
}
