using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class MenuInputRouter : MonoBehaviour
{
    [Header("Input Actions (Action Map: MainMenu)")]
    [SerializeField] private InputActionAsset interfaceAsset; // tu Interface.inputactions
    [SerializeField] private string actionMapName = "MainMenu";
    [SerializeField] private string blueActionName = "BotonAzul";
    [SerializeField] private string orangeActionName = "BotonNaranja";
    [SerializeField] private string greenActionName = "BotonVerde";
    [SerializeField] private string yellowActionName = "BotonAmarillo"; 

    [Header("Optional")]
    [SerializeField] private PlayerInput playerInput; // si usas PlayerInput

    private static MenuInputRouter _instance;
    public static MenuInputRouter Instance => _instance;

    private IMenuActionsProvider currentProvider;

    private InputAction blueAction, orangeAction, greenAction, yellowAction;

    private void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        _instance = this;
        DontDestroyOnLoad(gameObject); // opcional si quieres que persista

        var actions = playerInput != null ? playerInput.actions : interfaceAsset;
        var map = actions.FindActionMap(actionMapName, throwIfNotFound: true);

        blueAction = map.FindAction(blueActionName, throwIfNotFound: true);
        orangeAction = map.FindAction(orangeActionName, throwIfNotFound: true);
        greenAction = map.FindAction(greenActionName, throwIfNotFound: true);
        yellowAction = map.FindAction(yellowActionName, throwIfNotFound: false);

        blueAction.performed += _ => Click(currentProvider?.BlueButton);
        orangeAction.performed += _ => Click(currentProvider?.OrangeButton);
        greenAction.performed += _ => Click(currentProvider?.GreenButton);
        if (yellowAction != null) yellowAction.performed += _ => Click(currentProvider?.YellowButton);

        if (playerInput == null && !map.enabled) map.Enable();
    }

    private void OnDestroy()
    {
        if (_instance == this) _instance = null;
        if (blueAction != null) blueAction.performed -= _ => Click(currentProvider?.BlueButton);
        if (orangeAction != null) orangeAction.performed -= _ => Click(currentProvider?.OrangeButton);
        if (greenAction != null) greenAction.performed -= _ => Click(currentProvider?.GreenButton);
        if (yellowAction != null) yellowAction.performed -= _ => Click(currentProvider?.YellowButton);
    }

    private void Click(Button b)
    {
        if (b == null || !b.interactable) return;
        b.onClick?.Invoke();
        b.Select(); // opcional: highlight/anim UI
    }

    // API para proveedores
    public void SetProvider(IMenuActionsProvider provider) => currentProvider = provider;
    public void ClearProvider(IMenuActionsProvider provider) { if (currentProvider == provider) currentProvider = null; }
}
