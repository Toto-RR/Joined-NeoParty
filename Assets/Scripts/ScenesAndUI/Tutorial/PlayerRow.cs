using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

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

        string binding = "";

        if (assignedDevice is Keyboard)
        {
            binding = "<Keyboard>/space";
        }
        else if (assignedDevice is Gamepad)
        {
            binding = "<Gamepad>/buttonSouth";
        }

        readyAction = new InputAction(type: InputActionType.Button, binding: binding);
        readyAction.AddBinding(binding).WithGroup(player.Device.layout);
        readyAction.performed += ctx => OnJoinKeyPressed();
        readyAction.Enable();
    }

    private void OnDisable()
    {
        readyAction?.Disable();
        readyAction?.Dispose();
    }

    private void OnJoinKeyPressed()
    {
        if (!isReady)
        {
            tutorialManager.PlayerReady(this);
        }
    }

    public void SetReady()
    {
        isReady = true;
        checkMark.SetActive(true);
    }
}
