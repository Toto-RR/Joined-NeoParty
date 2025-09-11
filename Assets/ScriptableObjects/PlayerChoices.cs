using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.HID;

[CreateAssetMenu(menuName = "PlayerChoices/Player Choices", fileName = "PlayerChoices")]
public class PlayerChoices : ScriptableObject
{
    // --- Estado estático robusto ---
    private static PlayerChoices _instance;
    public static PlayerChoices Instance
    {
        get
        {
            if (_instance == null) InitIfNeeded();
            return _instance;
        }
        private set { _instance = value; }
    }

    private static bool _initialized;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        InitIfNeeded();
    }

    private static void InitIfNeeded()
    {
        if (_initialized) return;

        // 1) Intenta cargar desde Resources
        if (_instance == null)
        {
            _instance = Resources.Load<PlayerChoices>("PlayerChoices"); // Assets/Resources/PlayerChoices.asset
        }

        // 2) Si no existe el asset, crea una instancia en memoria (fallback)
        if (_instance == null)
        {
            _instance = ScriptableObject.CreateInstance<PlayerChoices>();
#if UNITY_EDITOR
            Debug.LogWarning("PlayerChoices: no se encontró 'Resources/PlayerChoices'. Se ha creado una instancia temporal en memoria.");
#endif
        }

        // 3) Asegura listas y tamaños mínimos
        _instance.jugadoresActivos ??= new List<PlayerData>();
        _instance.colorMaterials ??= new List<Material>();
        // opcional: asegura mínimo 4 entradas en colorMaterials para indexar por enum
        while (_instance.colorMaterials.Count < 4)
            _instance.colorMaterials.Add(null);

        _initialized = true;
    }

    // --- Estado de juego ---
    public static GameManager.GameLength gameLength;

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
            Schema = schema;
            SkinIndex = skinIndex;
            VehicleId = vehicleId;
            WeaponId = weaponId;
        }
    }

    // --- Setters ---
    public static void SetPartyLength(GameManager.GameLength gameLength_)
    {
        InitIfNeeded();
        gameLength = gameLength_;
    }

    private static bool TryToAddPlayer(PlayerColor color, InputDevice device)
    {
        InitIfNeeded();

        if (device == null)
        {
            Debug.LogWarning("TryToAddPlayer: device es null.");
            return false;
        }

        // Si el device es teclado o ratón, bloquea si ya hay uno de los dos
        if (IsKeyboardOrMouse(device))
        {
            if (Instance.jugadoresActivos.Exists(x => IsKeyboardOrMouse(x.Device)))
            {
                Debug.LogWarning("TryToAddPlayer: Ya hay un jugador usando teclado+ratón.");
                return false;
            }
        }
        else
        {
            // Para el resto, evita duplicados de device exacto
            if (Instance.jugadoresActivos.Exists(x => x.Device == device))
            {
                Debug.LogWarning("TryToAddPlayer: Este dispositivo ya está asignado a otro jugador.");
                return false;
            }
        }

        if (Instance.jugadoresActivos.Exists(x => x.Color == color))
        {
            Debug.LogWarning("TryToAddPlayer: Este color ya está asignado a otro jugador.");
            return false;
        }

        var schema = GetSchemaFromDevice(device);
        Instance.jugadoresActivos.Add(new PlayerData(color, device, schema, 0));
        Debug.Log($"Dispositivo {device.displayName} (Schema: {schema}) asignado al color {color}.");
        return true;
    }

    public static void AddPlayer(PlayerColor color, InputDevice device)
    {
        InitIfNeeded();
        if (!TryToAddPlayer(color, device))
        {
            Debug.LogError("No se puede añadir este jugador.");
            return;
        }
        Debug.Log($"Jugador registrado: {color} con {GetSchemaFromDevice(device)}");
    }

    public static void RemovePlayer(InputDevice device)
    {
        InitIfNeeded();
        var player = Instance.jugadoresActivos.Find(x => x.Device == device);
        if (player != null)
        {
            Instance.jugadoresActivos.Remove(player);
            Debug.Log($"Jugador con color {player.Color} y dispositivo {device?.displayName} eliminado.");
        }
        else
        {
            Debug.LogWarning("No se encontró ningún jugador con este dispositivo.");
        }
    }

    public void SetWinner(PlayerColor color)
    {
        // Método de instancia usado por tu código actual
        winner = color;
        AddWin(color);
        Debug.Log($"Ganador establecido: {color}");
    }

    public void AddWin(PlayerColor color)
    {
        var p = GetPlayerByColor(color);
        if (p != null) p.wins++;
    }

    // --- Getters ---
    public static GameManager.GameLength GetPartyLengthEnum()
    {
        InitIfNeeded();
        return gameLength;
    }

    public static List<PlayerData> GetActivePlayers()
    {
        InitIfNeeded();
        return Instance.jugadoresActivos;
    }

    public static int GetNumberOfPlayers()
    {
        InitIfNeeded();
        return Instance.jugadoresActivos.Count;
    }

    public static InputDevice GetDeviceForPlayer(PlayerColor color)
    {
        InitIfNeeded();
        var playerData = Instance.jugadoresActivos.Find(x => x.Color == color);
        return playerData?.Device;
    }

    public static PlayerData GetPlayerByColor(PlayerColor color)
    {
        InitIfNeeded();
        return Instance.jugadoresActivos.Find(x => x.Color == color);
    }

    public static PlayerData GetPlayerByDevice(InputDevice device)
    {
        InitIfNeeded();
        return Instance.jugadoresActivos.Find(x => x.Device == device);
    }

    public static PlayerColor? GetPlayerColorByDevice(InputDevice device)
    {
        InitIfNeeded();
        var playerData = Instance.jugadoresActivos.Find(x => x.Device == device);
        return playerData != null ? playerData.Color : (PlayerColor?)null;
    }

    public static List<PlayerColor> GetActivePlayersColors()
    {
        InitIfNeeded();
        List<PlayerColor> colors = new List<PlayerColor>();
        foreach (var player in Instance.jugadoresActivos)
            colors.Add(player.Color);
        return colors;
    }

    public static Material GetMaterialByColor(PlayerColor color)
    {
        InitIfNeeded();
        int idx = (int)color;
        if (idx < 0 || idx >= Instance.colorMaterials.Count)
        {
            Debug.LogWarning($"GetMaterialByColor: índice {idx} fuera de rango en colorMaterials.");
            return null;
        }
        return Instance.colorMaterials[idx];
    }

    public static bool IsPlayerActive(InputDevice device)
    {
        InitIfNeeded();
        return Instance.jugadoresActivos.Exists(x => x.Device == device);
    }

    public static bool IsPlayerActive(PlayerColor color)
    {
        InitIfNeeded();
        return Instance.jugadoresActivos.Exists(x => x.Color == color);
    }

    public static bool IsPlayerActive(string color)
    {
        InitIfNeeded();
        if (System.Enum.TryParse(color, true, out PlayerColor playerColor))
            return IsPlayerActive(playerColor);

        Debug.LogWarning($"Color '{color}' no es válido.");
        return false;
    }

    public static bool IsKeyboardOrMouse(InputDevice d)
    {
        return d is Keyboard || d is Mouse;
    }

    /// Devuelve los dispositivos con los que hay que emparejar al PlayerInput
    public static InputDevice[] GetPairDevices(InputDevice device)
    {
        InitIfNeeded();
        if (device is Keyboard || device is Mouse)
            return new InputDevice[] { Keyboard.current, Mouse.current };
        return new InputDevice[] { device };
    }

    public static string GetSchemaFromDevice(InputDevice device)
    {
        InitIfNeeded();
        if (device is Keyboard || device is Mouse) return "Keyboard&Mouse";
        if (device is Gamepad) return "Gamepad";
        if (device is HID) return "HID";
        if (device is Joystick) return "Joystick";
        if (device is Mouse) return "Mouse";

        Debug.LogWarning("Dispositivo no reconocido: " + device?.displayName);
        return "Unknown";
    }

    public static string GetColorFromDevice(InputDevice device)
    {
        InitIfNeeded();
        var playerData = Instance.jugadoresActivos.Find(x => x.Device == device);
        return playerData != null ? playerData.Color.ToString() : null;
    }

    public static bool RemovePlayer(PlayerColor color)
    {
        InitIfNeeded();
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
        InitIfNeeded();
        if (System.Enum.TryParse(color, true, out PlayerColor pc))
            return RemovePlayer(pc);

        Debug.LogWarning($"Color '{color}' no es válido.");
        return false;
    }

    public static PlayerColor GetWinner()
    {
        InitIfNeeded();
        return Instance.winner;
    }

    public static Color GetColorRGBA(PlayerColor color)
    {
        // No depende de instancia, pero llamamos Init por coherencia (si usas esto en Awake de UI)
        InitIfNeeded();
        return color switch
        {
            PlayerColor.Azul => new Color32(0, 109, 255, 255),
            PlayerColor.Naranja => new Color32(255, 161, 49, 255),
            PlayerColor.Verde => new Color32(0, 255, 0, 255),
            PlayerColor.Amarillo => new Color32(255, 252, 0, 255),
            _ => Color.white
        };
    }

    public static Color GetColorRGBA(string color)
    {
        InitIfNeeded();
        return color.ToLower() switch
        {
            "azul" => new Color32(0, 109, 255, 255),
            "naranja" => new Color32(255, 161, 49, 255),
            "verde" => new Color32(0, 255, 0, 255),
            "amarillo" => new Color32(255, 252, 0, 255),
            _ => Color.white
        };
    }

    public static PlayerColor GetRandomActivePlayerColor()
    {
        var actives = PlayerChoices.GetActivePlayers();
        if (actives != null && actives.Count > 0)
            return actives[UnityEngine.Random.Range(0, actives.Count)].Color;

        // Fallback si no hay activos
        return GetRandomPlayerColor();
    }

    public static PlayerColor GetRandomPlayerColor()
    {
        var values = (PlayerColor[])System.Enum.GetValues(typeof(PlayerColor));
        if (values == null || values.Length == 0) return default;

        // Si tu enum tiene un "None"/"Unknown", lo saltamos:
        for (int i = 0; i < 16; i++) // pequeños reintentos por seguridad
        {
            var pick = values[UnityEngine.Random.Range(0, values.Length)];
            var name = pick.ToString();
            if (!name.Equals("None", System.StringComparison.OrdinalIgnoreCase) &&
                !name.Equals("Unknown", System.StringComparison.OrdinalIgnoreCase))
                return pick;
        }
        return values[UnityEngine.Random.Range(0, values.Length)];
    }

    // --- Reset (instancia + wrapper estático) ---
    public void ResetPlayers()
    {
        // instancia (por compatibilidad con tu código)
        jugadoresActivos ??= new List<PlayerData>();
        jugadoresActivos.Clear();
    }

    public static void ResetPlayersStatic()
    {
        InitIfNeeded();
        Instance.ResetPlayers();
    }

    public void ResetGame()
    {
        InitIfNeeded();
        Instance.ResetPlayers();
        Instance.winner = default;
        gameLength = default;
    }

    // --- SKIN ---
    public static void SetPlayerSkin(PlayerColor color, int skinIndex)
    {
        InitIfNeeded();
        var p = Instance.jugadoresActivos.Find(x => x.Color == color);
        if (p != null) p.SkinIndex = skinIndex;
    }

    public static void SetPlayerSkin(InputDevice device, int skinIndex)
    {
        InitIfNeeded();
        var p = Instance.jugadoresActivos.Find(x => x.Device == device);
        if (p != null) p.SkinIndex = skinIndex;
    }

    public static int GetPlayerSkin(PlayerColor color)
    {
        InitIfNeeded();
        var p = Instance.jugadoresActivos.Find(x => x.Color == color);
        return p != null ? p.SkinIndex : 0;
    }

    public static int GetPlayerSkin(InputDevice device)
    {
        InitIfNeeded();
        var p = Instance.jugadoresActivos.Find(x => x.Device == device);
        return p != null ? p.SkinIndex : 0;
    }

    // --- Vehículo / Arma ---
    public static void SetPlayerVehicle(PlayerColor color, string vehicleId)
    {
        InitIfNeeded();
        var p = Instance.jugadoresActivos.Find(x => x.Color == color);
        if (p != null) p.VehicleId = vehicleId;
    }

    public static string GetPlayerVehicle(PlayerColor color)
    {
        InitIfNeeded();
        var p = Instance.jugadoresActivos.Find(x => x.Color == color);
        return p?.VehicleId;
    }

    public static void SetPlayerWeapon(PlayerColor color, string weaponId)
    {
        InitIfNeeded();
        var p = Instance.jugadoresActivos.Find(x => x.Color == color);
        if (p != null) p.WeaponId = weaponId;
    }

    public static string GetPlayerWeapon(PlayerColor color)
    {
        InitIfNeeded();
        var p = Instance.jugadoresActivos.Find(x => x.Color == color);
        return p?.WeaponId;
    }
}
