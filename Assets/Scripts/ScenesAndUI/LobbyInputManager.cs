using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Collections;
using System.Drawing;

public class LobbyInputManager : MonoBehaviour
{
    private LobbyManager lobbyManager;
    public InputActionAsset actions;

    [SerializeField] private float holdThreshold = 0.5f;
    private readonly Dictionary<string, double> pressStart = new();

    private void OnEnable()
    {
        lobbyManager = GetComponent<LobbyManager>();
        if (lobbyManager == null)
            Debug.Log("LobbyManager no encontrado");

        var map = actions.FindActionMap("CharacterSelector");
        if (map == null)
        {
            Debug.LogError("No se encontró el ActionMap 'CharacterSelector'.");
            return;
        }

        BindWithHold(map, "JoinBlue", "Blue");
        BindWithHold(map, "JoinOrange", "Orange");
        BindWithHold(map, "JoinGreen", "Green");
        BindWithHold(map, "JoinYellow", "Yellow");

        Continue(map, "Continue");

        map.Enable();
    }

    private readonly HashSet<string> holdFired = new();                 // Para saber si ya se lanzó el hold
    private readonly Dictionary<string, Coroutine> holdRoutines = new(); // Para parar limpito

    private void BindWithHold(InputActionMap map, string actionName, string color)
    {
        var action = map.FindAction(actionName);
        if (action == null)
        {
            Debug.LogError($"No se encontró la acción '{actionName}'.");
            return;
        }

        action.started += ctx =>
        {
            var key = Key(ctx.control.device, color);
            pressStart[key] = Time.realtimeSinceStartupAsDouble;
            holdFired.Remove(key);

            // Arrancamos el watcher que dispara el HOLD al cumplirse el umbral
            if (holdRoutines.TryGetValue(key, out var running) && running != null)
                StopCoroutine(running);
            holdRoutines[key] = StartCoroutine(HoldWatcher(action, ctx.control.device, color, key));
        };

        action.canceled += ctx =>
        {
            var device = ctx.control.device;
            var key = Key(device, color);

            // Paramos y limpiamos el watcher si seguía vivo
            if (holdRoutines.TryGetValue(key, out var running) && running != null)
                StopCoroutine(running);
            holdRoutines.Remove(key);

            // Si ya se disparó el HOLD, no hacemos TAP
            if (holdFired.Contains(key))
            {
                holdFired.Remove(key);
                pressStart.Remove(key);
                return;
            }

            // TAP (solo si no hubo hold)
            if (!pressStart.TryGetValue(key, out var startTime))
                return;

            var duration = Time.realtimeSinceStartupAsDouble - startTime;
            pressStart.Remove(key);

            // Por si acaso: el umbral ya lo gestiona el watcher; aquí solo tap
            OnTap(color, device);
        };
    }

    private IEnumerator HoldWatcher(InputAction action, InputDevice device, string color, string key)
    {
        double t0 = pressStart.TryGetValue(key, out var s) ? s : Time.realtimeSinceStartupAsDouble;

        // Mientras el botón siga presionado, esperamos al umbral
        while (action.IsPressed()) // equivalente a leer >0
        {
            var elapsed = Time.realtimeSinceStartupAsDouble - t0;
            if (elapsed >= holdThreshold)
            {
                if (!holdFired.Contains(key))
                {
                    holdFired.Add(key);
                    OnHold(color, device);   // Se dispara el HOLD aquí, sin soltar
                }
                break; // Salimos: ya no necesitamos seguir chequeando
            }
            yield return null;
        }

        // Limpieza: la rutina terminó por hold o por soltar antes de tiempo (canceled hará el tap)
        holdRoutines.Remove(key);
    }

    private string Key(InputDevice d, string color) => $"{d.deviceId}|{color}";

    private void OnTap(string color, InputDevice device)
    {
        if (PlayerChoices.IsPlayerActive(device))
        {
            // Si el jugador ya está en ese color -> cambiar modelo
            if (PlayerChoices.GetColorFromDevice(device).Equals(color, System.StringComparison.OrdinalIgnoreCase))
            {
                lobbyManager.CyclePlayerModel(color);
            }
        }
        else
        {
            // Si el color está libre -> unirse
            if (!PlayerChoices.IsPlayerActive(color))
            {
                RegisterPlayer(color, device);
            }
        }
    }

    private void OnHold(string color, InputDevice device)
    {
        // Si está en ese color -> salir
        if (PlayerChoices.GetColorFromDevice(device)?.Equals(color, System.StringComparison.OrdinalIgnoreCase) == true)
        {
            PlayerChoices.RemovePlayer(device);
            lobbyManager.RemovePlayer(color);
        }
    }

    void RegisterPlayer(string colorName, InputDevice device)
    {
        lobbyManager.AddNewPlayer(colorName, device);
    }

    private void Continue(InputActionMap map, string actionName)
    {
        var action = map.FindAction(actionName);
        if (action == null)
        {
            Debug.LogError($"No se encontró la acción '{actionName}'.");
            return;
        }

        action.started += ctx =>
        {
        
        };

        action.canceled += ctx =>
        {
            Debug.Log("Pasando a escena de juego...");
            lobbyManager.Continue();
        };
    }
}
