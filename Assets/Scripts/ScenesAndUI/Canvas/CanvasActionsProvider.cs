using UnityEngine;
using UnityEngine.UI;

public class CanvasActionsProvider : MonoBehaviour, IMenuActionsProvider
{
    [Header("Buttons of THIS canvas (modo colores)")]
    [SerializeField] private Button blueButton;
    [SerializeField] private Button orangeButton;
    [SerializeField] private Button greenButton;
    [SerializeField] private Button yellowButton;

    [Header("Modo lista")]
    [SerializeField] private bool useListNavigation = false;
    [Tooltip("Orden de navegación en modo lista")]
    [SerializeField] private Button[] navButtons;

    [Tooltip("Opcional: botón a invocar al pulsar AMARILLO en modo lista. Si no se asigna, se usa YellowButton.")]
    [SerializeField] private Button backButtonListMode;

    [Header("Colores de selección (modo lista)")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color selectedColor = new Color(1f, 0.4f, 0.4f);

    public Button BlueButton => blueButton;
    public Button OrangeButton => orangeButton;
    public Button GreenButton => greenButton;
    public Button YellowButton => yellowButton;

    public bool UseListNavigation => useListNavigation;
    public Button[] NavButtons => navButtons;
    public Color NormalColor => normalColor;
    public Color SelectedColor => selectedColor;

    // NUEVO: si no hay back específico, usa YellowButton
    public Button BackButton => backButtonListMode != null ? backButtonListMode : yellowButton;

    private void OnEnable()
    {
        MenuInputRouter.Instance?.SetProvider(this);
    }

    private void Start()
    {
        //ColorButton();
        ClearHighlight();
        MenuInputRouter.Instance?.SetProvider(this);
    }

    private void OnDisable()
    {
        MenuInputRouter.Instance?.ClearProvider(this);
    }

    public void SetHighlight(Button selected)
    {
        if (navButtons == null) return;
        foreach (var b in navButtons)
        {
            if (b == null) continue;
            var img = b.GetComponent<Image>();
            if (img != null) img.color = (b == selected) ? selectedColor : normalColor;
        }
    }

    public void ClearHighlight()
    {
        if (navButtons == null) return;
        foreach (var b in navButtons)
        {
            if (b == null) continue;
            var img = b.GetComponent<Image>();
            if (img != null) img.color = normalColor;
        }
    }

    private void ColorButton()
    {
        if(useListNavigation) return;

        if(blueButton != null) blueButton.GetComponent<Image>().color = PlayerChoices.GetColorRGBA("azul");
        if(orangeButton != null) orangeButton.GetComponent<Image>().color = PlayerChoices.GetColorRGBA("naranja");
        if(greenButton != null) greenButton.GetComponent<Image>().color = PlayerChoices.GetColorRGBA("verde");
        if(yellowButton != null) yellowButton.GetComponent<Image>().color = PlayerChoices.GetColorRGBA("amarillo");
    }

    public void CustomDebug(string message)
    {
        Debug.Log($"CanvasActionsProvider: {message}");
    }
}
