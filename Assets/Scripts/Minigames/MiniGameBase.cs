using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.Splines;

public class MiniGameBase : MonoBehaviour
{
    [Header("Canvases")]
    public GameObject tutorialCanvas;
    public GameObject gameplayCanvas;
    public GameObject endCanvas;

    //HARDCODED, QUITAR
    public PlayersSpawner playersSpawner;
    public CameraTransitionManager cameraTransitionManager;

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
            StartCoroutine(playersSpawner.ExpandCameras(playersSpawner.GetPlayerCameras()));
        });
    }

    public virtual void OnGameFinished()
    {
        gameplayCanvas.SetActive(false);
        endCanvas.SetActive(true);
    }

    public virtual void OnEndConfirmed()
    {
        FindObjectOfType<MiniGameManager>().OnMiniGameEnded();
    }
}