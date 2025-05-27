using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public static class PlayerChoices
{
    private static GameManager.GameLength gameLength;
    public enum PlayerColor { Blue, Orange, Green, Yellow }
    private static List<PlayerData> jugadoresActivos = new List<PlayerData>();

    public class PlayerData
    {
        public PlayerColor Color;
        public InputDevice Device;

        public PlayerData(PlayerColor color, InputDevice device)
        {
            Color = color;
            Device = device;
        }
    }

    // --- SETTERS ---
    public static void SetPartyLength(GameManager.GameLength gameLength_) { gameLength = gameLength_; }

    private static bool TryToAddPlayer(PlayerColor color, InputDevice device)
    {
        if (jugadoresActivos.Exists(x => x.Device == device) || jugadoresActivos.Exists(x => x.Color == color))
        {
            Debug.LogWarning($"Este dispositivo o color ya está asignado a otro jugador.");
            return false;
        }

        jugadoresActivos.Add(new PlayerData(color, device));
        Debug.Log($"Dispositivo {device.displayName} asignado al color {color}.");
        return true;
    }

    public static void AddPlayer(PlayerColor color, InputDevice device)
    {
        if (!TryToAddPlayer(color, device))
        {
            Debug.LogError("No se puede añadir este jugador.");
        }
        
        Debug.Log($"Jugador registrado: {color} con {device.displayName}");
    }


    // --- GETTERS ---
    public static GameManager.GameLength GetPartyLengthEnum() { return gameLength; }

    public static List<PlayerData> GetActivePlayers()
    {
        return jugadoresActivos;
    }

    public static int GetNumberOfPlayers()
    {
        return jugadoresActivos.Count;
    }

    public static InputDevice GetDeviceForPlayer(PlayerColor color)
    {
        var playerData = jugadoresActivos.Find(x => x.Color == color);
        return playerData?.Device;
    }

    public static PlayerData GetPlayerByColor(PlayerColor color)
    {
        return jugadoresActivos.Find(x => x.Color == color);
    }

    public static PlayerData GetPlayerByDevice(InputDevice device)
    {
        return jugadoresActivos.Find(x => x.Device == device);
    }

    public static PlayerColor GetPlayerColorByDevice(InputDevice device)
    {
        var playerData = jugadoresActivos.Find(x => x.Device == device);
        return playerData.Color;
    }

    public static List<PlayerColor> GetActivePlayersColors()
    {
        List<PlayerColor> colors = new List<PlayerColor>();
        foreach (var player in jugadoresActivos)
        {
            colors.Add(player.Color);
        }
        return colors;
    }

    public static bool IsPlayerActive(InputDevice device)
    {
        return jugadoresActivos.Exists(x => x.Device == device);
    }

    public static bool IsPlayerActive(PlayerColor color)
    {
        return jugadoresActivos.Exists(x => x.Color == color);
    }

    public static bool IsPlayerActive(string color)
    {
        if (System.Enum.TryParse(color, true, out PlayerColor playerColor))
        {
            return IsPlayerActive(playerColor);
        }
        Debug.LogWarning($"Color '{color}' no es válido.");
        return false;
    }

    public static void ResetPlayers()
    {
        jugadoresActivos.Clear();
    }

    public static string GetSchemaFromDevice(InputDevice device)
    {
        if (device is Keyboard)
        {
            return "Keyboard";
        }
        else if (device is Gamepad)
        {
            return "Gamepad";
        }
        else
        {
            Debug.LogWarning("Dispositivo no reconocido: " + device.displayName);
            return "Unknown";
        }
    }

}
