using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
using System.Collections;

public class PlayerRow : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI playerNameText;
    public GameObject checkMark;

    private TutorialManager tutorialManager;
    public bool isReady = false;

    private InputDevice assignedDevice;
    private InputAction readyAction;

    public void Setup(PlayerChoices.PlayerData player, TutorialManager manager)
    {
        playerNameText.text = player.Color.ToString();
        tutorialManager = manager;
        checkMark.SetActive(false);

        assignedDevice = player.Device;

        readyAction = new InputAction(type: InputActionType.Button);

        if (assignedDevice is Keyboard)
        {
            readyAction.AddBinding("<Keyboard>/space");
        }
        else if (assignedDevice is Gamepad)
        {
            readyAction.AddBinding("<Gamepad>/buttonSouth");
        }

        readyAction.performed += ctx =>
        {
            if (ctx.control.device == assignedDevice && !isReady)
            {
                tutorialManager.PlayerReady(this);
            }
        };

        readyAction.Enable();
    }

    private void OnDisable()
    {
        readyAction?.Disable();
    }

    private void OnDestroy()
    {
        readyAction?.Dispose();
    }

    public void SetReady()
    {
        isReady = true;
        checkMark.SetActive(true);
    }
}
