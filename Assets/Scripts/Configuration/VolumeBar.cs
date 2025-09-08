using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using TMPro;

/// <summary>
/// VolumeBar “todo en uno”:
/// - 10 imágenes que se llenan por opacidad (0..1 -> 0..10 pasos).
/// - Empuja/lee un parámetro EXPUESTO del AudioMixer (dB).
/// - Implementa Modo Volumen: enterButton -> esta barra pasa a ser el provider del MenuInputRouter.
///     Azul    -> +step
///     Naranja -> -step
///     Amarillo/Back -> salir (restaura provider del panel)
/// - Lee el valor actual del Mixer en OnEnable para que la barra refleje el volumen ya fijado (p.ej. 0.4).
/// - Botones “puente” ocultos solo en runtime (no serializados) => sin problemas en el Editor.
/// - (Opcional) label TMP que aumenta de tamaño al activar.
/// </summary>
[DisallowMultipleComponent]
public class VolumeBar : MonoBehaviour, IMenuActionsProvider
{
    [Header("Imágenes (10 pasos) de izquierda a derecha")]
    [SerializeField] private Image[] steps = new Image[10];

    [Header("Opacidad por paso")]
    [Range(0f, 1f)][SerializeField] private float filledAlpha = 1f;
    [Range(0f, 1f)][SerializeField] private float emptyAlpha = 0.15f;

    [Header("Paso por pulsación (0..1)")]
    [Range(0.01f, 1f)][SerializeField] private float step = 0.1f; // 10%

    public enum VolumeChannel { Music, SFX }
    [Header("Canal que controla")]
    [SerializeField] private VolumeChannel channel = VolumeChannel.Music;

    [Header("Interacción (modo volumen)")]
    [Tooltip("Botón (puede ser invisible) que activa el modo volumen para esta barra.")]
    [SerializeField] private Button enterButton;
    [Tooltip("Provider del panel (CanvasActionsProvider) para restaurar al salir.")]
    [SerializeField] private CanvasActionsProvider panelProvider;

    [Header("Hints opcionales durante el modo volumen")]
    [SerializeField] private GameObject hintUpBlue;     // “Azul sube”
    [SerializeField] private GameObject hintDownOrange; // “Naranja baja”

    [Header("Lectura al abrir Opciones")]
    [Tooltip("Si está activo, al habilitar la barra leerá el valor actual del Mixer y actualizará la UI sin empujar de vuelta.")]
    [SerializeField] private bool readFromMixerOnEnable = true;

    [Header("Texto asociado (opcional)")]
    [SerializeField] private TextMeshProUGUI label;       // Texto que crece al activar
    [SerializeField] private float selectedScale = 1.2f;  // Factor de escala del TMP al activar
    private Vector3 labelBaseScale;

    // Valor interno normalizado 0..1
    [SerializeField, Range(0f, 1f)]
    private float value01 = 0.5f;

    // --- Estado interno ---
    private bool _suppressPush; // evita empujar al Mixer al sincronizar desde él

    // --- Botones puente para el router (solo runtime; no serializar) ---
    private Button blueHidden;
    private Button orangeHidden;
    private Button yellowHidden;

    // =============== IMenuActionsProvider (modo volumen) ===============
    public Button BlueButton => blueHidden;
    public Button OrangeButton => orangeHidden;
    public Button GreenButton => null;          // no usado en este modo
    public Button YellowButton => yellowHidden;
    public Button BackButton => yellowHidden;  // Amarillo funciona como Back/Salir

    public bool UseListNavigation => false;
    public Button[] NavButtons => null;

    public Color NormalColor => Color.white;
    public Color SelectedColor => Color.white;

    public void SetHighlight(Button selected) { /* no-op */ }
    public void ClearHighlight() { /* no-op */ }

    // ============================ Unity ============================
    private void Reset()
    {
        if (panelProvider == null) panelProvider = GetComponentInParent<CanvasActionsProvider>();
        if (enterButton == null) enterButton = GetComponent<Button>();

        // Autorellenar imágenes si hay ≥10 como hijos (no crea nada)
        var imgs = GetComponentsInChildren<Image>(true);
        if (imgs.Length >= 10)
        {
            steps = new Image[10];
            for (int i = 0; i < 10; i++) steps[i] = imgs[i];
        }
    }

    private void Awake()
    {
        // Guardar escala base del label
        if (label != null) labelBaseScale = label.rectTransform.localScale;

        if (enterButton != null)
        {
            enterButton.onClick.RemoveAllListeners();
            enterButton.onClick.AddListener(EnterVolumeMode);
        }

        if (Application.isPlaying)
        {
            EnsureHiddenButtons();
            WireHiddenButtons();
            HideHiddenButtonsVisuals();
        }

        SetHints(false);
    }

    private void OnEnable()
    {
        // Al abrir Opciones, sincroniza desde el Mixer si procede
        if (readFromMixerOnEnable) SyncFromMixer();
    }

    private void OnDestroy()
    {
        if (Application.isPlaying)
        {
            if (blueHidden) blueHidden.onClick.RemoveAllListeners();
            if (orangeHidden) orangeHidden.onClick.RemoveAllListeners();
            if (yellowHidden) yellowHidden.onClick.RemoveAllListeners();
            if (enterButton) enterButton.onClick.RemoveAllListeners();
        }
    }

    private void OnValidate()
    {
        // Solo refresco visual en Editor
        value01 = Mathf.Clamp01(value01);
        Redraw();
    }

    // ===================== API de volumen =====================
    public float Value01
    {
        get => value01;
        set
        {
            value01 = Mathf.Clamp01(value);
            Redraw();
            if (!_suppressPush) PushToMixerIfAny();  // no empujar al sincronizar
        }
    }

    public int StepsFilled => Mathf.RoundToInt(value01 * 10f); // 0..10

    public void SetPercent(int percent) => Value01 = Mathf.Clamp01(percent / 100f);
    public void AddPercent(int deltaPercent) => AddStep01(deltaPercent / 100f);
    public void AddStep01(float delta01) => Value01 += delta01;

    /// <summary>Hook visual opcional: resalta/escala la barra activa (incluye TMP si está asignado).</summary>
    public void SetActive(bool enabled)
    {
        if (label != null)
        {
            label.rectTransform.localScale = enabled ? labelBaseScale * selectedScale : labelBaseScale;
        }
        // Si quieres, también puedes escalar la propia barra:
        // transform.localScale = enabled ? Vector3.one * 1.03f : Vector3.one;
    }

    // ===================== Modo volumen ======================
    public void EnterVolumeMode()
    {
        SetActive(true);
        SetHints(true);
        MenuInputRouter.Instance?.SetProvider(this);
    }

    private void ExitVolumeMode()
    {
        SetHints(false);
        SetActive(false);
        if (panelProvider != null)
            MenuInputRouter.Instance?.SetProvider(panelProvider);
    }

    private void Increase()
    {
        AddStep01(step);
        SoundManager.PlayFX(5); // Hit_or_NotReady
    }
    private void Decrease()
    {
        AddStep01(-step);
        SoundManager.PlayFX(5); // Hit_or_NotReady
    }

    private void SetHints(bool on)
    {
        if (hintUpBlue) hintUpBlue.SetActive(on);
        if (hintDownOrange) hintDownOrange.SetActive(on);
    }

    // ==================== Mixer: leer / escribir ====================
    /// <summary>Llama esto para que la barra refleje el valor actual del Mixer (sin empujar).</summary>
    public void SyncFromMixer()
    {
        _suppressPush = true;
        if (channel == VolumeChannel.Music)
            Value01 = SoundManager.GetMusicVolume01();
        else
            Value01 = SoundManager.GetSfxVolume01();
        _suppressPush = false;
    }

    // Reemplaza PushToMixerIfAny() por esto:
    private void PushToMixerIfAny()
    {
        if (channel == VolumeChannel.Music)
            SoundManager.SetMusicVolume(value01);
        else
            SoundManager.SetFxVolume(value01);
    }

    // ===================== Botones “puente” =====================
    private void EnsureHiddenButtons()
    {
        if (blueHidden == null) blueHidden = CreateHiddenButton("Blue_Hidden");
        if (orangeHidden == null) orangeHidden = CreateHiddenButton("Orange_Hidden");
        if (yellowHidden == null) yellowHidden = CreateHiddenButton("Yellow_Hidden");
    }

    private Button CreateHiddenButton(string name)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.transform.SetParent(transform, false);
        go.hideFlags = HideFlags.DontSave; // no serializar en escena/prefab
        var img = go.GetComponent<Image>();
        img.color = new Color(0, 0, 0, 0); // invisible
        var btn = go.GetComponent<Button>();
        btn.interactable = true;
        return btn;
    }

    private void WireHiddenButtons()
    {
        if (blueHidden != null)
        {
            blueHidden.onClick.RemoveAllListeners();
            blueHidden.onClick.AddListener(Increase);
        }
        if (orangeHidden != null)
        {
            orangeHidden.onClick.RemoveAllListeners();
            orangeHidden.onClick.AddListener(Decrease);
        }
        if (yellowHidden != null)
        {
            yellowHidden.onClick.RemoveAllListeners();
            yellowHidden.onClick.AddListener(ExitVolumeMode);
        }
    }

    private void HideHiddenButtonsVisuals()
    {
        var arr = new[] { blueHidden, orangeHidden, yellowHidden };
        foreach (var b in arr)
        {
            if (!b) continue;
            var rt = b.GetComponent<RectTransform>();
            if (rt) rt.sizeDelta = Vector2.zero; // no molestan al layout
        }
    }

    // ==================== Render de la barra ====================
    private void Redraw()
    {
        if (steps == null) return;
        int filled = StepsFilled;
        for (int i = 0; i < steps.Length; i++)
        {
            if (!steps[i]) continue;
            var c = steps[i].color;
            c.a = (i < filled) ? filledAlpha : emptyAlpha;
            steps[i].color = c;
        }
    }
}
