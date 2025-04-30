using UnityEngine;
using System.Collections;

public class Minigame_1 : MonoBehaviour
{
    // ESTO IRA EN EL SCRIPT BASE DEL QUE TODOS LOS MINIJUEGOS HEREDARÁN
    [Header("Canvases")]
    public GameObject tutorialCanvas;
    public GameObject gameplayCanvas;
    public GameObject endCanvas;

    // --- Parameters ---
    public PlayersSpawner playersSpawner;
    public CameraTransitionManager cameraTransitionManager;
    public Canvas hudCanvas;

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
            StartCoroutine(HandlePostTransition());
        });
    }
    private IEnumerator HandlePostTransition()
    {
        yield return StartCoroutine(playersSpawner.ExpandCameras(playersSpawner.GetPlayerCameras()));

        BlackoutMainCamera();
        ActivateGameCanvas();
    }
    private void ActivateGameCanvas()
    {
        hudCanvas.enabled = true;
    }

    public virtual void OnGameFinished()
    {
        gameplayCanvas.SetActive(false);
        endCanvas.SetActive(true);
    }

    public virtual void OnEndConfirmed()
    {
        //FindObjectOfType<MiniGameManager>().OnMiniGameEnded();
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
}