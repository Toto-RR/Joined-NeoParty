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
        // Inicializa scores a 0 por cada jugador definido
        foreach (var p in PlayerChoices.GetActivePlayers())
        {
            if (!playerScores.ContainsKey(p.Color))
                playerScores[p.Color] = 0;
        }

        if (DebugMode)
        {
            PlayerChoices.Instance.ResetPlayers();
            PlayerChoices.AddPlayer(PlayerChoices.PlayerColor.Azul, Keyboard.current);
            PlayerChoices.AddPlayer(
                PlayerChoices.PlayerColor.Naranja,
                Gamepad.all.Count > 0 ? Gamepad.all[0] : Keyboard.current
            );
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
            yield return new WaitForSeconds(1f);
            countdown -= 1f;
        }

        countdownText.text = "¡YA!";
        yield return new WaitForSeconds(1f);

        if (countdownCanvas != null)
            countdownCanvas.SetActive(false);

        // Arranca el bucle de juego (oleadas/patrones/dificultad)
        if (wallSpawner.startLoopAutomaticallyAfterCountdown)
            wallSpawner.StartGame();
    }
}
