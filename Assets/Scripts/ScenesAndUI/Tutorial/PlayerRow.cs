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
    [SerializeField] private InputActionAsset actions;

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

        map = actions.FindActionMap("Tutorial", throwIfNotFound: false);
        if (map == null)
        {
            Debug.LogError("[PlayerRow] No se encontró el ActionMap.");
            return;
        }

        readyAction = map.FindAction("Ready", throwIfNotFound: false);
        if (readyAction == null)
        {
            Debug.LogError("[PlayerRow] No se encontró la acción.");
            return;
        }

        readyAction.performed += OnReadyPerformed;

        //if (!map.enabled) map.Enable();
    }

    private void OnReadyPerformed(InputAction.CallbackContext ctx)
    {
        if (ctx.control != null && ctx.control.device == assignedDevice)
        {
            ToggleReady();
        }
    }

    private void ToggleReady()
    {
        SetReady(!isReady);
        tutorialManager.OnPlayerReadyChanged(this, isReady);
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
        // opcional: avisar al manager si esta fila se desactiva
        if (tutorialManager != null) tutorialManager.OnPlayerReadyChanged(this, false);
    }

    private void OnDestroy()
    {
        if (readyAction != null) readyAction.performed -= OnReadyPerformed;
        if (tutorialManager != null) tutorialManager.UnregisterRow(this);
    }

    public void ToggleMap(bool enable)
    {
        if (enable) map.Enable();
        else map.Disable();
    }
}
