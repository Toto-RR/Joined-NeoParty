using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class LobbyInputManager : MonoBehaviour
{
    public LobbyManager lobbyManager;

    private List<string> colorOrder = new List<string> { "Blue", "Orange", "Green", "Yellow" };

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

        // TECLADO: Salir del lobby
        if (Keyboard.current != null && Keyboard.current.backspaceKey.wasPressedThisFrame)
        {
            Debug.Log("Backspace presionado");
            string color = PlayerChoices.GetColorFromDevice(Keyboard.current);
            PlayerChoices.RemovePlayer(Keyboard.current);
            if (color != null) lobbyManager.RemovePlayer(color);
        }

        // GAMEPADS: Salir del lobby
        foreach (var gamepad in Gamepad.all)
        {
            if (gamepad != null && gamepad.buttonEast.wasPressedThisFrame)
            {
                Debug.Log($"Botón East presionado por {gamepad.displayName}");
                string color = PlayerChoices.GetColorFromDevice(gamepad);
                PlayerChoices.RemovePlayer(gamepad);
                if (color != null) lobbyManager.RemovePlayer(color);
            }
        }
    }

    void JoinNextColor(InputDevice device)
    {
        if (PlayerChoices.IsPlayerActive(device)) return;

        foreach (var color in colorOrder)
        {
            if (!PlayerChoices.IsPlayerActive(color))
            {
                RegisterPlayer(color, device);
                return;
            }
        }

        Debug.LogWarning("No hay colores disponibles.");
    }

    void JoinSpecificColor(string color, InputDevice device)
    {
        if (PlayerChoices.IsPlayerActive(device) || PlayerChoices.IsPlayerActive(color)) return;

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
