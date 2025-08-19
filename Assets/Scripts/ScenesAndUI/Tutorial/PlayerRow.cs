using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PlayerRow : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI playerNameText;
    public GameObject checkMark;

    [Header("Input")]
    [SerializeField] private InputActionAsset actions;  // Asigna tu .inputactions

    private TutorialManager tutorialManager;
    public bool isReady = false;

    private InputDevice assignedDevice;
    private InputActionMap map;
    private InputAction readyAction;

    public void Setup(PlayerChoices.PlayerData player, TutorialManager manager)
    {
        ConfigureColor(player.Color);
        
        playerNameText.text = player.Color.ToString();
        tutorialManager = manager;

        assignedDevice = player.Device;
        
        SetReady(false); // estado inicial

        // Buscar el mapa/acción del asset
        map = actions.FindActionMap("Tutorial", throwIfNotFound: false);
        if (map == null)
        {
            Debug.LogError($"[PlayerRow] No se encontró el ActionMap.");
            return;
        }

        readyAction = map.FindAction("Ready", throwIfNotFound: false);
        if (readyAction == null)
        {
            Debug.LogError($"[PlayerRow] No se encontró la acción.");
            return;
        }

        // Suscribirse filtrando por el dispositivo del jugador
        readyAction.performed += OnReadyPerformed;

        // Habilitar (si no lo está ya por otro componente)
        if (!map.enabled) map.Enable();
    }

    private void OnReadyPerformed(InputAction.CallbackContext ctx)
    {
        // Solo responde si la entrada proviene del dispositivo asignado
        if (ctx.control != null && ctx.control.device == assignedDevice)
        {
            ToggleReady();
        }
    }

    private void ToggleReady()
    {
        SetReady(!isReady);

        // Si tu TutorialManager ya tiene un método para “unready”, llámalo aquí.
        // Con lo que tienes ahora, mantengo la llamada existente cuando pasa a Ready.
        if (isReady)
        {
            tutorialManager.PlayerReady(this);
        }
    }

    public void SetReady(bool value)
    {
        isReady = value;
        if (checkMark != null) checkMark.SetActive(isReady);
    }

    private void ConfigureColor(PlayerChoices.PlayerColor color)
    {
        GetComponent<Image>().color = PlayerChoices.GetColorRGBA(color);
    }

    private void OnDisable()
    {
        if (readyAction != null) readyAction.performed -= OnReadyPerformed;
        // No deshabilito el map global aquí porque puede estar compartido con otros.
    }

    private void OnDestroy()
    {
        if (readyAction != null) readyAction.performed -= OnReadyPerformed;
    }
}
