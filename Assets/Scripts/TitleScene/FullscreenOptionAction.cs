using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FullscreenOptionAction : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Toggle checkbox;
    [SerializeField] private Button actionButton;

    [Header("Opciones")]
    [SerializeField] private FullScreenMode fullScreenMode = FullScreenMode.FullScreenWindow;
    [SerializeField] private string playerPrefsKey = "opt_fullscreen";
    [SerializeField] private bool defaultToSystem = true;

#if UNITY_EDITOR
    [Header("Editor (simulación)")]
    [SerializeField] private bool simulateInEditor = true;
#endif

    [Header("Texto (opcional)")]
    [SerializeField] private TextMeshProUGUI debugLabel;
    [SerializeField] private string onText = "Fullscreen: ON";
    [SerializeField] private string offText = "Fullscreen: OFF";

    bool _suppress;
    int _lastHandledFrame = -1; // debounce por frame

    void Awake()
    {
        if (!debugLabel) debugLabel = GetComponentInChildren<TextMeshProUGUI>(true);

        bool initial = PlayerPrefs.HasKey(playerPrefsKey)
            ? PlayerPrefs.GetInt(playerPrefsKey) == 1
            : (defaultToSystem ? Screen.fullScreen : true);

        _suppress = true;
        if (checkbox) checkbox.isOn = initial;
        _suppress = false;

        UpdateLabel(initial);
        ApplyFullscreenInternal(initial, save: false);

        if (checkbox) checkbox.onValueChanged.AddListener(OnCheckboxChanged);
        if (actionButton) actionButton.onClick.AddListener(OnActionButtonClick);
    }

    void OnDestroy()
    {
        if (checkbox) checkbox.onValueChanged.RemoveListener(OnCheckboxChanged);
        if (actionButton) actionButton.onClick.RemoveListener(OnActionButtonClick);
    }

    void OnActionButtonClick()
    {
        // si ya hubo cambio este frame (por el Toggle), ignora este click
        if (_lastHandledFrame == Time.frameCount) return;

        bool next = checkbox ? !checkbox.isOn : !Screen.fullScreen;

        _suppress = true;
        if (checkbox) checkbox.isOn = next; // esto ya no disparará OnCheckboxChanged
        _suppress = false;

        _lastHandledFrame = Time.frameCount;
        UpdateLabel(next);
        ApplyFullscreenInternal(next, save: true);
    }

    void OnCheckboxChanged(bool isOn)
    {
        if (_suppress) return;

        _lastHandledFrame = Time.frameCount; //  marca que ya manejamos el cambio este frame
        UpdateLabel(isOn);
        ApplyFullscreenInternal(isOn, save: true);
    }

    void UpdateLabel(bool isOn)
    {
        if (debugLabel) debugLabel.text = isOn ? onText : offText;
    }

    void ApplyFullscreenInternal(bool isOn, bool save)
    {
#if UNITY_EDITOR
        if (simulateInEditor)
        {
            Debug.Log($"[FullscreenOptionAction] (Editor Sim) Would set fullscreen={isOn}, mode={fullScreenMode}");
        }
        // En editor no cambiamos la ventana, pero sí guardamos prefs si toca
#else
#if UNITY_WEBGL
        Screen.fullScreen = isOn;
#else
        if (isOn) { Screen.fullScreenMode = fullScreenMode; Screen.fullScreen = true; }
        else      { Screen.fullScreenMode = FullScreenMode.Windowed; Screen.fullScreen = false; }
#endif
#endif
        if (save)
        {
            PlayerPrefs.SetInt(playerPrefsKey, isOn ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
}
