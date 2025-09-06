using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

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

    public Canvas hudCanvas;
    public bool DebugMode = false;
    private void Awake()
    {
        if (DebugMode)
        {
            PlayerChoices.Instance.ResetPlayers();
            PlayerChoices.AddPlayer(PlayerChoices.PlayerColor.Azul, Joystick.all.Count > 0 ? Joystick.all[0] : Keyboard.current);
            //PlayerChoices.AddPlayer(PlayerChoices.PlayerColor.Naranja,
            //    Joystick.all.Count > 0 ? Joystick.all[0] : Keyboard.current
            //);
        }

        wallSpawner.Setup(matrixSystem, laserPointer);

        Instance = this;
    }

    public override void OnAllPlayersReady()
    {
        base.OnAllPlayersReady();

        droneSpawner.SpawnDrone();

        cameraTransitionManager.SwitchToGameplay(() =>
        {
            totalJugadores = PlayerChoices.GetNumberOfPlayers();
            StartCoroutine(HandlePostTransition());
        });
    }

    public IEnumerator HandlePostTransition()
    {
        yield return new WaitForSeconds(0.5f);

        yield return StartCoroutine(StartCountdownThenBegin());
    }

    private IEnumerator StartCountdownThenBegin()
    {
        SoundManager.FadeOutMusic(1f);

        if (countdownCanvas != null)
            countdownCanvas.SetActive(true);

        // Desbloquea control de los jugadores
        foreach (var player in droneSpawner.GetPlayers())
        {
            var ctrl = player.GetComponent<DroneController>();
            if (ctrl) ctrl.enabled = true;
        }

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

        SoundManager.PlayMusicWithFade(2);

        // Arranca el bucle de juego (oleadas/patrones/dificultad)
        if (wallSpawner.startLoopAutomaticallyAfterCountdown)
            wallSpawner.StartGame();
    }
}
