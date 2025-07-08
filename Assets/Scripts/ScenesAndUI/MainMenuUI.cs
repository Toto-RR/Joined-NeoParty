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

    public GameObject defaultButtonInicio;
    public GameObject defaultButtonPartida;
    public GameObject defaultButtonOpciones;

    public InputActionReference cancelAction;

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

    private void OnEnable()
    {
        if (cancelAction != null)
            cancelAction.action.performed += OnCancel;
    }

    private void OnDisable()
    {
        if (cancelAction != null)
            cancelAction.action.performed -= OnCancel;
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

            SelectDefaultButton(defaultButtonInicio);
        });
    }

    public void ShowPanelPartida()
    {
        canvasPartida.DOFade(0, fadeDuration).OnComplete(() =>
        {
            panelInicio.SetActive(false);
            panelPartida.SetActive(true);
            panelOpciones.SetActive(false);

            //canvasInicio.alpha = 1;
            canvasPartida.DOFade(1, fadeDuration);

            SelectDefaultButton(defaultButtonPartida);
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

            SelectDefaultButton(defaultButtonOpciones);
        });
    }

    private void SelectDefaultButton(GameObject button)
    {
        EventSystem.current.SetSelectedGameObject(null); 
        EventSystem.current.SetSelectedGameObject(button);
    }

    private void OnCancel(InputAction.CallbackContext ctx)
    {
        // Si estás en el panel de opciones, vuelve al título
        if (panelOpciones.activeSelf)
        {
            ShowPanelTitulo();
        }
        // Si estás en el panel de selección de partida, vuelve al título
        else if (panelPartida.activeSelf)
        {
            ShowPanelTitulo();
        }
    }

    public void StartPartidaCorta()
    {
        GameManager.Instance.StartGame(GameManager.GameLength.Short);
    }

    public void StartPartidaMedia()
    {
        GameManager.Instance.StartGame(GameManager.GameLength.Medium);
    }

    public void StartPartidaLarga()
    {
        GameManager.Instance.StartGame(GameManager.GameLength.Long);
    }

    public void StartPartidaMaraton()
    {
        GameManager.Instance.StartGame(GameManager.GameLength.Marathon);
    }

    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("Cerrando juego...");
    }
}
