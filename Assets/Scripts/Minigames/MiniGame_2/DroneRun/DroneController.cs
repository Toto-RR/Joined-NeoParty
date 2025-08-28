using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class DroneController : MonoBehaviour
{
    [Header("Movimiento (plano XY)")]
    public float maxSpeed = 12f;         // u/s
    public float acceleration = 40f;     // u/s^2
    public float linearDrag = 10f;       // freno pasivo

    [Header("Límites rectangulares (mundo)")]
    public bool useWorldBounds = true;          // activar/desactivar clamp
    public Vector2 boundsCenter;                // centro del rectángulo en mundo
    public Vector2 boundsHalfExtents;           // semiejes (ancho/2, alto/2)
    public float boundsPadding = 0.3f;          // margen interior para no “pegarse” al borde

    [Header("Rotacion visual (tilt)")]
    public Transform visual;             // hijo grafico opcional
    public float maxTiltDeg = 15f;       // inclinaci�n visual
    public float tiltSmooth = 10f;

    private int score = 100;
    private PlayerInput playerInput;
    private InputAction moveAction;
    private Renderer droneRenderer;
    private Color originalColor;

    private string schema;
    private PlayerChoices.PlayerColor playerColor;

    private Rigidbody rb;

    public PlayerChoices.PlayerColor pColor => playerColor;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.constraints =
            RigidbodyConstraints.FreezePositionZ |      // solo nos movemos en X/Y
            RigidbodyConstraints.FreezeRotation;        // rotacion la controla "visual"
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

        Debug.Log("CENTER POS:" + boundsCenter + " EXTENTS: " + boundsHalfExtents);
        Debug.Log("POSITION: " + transform.position);
    }

    public void SetBounds(Vector2 centerPos, Vector2 halfextents)
    {
        boundsCenter = centerPos;
        boundsHalfExtents = halfextents;
        useWorldBounds = true;
    }

    void OnEnable() 
    { 
        moveAction = playerInput.actions["Move"];
        moveAction?.Enable();
    }

    void OnDisable() 
    {
        moveAction?.Disable();    
    }

    void FixedUpdate()
    {
        Vector2 input = moveAction.ReadValue<Vector2>();
        // Opcional: deadzone extra
        if (input.sqrMagnitude < 0.0025f) input = Vector2.zero;

        // Deseamos velocidad objetivo en el plano
        Vector3 targetVel = new Vector3(input.x, input.y, 0f) * maxSpeed;
        Vector3 vel = rb.linearVelocity;

        // Aceleraci�n hacia la velocidad objetivo (cr�tico para chocar bien con paredes)
        Vector3 velDelta = targetVel - vel;
        Vector3 accel = Vector3.ClampMagnitude(velDelta / Time.fixedDeltaTime, acceleration);
        rb.AddForce(accel, ForceMode.Acceleration);

        // Tilt visual (roll por X, pitch por Y) � solo el hijo gr�fico
        if (visual)
        {
            float roll = -input.x * maxTiltDeg; // inclina sobre Z para banca lateral
            float pitch = -input.y * maxTiltDeg; // inclina sobre X para subir/bajar
            Quaternion targetRot = Quaternion.Euler(pitch, 0, roll);
            visual.localRotation = Quaternion.Slerp(visual.localRotation, targetRot, 1f - Mathf.Exp(-tiltSmooth * Time.fixedDeltaTime));
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
        droneRenderer.material.color = Color.red;
        Invoke(nameof(ResetColor), 0.5f);
        score--;
    }

}
