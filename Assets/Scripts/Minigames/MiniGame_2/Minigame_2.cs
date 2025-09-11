using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class Minigame_2 : BaseMinigame
{
    public static Minigame_2 Instance;

    [Header("Matrix Control")]
    public MatrixHoleSystem matrixSystem;

    [Header("Wall Spawner")]
    public WallSpawner wallSpawner;

    [Header("Laser Pointer")]
    public LaserPointerMover laserPointer;

    [Header("Drone Control")]
    public DroneSpawner droneSpawner;

    [Header("Calibration HUD")]
    public Canvas calibrationCanvas;                 // tu canvas de calibración (el que tiene el HLayout como primer hijo)
    public UnityEngine.UI.Image readyIconTemplate;   // imagen a instanciar (desactivada en escena)
    [Range(0f, 1f)] public float pendingAlpha = 0.5f;
    [Range(0f, 1f)] public float readyAlpha = 1.0f;

    private readonly Dictionary<DroneController, bool> _calibration = new();
    private readonly Dictionary<DroneController, UnityEngine.UI.Image> _iconByDrone = new();
    private bool _countdownStarted = false;


    public bool DebugMode = false;
    private void Awake()
    {
        if (DebugMode)
        {
            PlayerChoices.Instance.ResetPlayers();
            PlayerChoices.AddPlayer(PlayerChoices.PlayerColor.Azul, Gamepad.all.Count > 0 ? Gamepad.all[0] : Keyboard.current);
            PlayerChoices.AddPlayer(PlayerChoices.PlayerColor.Naranja,
                Joystick.all.Count > 0 ? Joystick.all[0] : Keyboard.current
            );
        }

        wallSpawner.Setup(matrixSystem, laserPointer);

        calibrationCanvas.enabled = false;
        Instance = this;
    }

    public override void OnAllPlayersReady()
    {
        base.OnAllPlayersReady();

        droneSpawner.SpawnDrone();

        cameraTransitionManager.SwitchToGameplay(() =>
        {
            totalJugadores = PlayerChoices.GetNumberOfPlayers();

            foreach (var player in droneSpawner.GetPlayers())
            {
                var ctrl = player.GetComponent<DroneController>();
                if (!ctrl) continue;

                ctrl.enabled = true; 
                ctrl.BeginCalibrationInputGate(); 
                _calibration[ctrl] = ctrl.CalibrationReady;

                ctrl.OnCalibrationToggled -= OnDroneCalibrationToggled;
                ctrl.OnCalibrationToggled += OnDroneCalibrationToggled;
            }

            BuildCalibrationIcons();
            if (calibrationCanvas) calibrationCanvas.enabled = true;
        });
    }

    private void OnDroneCalibrationToggled(DroneController dc, bool ready)
    {
        if (_countdownStarted) return;

        if (_calibration.ContainsKey(dc))
            _calibration[dc] = ready;

        if (_iconByDrone.TryGetValue(dc, out var img) && img)
        {
            var c = img.color;
            c.a = ready ? readyAlpha : pendingAlpha;
            img.color = c;
        }

        foreach (var kv in _calibration)
            if (!kv.Value) return;

        _countdownStarted = true;

        foreach (var p in _calibration.Keys)
            p.OnCalibrationToggled -= OnDroneCalibrationToggled;

        if (calibrationCanvas) calibrationCanvas.enabled = false;

        StartCoroutine(StartCountdownThenBegin());
    }

    private IEnumerator StartCountdownThenBegin()
    {
        SoundManager.FadeOutMusic(1f);

        if (countdownCanvas != null)
            countdownCanvas.SetActive(true);

        float countdown = 3f;
        while (countdown > 0)
        {
            countdownText.text = Mathf.Ceil(countdown).ToString();
            SoundManager.PlayFX(12);
            yield return new WaitForSeconds(1f);
            countdown -= 1f;
        }

        countdownText.text = "¡YA!";
        SoundManager.PlayFX(13);
        yield return new WaitForSeconds(1f);

        if (countdownCanvas != null)
            countdownCanvas.SetActive(false);

        SoundManager.PlayMusicWithFade(9); // HoverChamps_Theme

        // Arranca el bucle de juego (oleadas/patrones/dificultad)
        if (wallSpawner.startLoopAutomaticallyAfterCountdown)
            wallSpawner.StartGame();
    }

    private void BuildCalibrationIcons()
    {
        if (!calibrationCanvas || !readyIconTemplate)
        {
            Debug.LogWarning("[Minigame_2] Falta calibrationCanvas o readyIconTemplate.");
            return;
        }

        var root = calibrationCanvas.transform;
        if (root.childCount == 0)
        {
            Debug.LogWarning("[Minigame_2] El canvas no tiene hijos; coloca tu HorizontalLayout como primer hijo.");
            return;
        }

        var container = root.GetChild(0); // Horizontal Layout Group
                                          // Limpia instancias anteriores (mantén la plantilla si está dentro de ese mismo contenedor)
        for (int i = container.childCount - 1; i >= 0; i--)
        {
            var child = container.GetChild(i);
            if (child.gameObject != readyIconTemplate.gameObject)
                Destroy(child.gameObject);
        }

        readyIconTemplate.gameObject.SetActive(false);
        _iconByDrone.Clear();

        var players = droneSpawner.GetPlayers();
        foreach (var p in players)
        {
            var icon = Instantiate(readyIconTemplate, container);
            icon.gameObject.SetActive(true);

            // Color del jugador con alpha pendiente
            var c = PlayerChoices.GetColorRGBA(p.pColor);
            c.a = pendingAlpha;
            icon.color = c;

            _iconByDrone[p] = icon;
        }
    }

}
