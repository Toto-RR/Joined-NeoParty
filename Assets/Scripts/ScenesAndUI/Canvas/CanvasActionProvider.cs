using UnityEngine;
using UnityEngine.UI;

public class CanvasActionsProvider : MonoBehaviour, IMenuActionsProvider
{
    [Header("Buttons of THIS canvas/panel")]
    [SerializeField] private Button blueButton;
    [SerializeField] private Button orangeButton;
    [SerializeField] private Button greenButton;
    [SerializeField] private Button yellowButton;

    public Button BlueButton => blueButton;
    public Button OrangeButton => orangeButton;
    public Button GreenButton => greenButton;
    public Button YellowButton => yellowButton;

    private void Awake()
    {
        ColorButton();
    }

    private void OnEnable()
    {
        if (MenuInputRouter.Instance != null)
            MenuInputRouter.Instance.SetProvider(this);
    }

    private void OnDisable()
    {
        if (MenuInputRouter.Instance != null)
            MenuInputRouter.Instance.ClearProvider(this);
    }

    private void ColorButton()
    {
        if (blueButton != null) blueButton.GetComponent<Image>().color = PlayerChoices.GetColorRGBA("azul");
        if (orangeButton != null) orangeButton.GetComponent<Image>().color = PlayerChoices.GetColorRGBA("naranja");
        if (greenButton != null) greenButton.GetComponent<Image>().color = PlayerChoices.GetColorRGBA("verde");
        if (yellowButton != null) yellowButton.GetComponent<Image>().color = PlayerChoices.GetColorRGBA("amarillo");
    }
}
