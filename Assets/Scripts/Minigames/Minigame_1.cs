using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class Minigame_1 : MonoBehaviour
{
    public static Minigame_1 Instance;

    // ESTO IRA EN EL SCRIPT BASE DEL QUE TODOS LOS MINIJUEGOS HEREDARÁN
    [Header("Canvases")]
    public GameObject tutorialCanvas;
    public GameObject gameplayCanvas;
    public GameObject endCanvas;

    [Header("Gameplay Control")]
    public List<ObstacleSpawner> obstacleSpawners = new();
    public GameObject countdownCanvas;
    public TextMeshProUGUI countdownText;

    // --- Parameters ---
    public PlayersSpawner playersSpawner;
    public CameraTransitionManager cameraTransitionManager;
    public Canvas hudCanvas;

    public TextMeshProUGUI winnerText;
    
    private int jugadoresTerminados = 0;
    private int totalJugadores;

    private Dictionary<PlayerChoices.PlayerColor, int> playerScores = new();

    private void Awake()
    {
#if UNITY_EDITOR
        if(PlayerChoices.GetNumberOfPlayers() <= 0)
        {
            PlayerChoices.AddPlayer(PlayerChoices.PlayerColor.Blue, Keyboard.current);
            PlayerChoices.AddPlayer(PlayerChoices.PlayerColor.Orange, Gamepad.all.Count > 0 ? Gamepad.all[0] : Keyboard.current);
        }
#endif
        Instance = this;
    }

    protected virtual void Start()
    {
        tutorialCanvas.SetActive(true);
        gameplayCanvas.SetActive(true);
        endCanvas.SetActive(false);
    }

    protected virtual void Update()
    {

    }

    public virtual void OnAllPlayersReady()
    {
        tutorialCanvas.SetActive(false);

        cameraTransitionManager.SwitchToGameplay(() =>
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

    public void PlayerFinished(PlayerChoices.PlayerColor color, int puntos)
    {
        RegisterPlayerScore(color, puntos);
        jugadoresTerminados++;

        if (jugadoresTerminados >= totalJugadores)
        {
            OnGameFinished();
        }
    }

    public void OnGameFinished()
    {
        endCanvas.SetActive(true);

        var scores = GetScores();

        // Buscar al jugador con más puntos
        PlayerChoices.PlayerColor ganador = PlayerChoices.PlayerColor.Blue;
        int maxPuntos = int.MinValue;

        foreach (var kvp in scores)
        {
            if (kvp.Value > maxPuntos)
            {
                maxPuntos = kvp.Value;
                ganador = kvp.Key;
            }
        }

        // Mostrar texto
        if (winnerText != null)
        {
            winnerText.text = $"¡{ganador}!";
        }
    }

    public virtual void OnEndConfirmed()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
    }

    public void RegisterPlayerScore(PlayerChoices.PlayerColor color, int score)
    {
        if (!playerScores.ContainsKey(color))
            playerScores[color] = score;
    }

    public Dictionary<PlayerChoices.PlayerColor, int> GetScores()
    {
        return playerScores;
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

        // Activar spawner y desbloquear animaciones
        foreach (var spawner in obstacleSpawners)
        {
            if (spawner != null)
                spawner.enabled = true;
        }

        foreach (var player in playersSpawner.GetPlayers())
        {
            player.GetComponent<PlayerController>().enabled = true;
        }
    }

}