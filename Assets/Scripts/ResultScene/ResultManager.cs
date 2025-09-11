using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem; // <-- NUEVO
using UnityEngine.Rendering;

public class ResultManager : MonoBehaviour
{
    [Header("Player Base")]
    public GameObject basePlayer;

    [Header("Lights")]
    public List<Light> lights = new();

    [Header("UI Elements")]
    public TextMeshPro winnerText;

    [Header("Continue Prompt")]
    public Canvas continuePrompt;

    [Header("Input")]
    [Tooltip("Arrastra aquí la acción 'Ready' del action map 'Tutorial' (Input System).")]
    public InputActionReference readyAction; // <-- NUEVO

    private Animator animator;
    private bool promptShown = false;
    private bool advanced = false; // para evitar dobles disparos

    private void Awake()
    {
        animator = basePlayer.GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError("Animator component not found on playerBase.");
        }
    }

    private void OnEnable()
    {
        if (readyAction != null && readyAction.action != null)
        {
            readyAction.action.performed += OnReady;
            readyAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (readyAction != null && readyAction.action != null)
        {
            readyAction.action.performed -= OnReady;
            readyAction.action.Disable();
        }
    }

    private void Start()
    {
        UpdateWinner();
        if (continuePrompt != null) continuePrompt.enabled = false;

        // Animación de celebración aleatoria
        animator.SetInteger("Celebration", GetRandomNumber());
    }

    private void Update()
    {
        if (animator == null) return;

        // Obtenemos el estado actual del Animator
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

        // Comparamos con el nombre de la animación
        if (stateInfo.IsName("HumanoidIdle"))
        {
            if (!promptShown)
            {
                ShowContinuePrompt();
                promptShown = true; // marcar para que no se repita
            }
        }
    }


    private void OnReady(InputAction.CallbackContext ctx)
    {
        if (advanced) return;
        advanced = true;

        // Ocultar prompt (opcional)
        if (continuePrompt != null) continuePrompt.enabled = false;

        // Avanzar a siguiente mini
        if (GameManager.Instance != null)
            GameManager.Instance.LoadNextMiniGame();
        else
            Debug.LogWarning("MiniGameManager.Instance es null al intentar continuar desde post.");
    }

    public void UpdateWinner()
    {
        var color = PlayerChoices.GetWinner();
        winnerText.text = $"{color}";
        winnerText.color = PlayerChoices.GetColorRGBA(color);

        GameObject newPlayer = CharacterCatalog.Instance.Get(PlayerChoices.GetPlayerSkin(color));
        basePlayer.GetComponentInChildren<SkinnedMeshRenderer>().sharedMesh = newPlayer.GetComponentInChildren<SkinnedMeshRenderer>().sharedMesh;
        basePlayer.GetComponentInChildren<SkinnedMeshRenderer>().SetSharedMaterials(new List<Material>() { PlayerChoices.GetMaterialByColor(color) });

        foreach (Light light in lights)
        {
            light.color = PlayerChoices.GetColorRGBA(color);
        }
    }

    private void ShowContinuePrompt()
    {
        if (continuePrompt != null)
            continuePrompt.enabled = true;
        else 
            Debug.LogWarning("continuePrompt no asignado en el inspector.");

        SoundManager.FadeOutMusic(2f);
    }

    public int GetRandomNumber()
    {
        int rNumber = Random.Range(1, 5);

        switch (rNumber)
        {
            case 1: SoundManager.PlayMusic(2); break;  // Macarena
            case 2: SoundManager.PlayMusic(1); break;  // Gangnam Style
            case 3: SoundManager.PlayMusic(3); break; // Pajaritos
            case 4: SoundManager.PlayMusic(0); break;  // fallback
        }

        SoundManager.FadeInMusic(1f);
        Debug.Log($"Random number generated for celebration: {rNumber}");
        return rNumber;
    }
}
