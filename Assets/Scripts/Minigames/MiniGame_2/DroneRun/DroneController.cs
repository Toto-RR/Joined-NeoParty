using System;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class DroneController : MonoBehaviour
{
    [Header("Movimiento (plano XY)")]
    public float maxSpeed = 12f;         
    public float acceleration = 40f;     
    public float linearDrag = 10f;       // freno pasivo

    [Header("Límites rectangulares (mundo)")]
    public bool useWorldBounds = true;          // activar/desactivar clamp
    public Vector2 boundsCenter;                // centro del rectángulo en mundo
    public Vector2 boundsHalfExtents;           // semiejes (ancho/2, alto/2)
    public float boundsPadding = 0.3f;          // margen interior para no “pegarse” al borde

    [Header("Rotacion visual (tilt)")]
    public Transform visual;             // hijo grafico opcional
    public float maxTiltDeg = 15f;       // inclinacion visual
    public float tiltSmooth = 10f;
    public float maxTiltSpeedDegPerSec = 180f; // límite de velocidad angular

    private int score = 100;
    private PlayerInput playerInput;
    private InputAction moveAction;
    private InputAction readyAction;
    private Renderer droneRenderer;
    private Color originalColor;
    public bool CalibrationReady { get; private set; } = false;
    public event Action<DroneController, bool> OnCalibrationToggled;
    private bool readyArmed = false;

    private string schema;
    private PlayerChoices.PlayerColor playerColor;

    private AudioSource audioSource;
    private Rigidbody rb;

    public PlayerChoices.PlayerColor pColor => playerColor;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.constraints =
            RigidbodyConstraints.FreezePositionZ |      // movimiento solo en X/Y
            RigidbodyConstraints.FreezeRotation;        // solo rotacion "visual"
        rb.linearDamping = linearDrag;
        rb.useGravity = false;

        droneRenderer = GetComponentInChildren<Renderer>();
        originalColor = droneRenderer.material.color;

        this.enabled = false;
    }

    public void Init(string schema_, PlayerChoices.PlayerColor color, Vector2 halfextents, Vector2 centerPos)
    {
        playerColor = color;
        schema = schema_;
        playerInput = GetComponent<PlayerInput>();

        boundsHalfExtents = halfextents;
        boundsCenter = centerPos;
        useWorldBounds = true;

        audioSource = GetComponentInChildren<AudioSource>();
        //audioSource.Play();
    }

    public void SetBounds(Vector2 centerPos, Vector2 halfextents)
    {
        boundsCenter = centerPos;
        boundsHalfExtents = halfextents;
        useWorldBounds = true;
    }

    public void BeginCalibrationInputGate()
    {
        // Llamado al entrar en calibración
        readyArmed = false;        // exige una suelta antes del primer toggle
                                   // Opcional: limpiar estado para evitar “fantasmas”
        readyAction?.Disable();
        readyAction?.Enable();
    }

    void OnEnable()
    {
        moveAction = playerInput.actions["Move"];
        moveAction?.Enable();

        readyAction = playerInput.actions.FindAction("Ready", throwIfNotFound: false);
        if (readyAction != null)
        {
            readyAction.Enable();
            // Se arma cuando el jugador SUELTA el gatillo/botón por primera vez
            readyAction.canceled += OnReadyCanceled;
            // Solo permite toggle si está armado
            readyAction.performed += OnReadyPerformed;
        }
    }
    void OnDisable()
    {
        moveAction?.Disable();
        if (readyAction != null)
        {
            readyAction.canceled -= OnReadyCanceled;
            readyAction.performed -= OnReadyPerformed;
            readyAction.Disable();
        }
    }

    void FixedUpdate()
    {
        Vector2 input = moveAction.ReadValue<Vector2>();
        if (input.sqrMagnitude < 0.0025f) input = Vector2.zero;

        // Física de movimiento (igual que ya tienes)...
        Vector3 targetVel = new Vector3(input.x, input.y, 0f) * maxSpeed;
        Vector3 vel = rb.linearVelocity;
        rb.linearVelocity = Vector3.MoveTowards(vel, targetVel, acceleration * Time.fixedDeltaTime);

        // ----- TILT SUAVE BASADO EN VELOCIDAD -----
        if (visual)
        {
            Vector3 v = rb.linearVelocity;
            float speed01 = Mathf.Clamp01(v.magnitude / Mathf.Max(0.0001f, maxSpeed));
            Vector2 dir = new Vector2(v.x, v.y).normalized;

            // roll por X, pitch por Y (ligado a la dirección real de desplazamiento)
            float roll = -dir.x * maxTiltDeg * speed01;
            float pitch = -dir.y * maxTiltDeg * speed01;

            Quaternion current = visual.localRotation;
            Quaternion target = Quaternion.Euler(pitch, 0f, roll);
            visual.localRotation = Quaternion.RotateTowards(
                current, target,
                maxTiltSpeedDegPerSec * Time.fixedDeltaTime
            );
        }

        ClampToWorldRect();
    }


    void ClampToWorldRect()
    {
        if (!useWorldBounds) return;

        float minX = boundsCenter.x - boundsHalfExtents.x + boundsPadding;
        float maxX = boundsCenter.x + boundsHalfExtents.x - boundsPadding;
        float minY = boundsCenter.y - boundsHalfExtents.y + boundsPadding;
        float maxY = boundsCenter.y + boundsHalfExtents.y - boundsPadding;

        Vector3 p = rb.position;
        Vector3 v = rb.linearVelocity;

        bool hitLeft = p.x < minX;
        bool hitRight = p.x > maxX;
        bool hitBottom = p.y < minY;
        bool hitTop = p.y > maxY;

        // clamp posición
        float newX = Mathf.Clamp(p.x, minX, maxX);
        float newY = Mathf.Clamp(p.y, minY, maxY);

        // anular solo la componente de velocidad que empuja hacia fuera
        if (hitLeft && v.x < 0f) v.x = 0f;
        if (hitRight && v.x > 0f) v.x = 0f;
        if (hitBottom && v.y < 0f) v.y = 0f;
        if (hitTop && v.y > 0f) v.y = 0f;

        rb.position = new Vector3(newX, newY, p.z);
        rb.linearVelocity = v;
    }
    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("SafeZone"))
        {
            var lw = other.GetComponentInParent<LaserWall>();
            if (lw && lw.colorsMode)
            {
                if (this.pColor != lw.gateColor)
                {
                    // Pasó por puerta de otro color => penaliza
                    Penalize(" cruzó gate de otro color");
                }
                // Si es su color: no pasa nada (si quieres recompensa, súmala aquí)
            }
            else
            {
                // SafeZone clásica: sin efecto de puntos
            }
        }
        else if (other.gameObject.layer == LayerMask.NameToLayer("Wall"))
        {
            var lw = other.gameObject.GetComponentInParent<LaserWall>();
            if (lw && lw.colorsMode)
            {
                // En modo colores: si chocas con tu propia pared, penaliza; si no, nada
                if (this.pColor == lw.gateColor)
                    Penalize(" chocó con la pared en ColorsMode");
            }
            else
            {
                // Modo clásico: chocar con pared penaliza siempre
                Penalize(" choque con obstáculo");
            }
        }
        else if (other.gameObject.layer == LayerMask.NameToLayer("Finish"))
        {
            audioSource.Stop();
            Minigame_2.Instance.PlayerFinished(pColor, score);
        }
    }

    private void ResetColor()
    {
        droneRenderer.material.color = originalColor;
    }

    void Penalize(string reason)
    {
        Debug.Log("Penalty: " + reason);
        SoundManager.PlayFX(4); // Hit_3
        droneRenderer.material.color = Color.red;
        Invoke(nameof(ResetColor), 0.5f);
        score--;
    }

    private void OnReadyCanceled(InputAction.CallbackContext _)
    {
        readyArmed = true;
    }

    private void OnReadyPerformed(InputAction.CallbackContext _)
    {
        if (!readyArmed) return; 
        ToggleCalibrationReady();
    }

    public void ToggleCalibrationReady()
    {
        CalibrationReady = !CalibrationReady;
        SoundManager.PlayFX(CalibrationReady ? 7 : 6);
        OnCalibrationToggled?.Invoke(this, CalibrationReady);
    }
}
