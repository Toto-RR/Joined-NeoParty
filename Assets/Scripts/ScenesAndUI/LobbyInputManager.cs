using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class LobbyInputManager : MonoBehaviour
{
    public LobbyManager lobbyManager;

    private List<string> colorOrder = new List<string> { "Blue", "Orange", "Green", "Yellow" };
    private int currentIndex = 0;

    void Start()
    {
        if (Keyboard.current == null)
        {
            Debug.LogWarning("Teclado no detectado. Forzando...");
            InputSystem.AddDevice<Keyboard>();
        }
        else
        {
            Debug.Log("✔ Teclado detectado correctamente.");
        }
    }

    void Update()
    {
        // TECLADO: Join automático
        if (Keyboard.current != null)
        {
            if (Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                Debug.Log("Se ha presionado SPACE");
                JoinNextColor(Keyboard.current);
            }
        }

        // GAMEPADS: Join automático
        foreach (var gamepad in Gamepad.all)
        {
            if (gamepad != null && gamepad.buttonSouth.wasPressedThisFrame)
            {
                JoinNextColor(gamepad);
            }
        }

        // TECLADO: Join específico
        if (Keyboard.current != null)
        {
            if (Keyboard.current.digit1Key.wasPressedThisFrame) JoinSpecificColor("Blue", Keyboard.current);
            if (Keyboard.current.digit2Key.wasPressedThisFrame) JoinSpecificColor("Orange", Keyboard.current);
            if (Keyboard.current.digit3Key.wasPressedThisFrame) JoinSpecificColor("Green", Keyboard.current);
            if (Keyboard.current.digit4Key.wasPressedThisFrame) JoinSpecificColor("Yellow", Keyboard.current);
        }
    }

    void JoinNextColor(InputDevice device)
    {
        if (PlayerChoices.IsPlayerActive(device)) return;

        string color = colorOrder[currentIndex];
        RegisterPlayer(color, device);
        currentIndex++;
    }

    void JoinSpecificColor(string color, InputDevice device)
    {
        RegisterPlayer(color, device);
    }

    void RegisterPlayer(string colorName, InputDevice device)
    {
        if (System.Enum.TryParse(colorName, out PlayerChoices.PlayerColor color))
        {
            PlayerChoices.AddPlayer(color, device);
            if (PlayerChoices.IsPlayerActive(color))
            {
                lobbyManager.AddNewPlayer(color.ToString());
            }
        }
        else
        {
            Debug.LogError($"❌ Color inválido: {colorName}");
        }
    }
}
