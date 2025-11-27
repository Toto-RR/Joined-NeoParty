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

    public int vueltasTotales = 5;

    // ---------------- AUDIO: Engine ----------------
    [Header("Audio - Motor")]
    public AudioSource engineSource;
    public AudioClip engineLoop;
    [Range(0.2f, 3f)] public float engineMinPitch = 0.8f;
    [Range(0.2f, 3f)] public float engineMaxPitch = 2.0f;
    [Range(0f, 1f)] public float engineBaseVolume = 0.5f;
    [Range(0f, 1f)] public float engineVolumeByInput = 0.3f;
    [Tooltip("Suavizado de pitch/volumen para evitar saltos bruscos")]
    [Range(0.01f, 1f)] public float engineLerp = 0.2f;

    // ---------------- AUDIO: Skid ----------------
    [Header("Audio - Derrape")]
    public AudioSource skidSource;
    public AudioClip skidLoop;
    [Range(0f, 1f)] public float skidMaxVolume = 0.9f;
    [Range(0f, 1f)] public float skidVolumeBySpeed = 0.6f;
    [Tooltip("Si es true, usa el ParticleSystem para detectar derrape")]
    public bool autoDriftFromParticles = false;
    [Range(0f, 1f)] public float skidSpeedPivot = 0.4f;
    [Tooltip("Suavizado de volumen del skid")]
    [Range(0.01f, 1f)] public float skidLerp = 0.2f;
    private bool isDrifting = false;

    // Cache del último input normalizado (0..1)
    private float lastNormalizedInput = 0f;
    // CarController.cs
    public PlayerChoices.PlayerColor PlayerColor => playerColor;
    public int Laps => roadCount; // ya lo incrementas al pasar por "Finish"
    public float Progress01 => GetComponent<UnityEngine.Splines.SplineAnimate>()?.NormalizedTime ?? 0f;
    public float TotalProgress => Laps + Mathf.Clamp01(Progress01); // clave para ordenar


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
        InitAudio(); // AUDIO: prepara fuentes

        if (DebugMode)
        {
            Setup(PlayerChoices.PlayerColor.Azul, FindFirstObjectByType<SplineContainer>(), 2, 100f);
        }
    }

    private void InitAudio()
    {
        // ENGINE
        if (engineSource != null)
        {
            if (engineLoop != null) engineSource.clip = engineLoop;
            engineSource.loop = true;
            engineSource.playOnAwake = false;

            engineSource.pitch = engineMinPitch;
            engineSource.volume = engineBaseVolume;

            if (engineSource.clip != null && !engineSource.isPlaying)
                engineSource.Play();
        }

        // SKID
        if (skidSource != null)
        {
            if (skidLoop != null) skidSource.clip = skidLoop;
            skidSource.loop = true;
            skidSource.playOnAwake = false;
            skidSource.volume = 0f; // empieza silencioso
            // No reproducimos de inicio; lo haremos al entrar en derrape
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

        // AUDIO: actualizar motor con el último input
        UpdateEngineAudio(lastNormalizedInput);

        // AUDIO: derrape auto (opcional)
        if (autoDriftFromParticles)
            AutoUpdateSkidFromParticles();
    }

    public void GetInputSpeed()
    {
        float input = moveAction.ReadValue<float>();
        float normalizedInput = input;

        if (input < 0f)
            normalizedInput = NormalizeInput(input);

        lastNormalizedInput = Mathf.Clamp01(normalizedInput);

        if (normalizedInput > 0f)
        {
            UpdatePathSpeed(normalizedInput * speedMultiplier);
        }
        else
        {
            splineAnimate.Pause();
        }
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
            if (roadCount >= vueltasTotales)
            {
                Debug.Log("Color " + playerColor + " ha terminado!");
                moveAction.Disable();
                Minigame_3.Instance.RegisterPlayerFinish(playerColor);
            }
            else
            {
                Debug.Log("Color " + playerColor + " ha completado una vuelta! (" + roadCount + "/3)");
            }
        }
    }

    // ---------------- AUDIO: Engine ----------------
    private void UpdateEngineAudio(float normalizedInput)
    {
        if (engineSource == null) return;

        float targetPitch = Mathf.Lerp(engineMinPitch, engineMaxPitch, normalizedInput);
        float targetVol = Mathf.Clamp01(engineBaseVolume + engineVolumeByInput * normalizedInput);

        engineSource.pitch = Mathf.Lerp(engineSource.pitch, targetPitch, engineLerp);
        engineSource.volume = Mathf.Lerp(engineSource.volume, targetVol, engineLerp);

        // (Opcional) Si hay un SEF_Equalizer, colorea por gas
        var eq = engineSource != null ? engineSource.GetComponent<SEF_Equalizer>() : null;
        if (eq != null && eq.filterOn)
        {
            // Graves bajan ligeramente, medios estables, agudos suben con el gas
            eq.lowFreq = Mathf.Lerp(1.1f, 0.9f, normalizedInput); // un pelín menos graves a altas rpm
            eq.midFreq = Mathf.Lerp(1.0f, 1.1f, normalizedInput * 0.5f);
            eq.highFreq = Mathf.Lerp(1.0f, 1.4f, normalizedInput); // más brillo con gas
        }
    }

    // ---------------- AUDIO: Skid / Derrape ----------------
    /// <summary>
    /// Activa/Desactiva manualmente el sonido de derrape.
    /// </summary>
    public void SetDrifting(bool drifting)
    {
        if (isDrifting == drifting) return;
        isDrifting = drifting;
        ApplySkidAudio(isDrifting);
    }

    private void AutoUpdateSkidFromParticles()
    {
        if (particleSystem == null) return;

        bool particlesActive = particleSystem.isEmitting || particleSystem.IsAlive(true);
        if (particlesActive != isDrifting)
        {
            isDrifting = particlesActive;
            ApplySkidAudio(isDrifting);
        }

        if (isDrifting && skidSource != null)
        {
            // Volumen del skid según velocidad normalizada del propio spline
            float normSpeed = Mathf.Clamp01(splineAnimate.MaxSpeed / Mathf.Max(1f, speedMultiplier));
            float t = Mathf.InverseLerp(skidSpeedPivot, 1f, normSpeed);
            float target = Mathf.Lerp(0.25f * skidMaxVolume, skidMaxVolume, t * skidVolumeBySpeed + (1f - skidVolumeBySpeed));
            skidSource.volume = Mathf.Lerp(skidSource.volume, target, skidLerp);
        }
    }

    private void ApplySkidAudio(bool active)
    {
        if (skidSource == null) return;

        if (active)
        {
            if (skidSource.clip == null && skidLoop != null)
                skidSource.clip = skidLoop;

            if (!skidSource.isPlaying && skidSource.clip != null)
                skidSource.Play();

            skidSource.volume = Mathf.Max(skidSource.volume, 0.2f * skidMaxVolume);
        }
        else
        {
            StopAllCoroutines();
            StartCoroutine(FadeOutSkid(0.25f));
        }
    }

    private System.Collections.IEnumerator FadeOutSkid(float time)
    {
        if (skidSource == null) yield break;
        float start = skidSource.volume;
        float t = 0f;
        while (t < time)
        {
            t += Time.deltaTime;
            skidSource.volume = Mathf.Lerp(start, 0f, t / time);
            yield return null;
        }
        skidSource.volume = 0f;
        if (skidSource.isPlaying) skidSource.Stop();
    }
}
