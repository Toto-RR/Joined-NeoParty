using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class Minigame_1 : BaseMinigame
{
    public static Minigame_1 Instance;

    [Header("Gameplay Control")]
    public List<ObstacleSpawner> obstacleSpawners = new();
    public PlayersSpawner playersSpawner;

    public Canvas hudCanvas;

    public TextMeshProUGUI winnerText;
    
    public bool debugMode = false;

    private void Awake()
    {
        if (debugMode)
        {
            PlayerChoices.Instance.ResetPlayers();
            PlayerChoices.AddPlayer(PlayerChoices.PlayerColor.Azul, Keyboard.current);
            PlayerChoices.AddPlayer(PlayerChoices.PlayerColor.Naranja, Gamepad.all.Count > 0 ? Gamepad.all[0] : Keyboard.current);
        }
        Instance = this;
    }

    public override void OnAllPlayersReady()
    {
        base.OnAllPlayersReady();

        base.cameraTransitionManager.SwitchToGameplay(() =>
        {
            playersSpawner.SpawnPlayers();
            totalJugadores = PlayerChoices.GetNumberOfPlayers();
            StartCoroutine(HandlePostTransition());
        });
    }

    private IEnumerator HandlePostTransition()
    {
        yield return StartCoroutine(playersSpawner.ExpandCameras(playersSpawner.GetPlayerCameras()));

        BlackoutMainCamera();
        ActivateGameCanvas();

        yield return StartCoroutine(StartCountdownThenBegin());
    }

    private void ActivateGameCanvas()
    {
        hudCanvas.enabled = true;
    }

    private void BlackoutMainCamera()
    {
        // Luego limpia MainCamera:
        var mainCam = Camera.main;

        if (mainCam != null)
        {
            mainCam.clearFlags = CameraClearFlags.SolidColor;
            mainCam.backgroundColor = Color.black;
            mainCam.cullingMask = 0;
        }
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

        // Activar spawner y desbloquear animaciones
        foreach (var spawner in obstacleSpawners)
        {
            if (spawner != null)
                spawner.enabled = true;
        }

        SoundManager.PlayMusicWithFade(1);

        foreach (var player in playersSpawner.GetPlayers())
        {
            player.GetComponent<PlayerController>().enabled = true;
        }
    }

}