using DG.Tweening;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class MainMenuUI : MonoBehaviour
{
    public GameObject panelInicio;
    public GameObject panelPartida;
    public GameObject panelOpciones;
    public float fadeDuration = 0.2f;

    private CanvasGroup canvasInicio;
    private CanvasGroup canvasPartida;
    private CanvasGroup canvasOpciones;

    private void Awake()
    {
        canvasInicio = panelInicio.GetComponent<CanvasGroup>();
        canvasPartida = panelPartida.GetComponent<CanvasGroup>();
        canvasOpciones = panelOpciones.GetComponent<CanvasGroup>();
    }

    private void Start()
    {
        ShowPanelTitulo();
    }

    public void ShowPanelTitulo()
    {
        canvasInicio.DOFade(0, fadeDuration).OnComplete(() =>
        {
            panelInicio.SetActive(true);
            panelPartida.SetActive(false);
            panelOpciones.SetActive(false);

            canvasPartida.alpha = 1;
            canvasInicio.DOFade(1, fadeDuration);
            MenuInputRouter.Instance?.SetProvider(panelInicio.GetComponent<CanvasActionsProvider>());
        });
    }

    public void ShowPanelPartida()
    {
        canvasPartida.DOFade(0, fadeDuration).OnComplete(() =>
        {
            panelInicio.SetActive(false);
            panelPartida.SetActive(true);
            panelOpciones.SetActive(false);

            canvasPartida.DOFade(1, fadeDuration);
            MenuInputRouter.Instance?.SetProvider(panelPartida.GetComponent<CanvasActionsProvider>());
        });
    }

    public void ShowPanelOpciones()
    {
        canvasOpciones.DOFade(0, fadeDuration).OnComplete(() =>
        {
            panelInicio.SetActive(false);
            panelPartida.SetActive(false);
            panelOpciones.SetActive(true);

            canvasPartida.alpha = 0;
            canvasOpciones.DOFade(1, fadeDuration);
            MenuInputRouter.Instance?.SetProvider(panelOpciones.GetComponent<CanvasActionsProvider>());
        });
    }

    public void StartPartidaCorta()
    {
        GameManager.Instance.StartGame(GameManager.GameLength.Short);
        MenuInputRouter.Instance?.ClearProvider(panelPartida.GetComponent<CanvasActionsProvider>());
    }

    public void StartPartidaMedia()
    {
        GameManager.Instance.StartGame(GameManager.GameLength.Medium);
        MenuInputRouter.Instance?.ClearProvider(panelPartida.GetComponent<CanvasActionsProvider>());
    }

    public void StartPartidaLarga()
    {
        GameManager.Instance.StartGame(GameManager.GameLength.Long);
        MenuInputRouter.Instance?.ClearProvider(panelPartida.GetComponent<CanvasActionsProvider>());
    }

    public void StartPartidaMaraton()
    {
        GameManager.Instance.StartGame(GameManager.GameLength.Marathon);
        MenuInputRouter.Instance?.ClearProvider(panelPartida.GetComponent<CanvasActionsProvider>());
    }

    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("Cerrando juego...");
    }
}
