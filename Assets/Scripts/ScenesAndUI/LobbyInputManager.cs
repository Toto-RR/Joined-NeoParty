using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Collections;
using System.Drawing; // (ya estaba)

public class LobbyInputManager : MonoBehaviour
{
    private LobbyManager lobbyManager;
    public InputActionAsset actions;

    [SerializeField] private float holdThreshold = 0.5f;
    private readonly Dictionary<string, double> pressStart = new();
    private readonly HashSet<string> holdFired = new();
    private readonly Dictionary<string, Coroutine> holdRoutines = new();

    // --- READY por color ---
    private readonly Dictionary<string, bool> readyByColor = new();
    private static readonly string[] COLORS = new[] { "Blue", "Orange", "Green", "Yellow" };

    // --- Luces opcionales ---
    [Header("Luces Ready (opcionales)")]
    [SerializeField] private GameObject blueReadyLight;
    [SerializeField] private GameObject orangeReadyLight;
    [SerializeField] private GameObject greenReadyLight;
    [SerializeField] private GameObject yellowReadyLight;

    // --- Evento opcional de cambios de ready ---
    [System.Serializable]
    public class ReadyEvent : UnityEngine.Events.UnityEvent<string, bool> { }
    public ReadyEvent onReadyChanged;

    // --- NUEVO: mapa robusto de dispositivo -> color actual ---
    private readonly Dictionary<int, string> deviceToColor = new();

    private InputActionMap map; // guarda el map para Enable/Disable y unbind

    // Delegates guardados para poder des-suscribir
    private readonly Dictionary<InputAction, System.Action<InputAction.CallbackContext>> startedHandlers = new();
    private readonly Dictionary<InputAction, System.Action<InputAction.CallbackContext>> canceledHandlers = new();
    private System.Action<InputAction.CallbackContext> continuePerformedHandler;


    private void OnEnable()
    {
        lobbyManager = GetComponent<LobbyManager>();
        if (lobbyManager == null) Debug.Log("LobbyManager no encontrado");

        map = actions.FindActionMap("CharacterSelector");
        if (map == null)
        {
            Debug.LogError("No se encontró el ActionMap 'CharacterSelector'.");
            return;
        }

        AttachBindings(map);

        map.Enable();

        foreach (var c in COLORS)
            SetReadyInternal(c, false);
    }

    private void OnDisable()
    {
        DetachBindings();

        if (map != null)
            map.Disable();

        // Limpieza de corutinas y estados de hold para evitar callbacks tardíos
        foreach (var kv in holdRoutines)
            if (kv.Value != null) StopCoroutine(kv.Value);
        holdRoutines.Clear();
        pressStart.Clear();
        holdFired.Clear();
    }

    private void OnDestroy()
    {
        OnDisable(); // por si acaso
    }
    private void AttachBindings(InputActionMap map)
    {
        BindWithHold(map, "JoinBlue", "Blue");
        BindWithHold(map, "JoinOrange", "Orange");
        BindWithHold(map, "JoinGreen", "Green");
        BindWithHold(map, "JoinYellow", "Yellow");
        Continue(map, "Continue");
    }

    private void DetachBindings()
    {
        // JoinX
        foreach (var kv in startedHandlers)
            if (kv.Key != null) kv.Key.started -= kv.Value;
        foreach (var kv in canceledHandlers)
            if (kv.Key != null) kv.Key.canceled -= kv.Value;
        startedHandlers.Clear();
        canceledHandlers.Clear();

        // Continue
        if (map != null)
        {
            var cont = map.FindAction("Continue", throwIfNotFound: false);
            if (cont != null && continuePerformedHandler != null)
                cont.performed -= continuePerformedHandler;
        }
        continuePerformedHandler = null;
    }

    private void BindWithHold(InputActionMap map, string actionName, string color)
    {
        var action = map.FindAction(actionName);
        if (action == null)
        {
            Debug.LogError($"No se encontró la acción '{actionName}'.");
            return;
        }

        System.Action<InputAction.CallbackContext> onStarted = ctx =>
        {
            if (this == null) return; // por seguridad extra
            var key = Key(ctx.control.device, color);
            pressStart[key] = Time.realtimeSinceStartupAsDouble;
            holdFired.Remove(key);

            if (holdRoutines.TryGetValue(key, out var running) && running != null)
                StopCoroutine(running);

            holdRoutines[key] = StartCoroutine(HoldWatcher(action, ctx.control.device, color, key));
        };

        System.Action<InputAction.CallbackContext> onCanceled = ctx =>
        {
            if (this == null) return;
            var device = ctx.control.device;
            var key = Key(device, color);

            if (holdRoutines.TryGetValue(key, out var running) && running != null)
                StopCoroutine(running);
            holdRoutines.Remove(key);

            if (holdFired.Contains(key))
            {
                holdFired.Remove(key);
                pressStart.Remove(key);
                return;
            }

            if (!pressStart.TryGetValue(key, out var startTime))
                return;

            pressStart.Remove(key);

            OnTap(color, device);
        };

        action.started += onStarted;
        action.canceled += onCanceled;

        startedHandlers[action] = onStarted;
        canceledHandlers[action] = onCanceled;
    }


    private IEnumerator HoldWatcher(InputAction action, InputDevice device, string color, string key)
    {
        double t0 = pressStart.TryGetValue(key, out var s) ? s : Time.realtimeSinceStartupAsDouble;

        while (action.IsPressed())
        {
            var elapsed = Time.realtimeSinceStartupAsDouble - t0;
            if (elapsed >= holdThreshold)
            {
                if (!holdFired.Contains(key))
                {
                    holdFired.Add(key);
                    OnHold(color, device);
                }
                break;
            }
            yield return null;
        }

        holdRoutines.Remove(key);
    }

    private string Key(InputDevice d, string color) => $"{d.deviceId}|{color}";

    private void OnTap(string color, InputDevice device) => RegisterOrTransferPlayer(color, device);

    private void OnHold(string color, InputDevice device)
    {
        // Solo salir si realmente estoy en ese color (según mapeo robusto)
        var cur = GetColorForDevice(device);
        if (ColorEquals(cur, color))
        {
            SetReadyInternal(color, false);

            deviceToColor.Remove(device.deviceId);
            PlayerChoices.RemovePlayer(device);
            lobbyManager.RemovePlayer(color);
        }
    }

    private bool ColorEquals(string a, string b)
        => !string.IsNullOrEmpty(a) && !string.IsNullOrEmpty(b) && a.Equals(b, System.StringComparison.OrdinalIgnoreCase);

    private void Continue(InputActionMap map, string actionName)
    {
        var action = map.FindAction(actionName);
        if (action == null)
        {
            Debug.LogError($"No se encontró la acción '{actionName}'.");
            return;
        }

        continuePerformedHandler = ctx =>
        {
            var device = ctx.control.device;
            var color = GetColorForDevice(device);

            if (string.IsNullOrEmpty(color))
            {
                if (string.IsNullOrEmpty(color))
                {
                    Debug.Log("Continue: ningún jugador asociado a este dispositivo y no hay un único activo.");
                    return;
                }
            }

            if (PlayerChoices.IsPlayerActive(color))
            {
                bool current = readyByColor.TryGetValue(color, out var v) && v;
                SetReadyInternal(color, !current);

                if (AreAllActivePlayersReady())
                    OnAllPlayersReady();
            }
        };

        action.performed += continuePerformedHandler;
    }

    // Resolución robusta del color del dispositivo
    private string GetColorForDevice(InputDevice device)
    {
        if (deviceToColor.TryGetValue(device.deviceId, out var color) && !string.IsNullOrEmpty(color))
            return color;

        // Si nuestro mapa aún no tiene el dato (p.ej. escena ya en marcha), consulta PlayerChoices
        var fromPC = PlayerChoices.GetColorFromDevice(device);
        if (!string.IsNullOrEmpty(fromPC))
        {
            deviceToColor[device.deviceId] = fromPC; // sincroniza para futuras veces
            return fromPC;
        }
        return null;
    }

    // --- READY state + luces + evento ---

    // Transfiere el dispositivo al color destino (desasigna del anterior si hay),
    // pero si el jugador actual está en "Listo", NO permite cambiar a otro color.
    private void RegisterOrTransferPlayer(string targetColor, InputDevice device)
    {
        var cur = GetColorForDevice(device);

        // Si ya estoy en el mismo color: ciclo modelo y quito "Listo" (igual que antes)
        if (ColorEquals(cur, targetColor))
        {
            lobbyManager.CyclePlayerModel(targetColor);
            SetReadyInternal(targetColor, false);
            return;
        }

        if (!string.IsNullOrEmpty(cur) && !ColorEquals(cur, targetColor) && IsReady(cur))
        {
            Debug.Log($"Cambio de color bloqueado: jugador en '{cur}' está LISTO. Desmarca 'Listo' para moverte.");
            // Aquí puedes disparar un sonido/flash de UI si quieres
            // onReadyChanged?.Invoke(cur, true); // (ejemplo: reutilizar evento)
            return;
        }

        // Si estoy en otro color pero NO listo: salgo limpiamente
        if (!string.IsNullOrEmpty(cur))
        {
            SetReadyInternal(cur, false);
            deviceToColor.Remove(device.deviceId);
            PlayerChoices.RemovePlayer(device);
            lobbyManager.RemovePlayer(cur);
        }

        // Si el color destino está libre, me uno
        if (!PlayerChoices.IsPlayerActive(targetColor))
        {
            lobbyManager.AddNewPlayer(targetColor, device);
            deviceToColor[device.deviceId] = targetColor;
            SetReadyInternal(targetColor, false);
        }
        else
        {
            Debug.Log($"Color '{targetColor}' ocupado.");
        }
    }


    private void SetReadyInternal(string color, bool isReady)
    {
        readyByColor[color] = isReady;
        SetReadyIndicator(color, isReady);
        onReadyChanged?.Invoke(color, isReady);
    }

    private void SetReadyIndicator(string color, bool on)
    {
        switch (color.ToLowerInvariant())
        {
            case "blue": if (blueReadyLight) blueReadyLight.SetActive(on); break;
            case "orange": if (orangeReadyLight) orangeReadyLight.SetActive(on); break;
            case "green": if (greenReadyLight) greenReadyLight.SetActive(on); break;
            case "yellow": if (yellowReadyLight) yellowReadyLight.SetActive(on); break;
        }
    }

    private bool AreAllActivePlayersReady()
    {
        bool anyActive = false;

        foreach (var c in COLORS)
        {
            if (PlayerChoices.IsPlayerActive(c))
            {
                anyActive = true;
                if (!readyByColor.TryGetValue(c, out var isReady) || !isReady)
                    return false;
            }
        }
        return anyActive;
    }
    private bool IsReady(string color)
    {
        return !string.IsNullOrEmpty(color)
            && readyByColor.TryGetValue(color, out var v)
            && v;
    }


    // Hook vacío para tu lógica cuando TODOS estén listos
    private void OnAllPlayersReady()
    {
        // TODO: Aquí lo que debe pasar cuando todos estén listos.
        // p.ej.: lobbyManager.Continue(); o cargar escena, etc.
        lobbyManager.Continue();
        //Debug.Log("Todos los jugadores están listos. Continuando...");
    }
}
