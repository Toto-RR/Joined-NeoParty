using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Minigame_3 : BaseMinigame
{
    public static Minigame_3 Instance;

    [Header("Car Spawner")]
    public CarSpawner carSpawner;

    public bool debugMode = false;
    
    private readonly List<PlayerChoices.PlayerColor> finishOrder = new();

    private void Awake()
    {
        if (debugMode)
        {
            PlayerChoices.Instance.ResetPlayers();
            PlayerChoices.AddPlayer(PlayerChoices.PlayerColor.Azul, Gamepad.all.Count > 0 ? Gamepad.all[0] : Keyboard.current);
            PlayerChoices.AddPlayer(PlayerChoices.PlayerColor.Verde, Keyboard.current);
            //PlayerChoices.AddPlayer(PlayerChoices.PlayerColor.Amarillo, Gamepad.all.Count > 0 ? Gamepad.all[1] : Keyboard.current);
            //PlayerChoices.AddPlayer(
            //    PlayerChoices.PlayerColor.Naranja,
            //    Joystick.all.Count > 0 ? Joystick.all[0] : Keyboard.current
            //);
        } 

        Instance = this;
    }

    public override void OnAllPlayersReady()
    {
        base.OnAllPlayersReady();

        carSpawner.SpawnCars();

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

        SoundManager.PlayMusicWithFade(3);

        // Desbloquea control de los jugadores
        foreach (var player in carSpawner.GetPlayers())
        {
            var ctrl = player.GetComponent<CarController>();
            if (ctrl) ctrl.enabled = true;
        }
    }

    public void RegisterPlayerFinish(PlayerChoices.PlayerColor color)
    {
        // Evitar dobles registros
        if (finishOrder.Contains(color)) return;

        finishOrder.Add(color);

        int position = finishOrder.Count; // 1-based
        int points = Mathf.Clamp(totalJugadores - (position - 1), 1, totalJugadores);

        PlayerFinished(color, points);
    }
}
