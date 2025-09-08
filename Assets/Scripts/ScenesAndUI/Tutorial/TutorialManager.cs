using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Splines;
using UnityEngine.Video;
using UnityEngine.UI;

public class TutorialManager : MonoBehaviour
{
    [Header("Setup")]
    public Transform playersPanel;
    public GameObject playerRowPrefab;
    public BaseMinigame minigameBase;

    private int totalPlayers = 0;
    private readonly List<PlayerRow> playerRows = new();
    private readonly HashSet<PlayerRow> readySet = new();
    private bool allReadyFired = false;

    [Header("Cinemachine Intro")]
    public SplineAnimate splineAnimate;

    public VideoPlayer videoTutorial;

    private void Start()
    {
        totalPlayers = PlayerChoices.GetNumberOfPlayers();
        SetupPlayerRows();

        splineAnimate.Completed += OnIntroAnimationFinished;
        videoTutorial.playOnAwake = false;
        if (videoTutorial.isPlaying) videoTutorial.Stop();

        if (splineAnimate != null)
            StartIntroAnimation();
        else
            OnIntroAnimationFinished();

        SoundManager.PlayMusic(5);
    }

    private void OnDestroy()
    {
        if (splineAnimate != null)
            splineAnimate.Completed -= OnIntroAnimationFinished;
    }

    protected virtual void StartIntroAnimation()
    {
        if (splineAnimate != null) splineAnimate.Play();
        TogglePlayerRows(false);
    }

    protected virtual void OnIntroAnimationFinished()
    {
        if(videoTutorial.isActiveAndEnabled) 
            videoTutorial.Play();

        TogglePlayerRows(true);
    }

    private void SetupPlayerRows()
    {
        foreach (var player in PlayerChoices.GetActivePlayers())
        {
            GameObject rowObj = Instantiate(playerRowPrefab, playersPanel);
            rowObj.GetComponent<Image>().color = PlayerChoices.GetColorRGBA(player.Color);

            PlayerRow row = rowObj.GetComponent<PlayerRow>();
            row.Setup(player, this); // PlayerRow enlaza su input y nos notificará cambios
            playerRows.Add(row);
        }
    }

    private void TogglePlayerRows(bool enable)
    {
        foreach (PlayerRow row in playerRows)
        {
            row.ToggleMap(enable);
        }
    }

    // NUEVO: se llama tanto al marcar listo como al desmarcar
    public void OnPlayerReadyChanged(PlayerRow row, bool isReady)
    {
        if (isReady) readySet.Add(row);
        else readySet.Remove(row);

        // dispara solo una vez cuando todos estén listos
        if (!allReadyFired && readySet.Count >= totalPlayers && totalPlayers > 0)
        {
            allReadyFired = true;
            Debug.Log("Todos los jugadores listos en el tutorial.");
            minigameBase.OnAllPlayersReady();
        }
    }

    public void UnregisterRow(PlayerRow row)
    {
        readySet.Remove(row);
        playerRows.Remove(row);
        totalPlayers = Mathf.Max(0, totalPlayers - 1);
        allReadyFired = false;
    }
}
