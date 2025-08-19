using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[CreateAssetMenu(menuName = "PlayerChoices/Player Choices", fileName = "PlayerChoices")]
public class PlayerChoices : ScriptableObject
{
    public static GameManager.GameLength gameLength;
    public static PlayerChoices Instance { get; private set; }

    public enum PlayerColor { Azul, Naranja, Verde, Amarillo };
    public List<PlayerData> jugadoresActivos = new();
    public List<Material> colorMaterials = new();
    public PlayerColor winner;

    [System.Serializable]
    public class PlayerData
    {
        public PlayerColor Color;
        public InputDevice Device;
        public string Schema; // "Keyboard", "Gamepad", etc.
        public int SkinIndex; // Index of the skin selected by the player
        public string VehicleId;  // null o "" si no hay vehículo en ese minijuego
        public string WeaponId;   // idem
        public int wins; // Number of wins for the player

        public PlayerData(PlayerColor color, InputDevice device, string schema = null, int skinIndex = 0, string vehicleId = null, string weaponId = null)
        {
            Color = color;
            Device = device;
            SkinIndex = skinIndex;
            Schema = schema;
            VehicleId = vehicleId;
            WeaponId = weaponId;
        }
    }
    private void OnEnable()
    {
        Instance = this;
    }

    // --- SETTERS ---
    public static void SetPartyLength(GameManager.GameLength gameLength_) { gameLength = gameLength_; }

    private static bool TryToAddPlayer(PlayerColor color, InputDevice device)
    {
        if (Instance.jugadoresActivos.Exists(x => x.Device == device) || Instance.jugadoresActivos.Exists(x => x.Color == color))
        {
            Debug.LogWarning($"Este dispositivo o color ya está asignado a otro jugador.");
            return false;
        }

        Instance.jugadoresActivos.Add(new PlayerData(color, device, device.displayName, 0));
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

    public static void RemovePlayer(InputDevice device)
    {
        var player = Instance.jugadoresActivos.Find(x => x.Device == device);
        if (player != null)
        {
            Instance.jugadoresActivos.Remove(player);
            Debug.Log($"Jugador con color {player.Color} y dispositivo {device.displayName} eliminado.");
        }
        else
        {
            Debug.LogWarning("No se encontró ningún jugador con este dispositivo.");
        }
    }

    public void SetWinner(PlayerColor color)
    {
        winner = color;
        AddWin(color);
        Debug.Log($"Ganador establecido: {color}");
    }

    public void AddWin(PlayerColor color)
    {
        GetPlayerByColor(color).wins++;
    }

    // --- GETTERS ---
    public static GameManager.GameLength GetPartyLengthEnum() { return gameLength; }

    public static List<PlayerData> GetActivePlayers()
    {
        return Instance.jugadoresActivos;
    }

    public static int GetNumberOfPlayers()
    {
        return Instance.jugadoresActivos.Count;
    }

    public static InputDevice GetDeviceForPlayer(PlayerColor color)
    {
        var playerData = Instance.jugadoresActivos.Find(x => x.Color == color);
        return playerData?.Device;
    }

    public static PlayerData GetPlayerByColor(PlayerColor color)
    {
        return Instance.jugadoresActivos.Find(x => x.Color == color);
    }

    public static PlayerData GetPlayerByDevice(InputDevice device)
    {
        return Instance.jugadoresActivos.Find(x => x.Device == device);
    }

    public static PlayerColor? GetPlayerColorByDevice(InputDevice device)
    {
        var playerData = Instance.jugadoresActivos.Find(x => x.Device == device);
        return playerData != null ? playerData.Color : (PlayerColor?)null;
    }

    public static List<PlayerColor> GetActivePlayersColors()
    {
        List<PlayerColor> colors = new List<PlayerColor>();
        foreach (var player in Instance.jugadoresActivos)
        {
            colors.Add(player.Color);
        }
        return colors;
    }

    public static Material GetMaterialByColor(PlayerColor color)
    {
        Material material = Instance.colorMaterials[(int)color];
        return material;
    }

    public static bool IsPlayerActive(InputDevice device)
    {
        return Instance.jugadoresActivos.Exists(x => x.Device == device);
    }

    public static bool IsPlayerActive(PlayerColor color)
    {
        return Instance.jugadoresActivos.Exists(x => x.Color == color);
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
        Instance.jugadoresActivos.Clear();
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
        else if (device is Joystick)
        {
            return "Joystick";
        }
        else if (device is Mouse)
        {
            return "Mouse";
        }
        else
        {
            Debug.LogWarning("Dispositivo no reconocido: " + device.displayName);
            return "Unknown";
        }
    }

    public static string GetColorFromDevice(InputDevice device)
    {
        var playerData = Instance.jugadoresActivos.Find(x => x.Device == device);
        return playerData != null ? playerData.Color.ToString() : null;
    }

    public static bool RemovePlayer(PlayerColor color)
    {
        var player = Instance.jugadoresActivos.Find(x => x.Color == color);
        if (player != null)
        {
            Instance.jugadoresActivos.Remove(player);
            Debug.Log($"Jugador con color {color} eliminado.");
            return true;
        }
        return false;
    }

    public static bool RemovePlayer(string color)
    {
        if (System.Enum.TryParse(color, true, out PlayerColor pc))
            return RemovePlayer(pc);
        Debug.LogWarning($"Color '{color}' no es válido.");
        return false;
    }

    public static PlayerColor GetWinner()
    {
        return Instance.winner;
    }

    public static Color GetColorRGBA(PlayerColor color)
    {
        return color switch
        {
            PlayerColor.Azul => new Color32(0, 109, 255, 255),    // Azul
            PlayerColor.Naranja => new Color32(255, 161, 49, 255), // Naranja
            PlayerColor.Verde => new Color32(0, 255, 0, 255),     // Verde
            PlayerColor.Amarillo => new Color32(255, 252, 0, 255),  // Amarillo
            _ => Color.white
        };
    }

    public static Color GetColorRGBA(string color)
    {
        return color.ToLower() switch
        {
            "azul" => new Color32(0, 109, 255, 255),    // Azul
            "naranja" => new Color32(255, 161, 49, 255), // Naranja
            "verde" => new Color32(0, 255, 0, 255),     // Verde
            "amarillo" => new Color32(255, 252, 0, 255),  // Amarillo
            _ => Color.white
        };
    }

    // --- GESTIÓN DE SKIN ---
    public static void SetPlayerSkin(PlayerColor color, int skinIndex)
    {
        var p = Instance.jugadoresActivos.Find(x => x.Color == color);
        if (p != null) p.SkinIndex = skinIndex;
    }

    public static void SetPlayerSkin(InputDevice device, int skinIndex)
    {
        var p = Instance.jugadoresActivos.Find(x => x.Device == device);
        if (p != null) p.SkinIndex = skinIndex;
    }

    public static int GetPlayerSkin(PlayerColor color)
    {
        var p = Instance.jugadoresActivos.Find(x => x.Color == color);
        return p != null ? p.SkinIndex : 0;
    }

    public static int GetPlayerSkin(InputDevice device)
    {
        var p = Instance.jugadoresActivos.Find(x => x.Device == device);
        return p != null ? p.SkinIndex : 0;
    }

    // --- VEHICLE AND WEAPON MANAGEMENT ---
    public static void SetPlayerVehicle(PlayerColor color, string vehicleId)
    {
        var p = Instance.jugadoresActivos.Find(x => x.Color == color);
        if (p != null) p.VehicleId = vehicleId;
    }

    public static string GetPlayerVehicle(PlayerColor color)
    {
        var p = Instance.jugadoresActivos.Find(x => x.Color == color);
        return p?.VehicleId;
    }

    public static void SetPlayerWeapon(PlayerColor color, string weaponId)
    {
        var p = Instance.jugadoresActivos.Find(x => x.Color == color);
        if (p != null) p.WeaponId = weaponId;
    }

    public static string GetPlayerWeapon(PlayerColor color)
    {
        var p = Instance.jugadoresActivos.Find(x => x.Color == color);
        return p?.WeaponId;
    }
}
