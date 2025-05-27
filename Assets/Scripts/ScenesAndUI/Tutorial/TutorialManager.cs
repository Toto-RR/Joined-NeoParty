using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Splines;
using UnityEngine.Video;
using UnityEngine.InputSystem;

public class TutorialManager : MonoBehaviour
{
    [Header("Setup")]
    public Transform playersPanel;              // Panel donde instanciar los PlayerRow
    public GameObject playerRowPrefab;          // Prefab de la fila de jugador
    public Minigame_1 minigameBase;           // Referencia al minijuego actual

    private int readyPlayers = 0;
    private int totalPlayers = 0;
    private List<PlayerRow> playerRows = new();

    [Header("Cinemachine Intro")]
    public SplineAnimate splineAnimate;

    public VideoPlayer videoTutorial;

    private void Start()
    {
        totalPlayers = PlayerChoices.GetNumberOfPlayers();
        SetupPlayerRows();

        splineAnimate.Completed += OnIntroAnimationFinished;
    }

    private void OnDestroy()
    {
        if (splineAnimate != null)
            splineAnimate.Completed -= OnIntroAnimationFinished;
    }

    protected virtual void StartIntroAnimation()
    {
        if (splineAnimate != null)
        {
            splineAnimate.Play();
        }
    }

    protected virtual void OnIntroAnimationFinished()
    {
        videoTutorial.Play();
    }

    private void SetupPlayerRows()
    {
        foreach (var player in PlayerChoices.GetActivePlayers())
        {
            GameObject rowObj = Instantiate(playerRowPrefab, playersPanel);
            PlayerRow row = rowObj.GetComponent<PlayerRow>();

            row.Setup(player, this);
            playerRows.Add(row);
        }
    }

    public void PlayerReady(PlayerRow row)
    {
        if (!row.isReady)
        {
            row.SetReady();
            readyPlayers++;

            if (readyPlayers >= totalPlayers)
            {
                Debug.Log("Todos los jugadores listos en el tutorial.");
                minigameBase.OnAllPlayersReady(); // Avisa al minijuego
            }
        }
    }
}
