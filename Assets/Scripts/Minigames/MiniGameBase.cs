using UnityEngine;

public class MiniGameBase : MonoBehaviour
{
    public GameObject tutorialCanvas;
    public GameObject gameplayCanvas;
    public GameObject endCanvas;

    protected virtual void Start()
    {
        tutorialCanvas.SetActive(true);
        gameplayCanvas.SetActive(false);
        endCanvas.SetActive(false);

        StartIntroAnimation();
    }

    protected virtual void StartIntroAnimation()
    {
        // Aquí lanzas animación de cámara propia
        // y cuando termine muestras el tutorialCanvas
    }

    public virtual void OnAllPlayersReady()
    {
        tutorialCanvas.SetActive(false);
        gameplayCanvas.SetActive(true);
    }

    public virtual void OnGameFinished()
    {
        gameplayCanvas.SetActive(false);
        endCanvas.SetActive(true);
    }

    public virtual void OnEndConfirmed()
    {
        // Avisar al MiniGameManager
        FindObjectOfType<MiniGameManager>().OnMiniGameEnded();
    }
}
