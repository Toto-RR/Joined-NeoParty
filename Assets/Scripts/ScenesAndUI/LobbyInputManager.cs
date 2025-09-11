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

    [SerializeField] private float triggerHoldToExit = 0.8f; // segundos
    private InputAction triggerAction;
    private System.Action<InputAction.CallbackContext> triggerStartedHandler;
    private System.Action<InputAction.CallbackContext> triggerCanceledHandler;

    private double triggerPressStart;
    private Coroutine triggerHoldRoutine;
    private bool triggerHoldFired;

    // --- READY por color ---
    private readonly Dictionary<string, bool> readyByColor = new();
    private static readonly string[] COLORS = new[] { "Azul", "Naranja", "Verde", "Amarillo" };

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

    [SerializeField] private string triggerActionName = "Return";
    [SerializeField, Range(0f, 1f)] private float triggerPressPoint = 0.2f; 
    private float triggerHeldSeconds;

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

    private void Update()
    {
        if (triggerAction == null) return;

        // Lee el valor continuo del gatillo: 0..1 en gamepad; 0/1 en teclas.
        float v = triggerAction.ReadValue<float>();

        if (v >= triggerPressPoint)
        {
            triggerHeldSeconds += Time.unscaledDeltaTime;

            if (!triggerHoldFired && triggerHeldSeconds >= triggerHoldToExit)
            {
                triggerHoldFired = true;

                // Solo permite salir si no hay nadie unido al lobby
                if (PlayerChoices.GetNumberOfPlayers() == 0)
                {
                    lobbyManager.GoBackToPreviousScene();
                }
            }
        }
        else
        {
            // Se soltó: resetea acumulador y permiso de disparo
            triggerHeldSeconds = 0f;
            triggerHoldFired = false;
        }
    }

    private void OnDestroy()
    {
        OnDisable(); // por si acaso
    }

    private void AttachBindings(InputActionMap map)
    {
        BindWithHold(map, "JoinBlue", "Azul");
        BindWithHold(map, "JoinOrange", "Naranja");
        BindWithHold(map, "JoinGreen", "Verde");
        BindWithHold(map, "JoinYellow", "Amarillo");
        AttachTriggerHold(map);
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

        if (triggerAction != null)
        {
            if (triggerStartedHandler != null) triggerAction.started -= triggerStartedHandler;
            if (triggerCanceledHandler != null) triggerAction.canceled -= triggerCanceledHandler;
        }
        triggerAction = null;
        triggerStartedHandler = null;
        triggerCanceledHandler = null;

        if (triggerHoldRoutine != null) StopCoroutine(triggerHoldRoutine);
        triggerHoldRoutine = null;
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
                
                if(!current) SoundManager.PlayFX(7); // Lobby_Ready
                else SoundManager.PlayFX(6); // Lobby_NotReady

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
            SoundManager.PlayFX(3); // sonido de unión (AddPlayer_Lobby)
            return;
        }

        if (!string.IsNullOrEmpty(cur) && !ColorEquals(cur, targetColor) && IsReady(cur))
        {
            Debug.Log($"Cambio de color bloqueado: jugador en '{cur}' está LISTO. Desmarca 'Listo' para moverte.");
            return;
        }

        // Si estoy en otro color pero NO listo: salgo limpiamente
        if (!string.IsNullOrEmpty(cur))
        {
            SetReadyInternal(cur, false);
            deviceToColor.Remove(device.deviceId);
            PlayerChoices.RemovePlayer(device);
            lobbyManager.RemovePlayer(cur);
            SoundManager.PlayFX(3); // sonido de unión (AddPlayer_Lobby)
        }

        // Si el color destino está libre, me uno
        if (!PlayerChoices.IsPlayerActive(targetColor))
        {
            bool wasZero = PlayerChoices.GetNumberOfPlayers() == 0;

            lobbyManager.AddNewPlayer(targetColor, device);
            deviceToColor[device.deviceId] = targetColor;
            SetReadyInternal(targetColor, false);

            SoundManager.PlayFX(3); // sonido de unión (AddPlayer_Lobby)
        }
        else
        {
            Debug.Log($"Color '{targetColor}' ocupado.");
        }
    }

    private void SetReadyInternal(string color, bool isReady)
    {
        bool prevReady = readyByColor.TryGetValue(color, out var prev) && prev;

        // Si no hay cambio, no toques la UI
        if (prevReady == isReady)
        {
            readyByColor[color] = isReady;            // mantiene el diccionario coherente
            SetReadyIndicator(color, isReady);        // opcional: mantener luces sincronizadas
            onReadyChanged?.Invoke(color, isReady);   // si quieres seguir notificando sin UI
            return;
        }

        // Hay cambio real de estado
        readyByColor[color] = isReady;
        SetReadyIndicator(color, isReady);
        onReadyChanged?.Invoke(color, isReady);
        lobbyManager.OnPlayerReadyChanged(color, isReady);
    }

    private void SetReadyIndicator(string color, bool on)
    {
        switch (color.ToLowerInvariant())
        {
            case "azul": if (blueReadyLight) blueReadyLight.SetActive(on); break;
            case "naranja": if (orangeReadyLight) orangeReadyLight.SetActive(on); break;
            case "verde": if (greenReadyLight) greenReadyLight.SetActive(on); break;
            case "amarillo": if (yellowReadyLight) yellowReadyLight.SetActive(on); break;
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

    private void OnAllPlayersReady()
    {
        lobbyManager.Continue();
    }

    private void AttachTriggerHold(InputActionMap map)
    {
        triggerAction = map.FindAction(triggerActionName, throwIfNotFound: false);
        if (triggerAction == null)
        {
            Debug.LogWarning($"No se encontró la acción '{triggerActionName}' en CharacterSelector (gatillo para volver).");
            return;
        }

        // Si tu acción ES de tipo Button, estos eventos ayudan; si es Value, el Update (abajo) hará el trabajo.
        if (triggerAction.type == InputActionType.Button)
        {
            triggerStartedHandler = ctx =>
            {
                triggerPressStart = Time.realtimeSinceStartupAsDouble;
                triggerHoldFired = false;
                if (triggerHoldRoutine != null) StopCoroutine(triggerHoldRoutine);
                triggerHoldRoutine = StartCoroutine(Co_TriggerHoldWatcher());
            };

            triggerCanceledHandler = ctx =>
            {
                if (triggerHoldRoutine != null) StopCoroutine(triggerHoldRoutine);
                triggerHoldRoutine = null;
                triggerPressStart = 0;
                triggerHoldFired = false;
            };

            triggerAction.started += triggerStartedHandler;
            triggerAction.canceled += triggerCanceledHandler;
        }
    }

    private IEnumerator Co_TriggerHoldWatcher()
    {
        while (triggerAction != null && triggerAction.IsPressed())
        {
            var elapsed = Time.realtimeSinceStartupAsDouble - triggerPressStart;
            if (!triggerHoldFired && elapsed >= triggerHoldToExit)
            {
                triggerHoldFired = true;

                // condición: nadie unido
                if (PlayerChoices.GetNumberOfPlayers() == 0)
                {
                    // salir al menú/anterior
                    lobbyManager.GoBackToPreviousScene();
                }
                break;
            }
            yield return null;
        }
        triggerHoldRoutine = null;
    }

}
