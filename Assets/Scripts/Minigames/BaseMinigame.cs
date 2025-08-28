using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;

public class BaseMinigame : MonoBehaviour
{
    [Header("Tutorial")]
    public GameObject tutorialPanel;

    [Header("Core Minigame")]
    public GameObject coreMinigame;

    [Header("Camera Transition")]
    public CameraTransitionManager cameraTransitionManager;

    [Header("Countdown")]
    public GameObject countdownCanvas;
    public TextMeshProUGUI countdownText;

    // Player scores
    public Dictionary<PlayerChoices.PlayerColor, int> playerScores = new();

    public int totalJugadores;
    private int jugadoresTerminados = 0;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public virtual void Start()
    {
        tutorialPanel.SetActive(true);
        coreMinigame.SetActive(true);

        if(cameraTransitionManager == null) 
            cameraTransitionManager = GetComponent<CameraTransitionManager>();
    }

    public virtual void OnAllPlayersReady() 
    { 
        tutorialPanel.SetActive(false);
    }

    public virtual void PlayerFinished(PlayerChoices.PlayerColor color, int points) 
    { 
        RegisterPlayerScore(color, points);
        jugadoresTerminados++;

        if (jugadoresTerminados >= totalJugadores)
        {
            EndMinigame();
        }
    }

    public virtual void EndMinigame() 
    {
        Debug.Log("Minigame ended");
        var scores = GetScores();

        PlayerChoices.PlayerColor ganador = PlayerChoices.PlayerColor.Azul;
        int maxPuntos = int.MinValue;

        foreach (var kvp in scores)
        {
            if (kvp.Value > maxPuntos)
            {
                maxPuntos = kvp.Value;
                ganador = kvp.Key;
            }
        }

        PlayerChoices.Instance.SetWinner(ganador);

        if (GameManager.Instance != null)
            GameManager.Instance.MiniGameFinished();
    }

    public virtual void RegisterPlayerScore(PlayerChoices.PlayerColor color, int score) 
    {
        //if (!playerScores.ContainsKey(color))
            playerScores[color] = score;
    }

    public Dictionary<PlayerChoices.PlayerColor, int> GetScores()
    {
        return playerScores;
    }
}
