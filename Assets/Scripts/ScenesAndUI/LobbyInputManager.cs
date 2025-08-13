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

        // GAMEPADS: Join específico
        foreach (var gamepad in Gamepad.all)
        {
            if (gamepad != null)
            {
                if (gamepad.buttonNorth.wasPressedThisFrame) JoinSpecificColor("Blue", gamepad);
                if (gamepad.buttonWest.wasPressedThisFrame) JoinSpecificColor("Orange", gamepad);
                if (gamepad.buttonSouth.wasPressedThisFrame) JoinSpecificColor("Green", gamepad);
                if (gamepad.buttonEast.wasPressedThisFrame) JoinSpecificColor("Yellow", gamepad);
            }
        }

        //// GAMEPADS: Join automático
        //foreach (var gamepad in Gamepad.all)
        //{
        //    if (gamepad != null && gamepad.rightTrigger.wasPressedThisFrame)
        //    {
        //        JoinNextColor(gamepad);
        //    }
        //}
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
        // Si el mismo device ya está unido a este color -> TOGGLE OFF
        string current = PlayerChoices.GetColorFromDevice(device); // devuelve null si no está
        if (!string.IsNullOrEmpty(current) &&
            current.Equals(color, System.StringComparison.OrdinalIgnoreCase))
        {
            PlayerChoices.RemovePlayer(device);
            if (color != null) lobbyManager.RemovePlayer(color);

            return;
        }

        if (PlayerChoices.IsPlayerActive(device))
        {
            return;
        }

        // Si el color destino está ocupado, no unimos
        if (PlayerChoices.IsPlayerActive(color)) return;

        // Join normal
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
            Debug.LogError($"Color inválido: {colorName}");
        }
    }
}
