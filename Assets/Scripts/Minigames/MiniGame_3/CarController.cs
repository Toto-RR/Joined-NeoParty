using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Splines;

[RequireComponent(typeof(SplineAnimate))]
public class CarController : MonoBehaviour
{
    private GameObject carModel;
    private SplineAnimate splineAnimate;
    private PlayerChoices.PlayerColor playerColor;
    private PlayerInput playerInput;
    private InputAction moveAction;

    float speedMultiplier = 100f;
    private int roadIndex = 0;

    private int roadCount = 0; // numero de vueltas completas

    public bool DebugMode = false;

    private void Awake()
    {
        splineAnimate = GetComponent<SplineAnimate>();
        if (splineAnimate == null)
        {
            Debug.LogError("SplineAnimate component not found on the GameObject.");
            return;
        }

        playerInput = GetComponent<PlayerInput>();

        carModel = transform.GetChild(0).gameObject; // Asume que el modelo es el primer hijo

        //splineAnimate.MaxSpeed = 0; // Velocidad inicial
        //splineAnimate.enabled = true;

        splineAnimate.PlayOnAwake = false;

        if (!DebugMode) this.enabled = false; // Desactiva el script hasta que se inicialice
    }

    private void Start()
    {
        if (DebugMode)
        {
            Setup(PlayerChoices.PlayerColor.Azul, FindFirstObjectByType<SplineContainer>(), 2, 100f);
        }
    }

    public void Setup(PlayerChoices.PlayerColor color, SplineContainer sContainer, int _roadIndex, float sMult)
    {
        playerColor = color;
        splineAnimate.Container = sContainer;
        roadIndex = _roadIndex;
        speedMultiplier = sMult;

        SetPositionByIndex(roadIndex);
    }

    private void OnEnable()
    {
        moveAction = playerInput.actions["Move"];
        moveAction?.Enable();
    }

    public void SetPositionByIndex(int index)
    {
        if (index < 0 || index > 3)
        {
            Debug.LogWarning("Indice fuera de rango! Solo hay 4 carriles, de 0 a 3");
            return;
        }

        // Offset de salida (a lo largo) y desplazamiento lateral (visual)
        float startOffset = 0.02f * index;
        float lateral = 2f * index;

        splineAnimate.StartOffset = startOffset;
        carModel.transform.SetLocalPositionAndRotation(new Vector3(lateral, 0f, 0f), Quaternion.identity);

        splineAnimate.Restart(false);
    }

    private void Update()
    {
        GetInputSpeed();
    }

    public void GetInputSpeed()
    {
        float input = moveAction.ReadValue<float>();
        //Debug.Log("Input: " + input);
        float normalizedInput = input;
        
        if (input < 0f)
             normalizedInput = NormalizeInput(input);

        //Debug.Log("Normalized: " + normalizedInput);

        if (normalizedInput > 0f)
        {
            UpdatePathSpeed(normalizedInput * speedMultiplier);
        }
        else
            splineAnimate.Pause();
    }

    private void UpdatePathSpeed(float newSpeed)
    {
        if (!splineAnimate.IsPlaying) splineAnimate.Play();

        float prevProgress = splineAnimate.NormalizedTime;
        splineAnimate.MaxSpeed = newSpeed;
        splineAnimate.NormalizedTime = prevProgress;
    }

    private float NormalizeInput(float input)
    {
        return Mathf.InverseLerp(-1f, 1f, input);
    }
}
