using UnityEngine;

public class OptionsVolumeController : MonoBehaviour
{
    [Header("Barras de volumen")]
    [SerializeField] private VolumeBar musicBar;
    [SerializeField] private VolumeBar sfxBar;

    [Header("Zonas/Indicadores de ayuda (se encienden al activar)")]
    [SerializeField] private GameObject hintUpBlue;     // zona que indica "Azul = subir"
    [SerializeField] private GameObject hintDownOrange; // zona que indica "Naranja = bajar"

    [Header("Comportamiento")]
    [SerializeField] private float step = 0.1f; // 10%

    private enum Channel { Music, SFX }
    [SerializeField] private Channel active = Channel.Music;

    private void OnEnable()
    {
        SetHintsVisible(true);
        ApplyActiveHighlight();
    }

    private void OnDisable()
    {
        SetHintsVisible(false);
    }

    // ---- Métodos para enganchar desde tus botones de colores ----
    // Asigna estos a los onClick del Azul/Naranja en el CanvasActionsProvider del panel Opciones

    public void Blue_Increase()
    {
        var bar = GetActiveBar();
        if (bar == null) return;
        bar.AddStep01(step); // +10%
        SoundManager.PlayFX(5); // Hit_or_NotReady
    }

    public void Orange_Decrease()
    {
        var bar = GetActiveBar();
        if (bar == null) return;
        bar.AddStep01(-step); // -10%
        SoundManager.PlayFX(5); // Hit_or_NotReady
    }

    // ---- Selección de canal (llámalos desde botones/toggles invisibles o con Green) ----
    public void SelectMusic()
    {
        active = Channel.Music;
        ApplyActiveHighlight();
    }

    public void SelectSFX()
    {
        active = Channel.SFX;
        ApplyActiveHighlight();
    }

    // ---- Utils ----
    private VolumeBar GetActiveBar() => (active == Channel.Music) ? musicBar : sfxBar;

    private void SetHintsVisible(bool on)
    {
        if (hintUpBlue) hintUpBlue.SetActive(on);
        if (hintDownOrange) hintDownOrange.SetActive(on);
    }

    private void ApplyActiveHighlight()
    {
        if (musicBar) musicBar.SetActive(active == Channel.Music);
        if (sfxBar) sfxBar.SetActive(active == Channel.SFX);
    }
}
