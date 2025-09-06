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

    public new Light light;
    public new ParticleSystem particleSystem;

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

        carModel = transform.GetChild(0).gameObject;
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

        if (light != null)
        {
            light.color = PlayerChoices.GetColorRGBA(playerColor);
        }

        if (particleSystem != null)
        {
            var main = particleSystem.main;
            main.startColor = PlayerChoices.GetColorRGBA(playerColor);
        }

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

        float lateral;
        if (index == 0)
            lateral = -1.25f;
        else if (index == 1)
            lateral = 1.30f;
        else if (index == 2)
            lateral = -3.6f;
        else
            lateral = 3.84f;
        

        splineAnimate.StartOffset = 0.01f;
        carModel.transform.SetLocalPositionAndRotation(new Vector3(lateral, 0f, 0f), Quaternion.identity);

        splineAnimate.Restart(false);
    }

    private void Update()
    {
        GetInputSpeed();

        if (splineAnimate.MaxSpeed <= 0) splineAnimate.Pause();
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


    public float SpeedMultiplier
    {
        get => speedMultiplier;
        set => speedMultiplier = Mathf.Max(0f, value);
    }


    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Finish"))
        {
            roadCount++;
            if(roadCount >= 3)
            {
                Debug.Log("Color " + playerColor + " ha terminado!");
                Minigame_3.Instance.RegisterPlayerFinish(playerColor);
            }
            else
            {
                Debug.Log("Color " + playerColor + " ha completado una vuelta! (" + roadCount + "/3)");
            }
        }
    }
}

