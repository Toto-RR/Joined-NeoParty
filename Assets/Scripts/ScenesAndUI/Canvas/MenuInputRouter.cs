using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

[DefaultExecutionOrder(-1000)]
public class MenuInputRouter : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionAsset interfaceAsset;
    [SerializeField] private string actionMapName = "MainMenu";
    [SerializeField] private string blueActionName = "BotonAzul";       // subir
    [SerializeField] private string orangeActionName = "BotonNaranja";  // bajar
    [SerializeField] private string greenActionName = "BotonVerde";     
    [SerializeField] private string backActionName = "BotonAmarillo";   // atrás
    [SerializeField] private string triggerActionName = "Trigger";      

    private static MenuInputRouter _instance;
    public static MenuInputRouter Instance => _instance;

    private IMenuActionsProvider currentProvider;
    private int selectionIndex = -1;

    private InputAction blueAction, orangeAction, greenAction, yellowAction, triggerAction;

    private void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        _instance = this;

        var map = interfaceAsset.FindActionMap(actionMapName, throwIfNotFound: true);

        blueAction = map.FindAction(blueActionName, throwIfNotFound: true);
        orangeAction = map.FindAction(orangeActionName, throwIfNotFound: true);
        greenAction = map.FindAction(greenActionName, throwIfNotFound: true);
        yellowAction = map.FindAction(backActionName, throwIfNotFound: false);
        triggerAction = map.FindAction(triggerActionName, throwIfNotFound: true);

        blueAction.performed += OnBluePerformed;
        orangeAction.performed += OnOrangePerformed;
        greenAction.performed += OnGreenPerformed;
        triggerAction.performed += OnTriggerPerformed;
        if (yellowAction != null) yellowAction.performed += OnYellowPerformed;

        map.Enable();
    }

    private void OnDestroy()
    {
        if (blueAction != null) blueAction.performed -= OnBluePerformed;
        if (orangeAction != null) orangeAction.performed -= OnOrangePerformed;
        if (greenAction != null) greenAction.performed -= OnGreenPerformed;
        if (yellowAction != null) yellowAction.performed -= OnYellowPerformed;
        if (triggerAction != null) triggerAction.performed -= OnTriggerPerformed;

        if (_instance == this) _instance = null;
    }

    // ===== Input callbacks =====
    private void OnBluePerformed(InputAction.CallbackContext ctx)
    {
        if (currentProvider == null) return;
        if (currentProvider.UseListNavigation) MoveSelection(-1);
        else Click(currentProvider.BlueButton);
    }

    private void OnOrangePerformed(InputAction.CallbackContext ctx)
    {
        if (currentProvider == null) return;
        if (currentProvider.UseListNavigation) MoveSelection(+1);
        else Click(currentProvider.OrangeButton);
    }

    private void OnGreenPerformed(InputAction.CallbackContext ctx)
    {
        if (currentProvider == null) return;
        else Click(currentProvider.GreenButton);
    }

    private void OnYellowPerformed(InputAction.CallbackContext ctx)
    {
        if (currentProvider == null) return;

        if (currentProvider.UseListNavigation)
            Back(); // siempre volver en modo lista
        else
            Click(currentProvider.YellowButton);
    }
    private void OnTriggerPerformed(InputAction.CallbackContext ctx)
    {
        if (currentProvider == null) return;
        if (currentProvider.UseListNavigation) SubmitSelection();
        else Click(currentProvider.GreenButton); // Trigger actúa como “aceptar” en modo colores
    }

    // ===== API para providers =====
    public void SetProvider(IMenuActionsProvider provider)
    {
        // limpiar selección del provider anterior
        if (currentProvider != null)
        {
            currentProvider.ClearHighlight();
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(null);
        }

        currentProvider = provider;

        // inicializar selección si el nuevo usa modo lista
        if (currentProvider != null && currentProvider.UseListNavigation)
            EnsureInitialSelection();
        else
            selectionIndex = -1;
    }

    public void ClearProvider(IMenuActionsProvider provider)
    {
        if (currentProvider == provider)
        {
            currentProvider.ClearHighlight();
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(null);

            currentProvider = null;
            selectionIndex = -1;
        }
    }

    // ===== Helpers =====
    private void Click(Button b)
    {
        if (b == null || !b.interactable) return;
        b.onClick?.Invoke();
        b.Select();
    }

    private void EnsureInitialSelection()
    {
        selectionIndex = -1;
        var list = currentProvider.NavButtons;
        if (list == null || list.Length == 0) return;

        for (int i = 0; i < list.Length; i++)
        {
            if (list[i] != null && list[i].interactable)
            {
                SetSelected(i);
                return;
            }
        }
    }

    private void MoveSelection(int dir)
    {
        var list = currentProvider?.NavButtons;
        if (list == null || list.Length == 0) return;

        if (selectionIndex < 0) EnsureInitialSelection();
        if (selectionIndex < 0) return;

        int next = selectionIndex;
        for (int step = 0; step < list.Length; step++)
        {
            next = (next + dir + list.Length) % list.Length;
            if (list[next] != null && list[next].interactable)
            {
                SetSelected(next);
                break;
            }
        }
    }

    private void SetSelected(int idx)
    {
        var list = currentProvider.NavButtons;
        selectionIndex = idx;

        var go = list[idx]?.gameObject;
        if (go != null && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(go);

        currentProvider.ClearHighlight();
        currentProvider.SetHighlight(list[idx]);
    }

    private void SubmitSelection()
    {
        var list = currentProvider?.NavButtons;
        if (list == null || list.Length == 0 || selectionIndex < 0) return;

        Click(list[selectionIndex]);
    }

    private void Back()
    {
        var back = currentProvider.BackButton; // específico o amarillo por defecto
        if (back != null && back.interactable)
        {
            currentProvider.ClearHighlight();
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(null);

            Click(back);
        }
    }
}
