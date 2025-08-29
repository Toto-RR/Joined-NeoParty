using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class Minigame_3 : BaseMinigame
{
    public static Minigame_3 Instance;

    [Header("Car Spawner")]
    public CarSpawner carSpawner;

    public bool debugMode = false;

    private void Awake()
    {
        if (debugMode)
        {
            PlayerChoices.Instance.ResetPlayers();
            PlayerChoices.AddPlayer(PlayerChoices.PlayerColor.Azul, Gamepad.all.Count > 0 ? Gamepad.all[0] : Keyboard.current);
            PlayerChoices.AddPlayer(
                PlayerChoices.PlayerColor.Naranja,
                Joystick.all.Count > 0 ? Joystick.all[0] : Keyboard.current
            );
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
        if (countdownCanvas != null)
            countdownCanvas.SetActive(true);

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

        // Desbloquea control de los jugadores
        foreach (var player in carSpawner.GetPlayers())
        {
            var ctrl = player.GetComponent<CarController>();
            if (ctrl) ctrl.enabled = true;
        }
    }
}
