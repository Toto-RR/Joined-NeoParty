using System.Collections.Generic;
using TMPro;
using UnityEngine;
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
    [Tooltip("GameObject (o Image) que muestra el aviso de 'pulsa botón para continuar'")]
    public GameObject continuePrompt; // arrástralo desde la escena
    [Tooltip("Evita que se dispare más de una vez")]
    public bool showOnlyOnce = true;

    private Animator animator;
    private bool promptShown = false;

    private void Awake()
    {
        animator = basePlayer.GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError("Animator component not found on playerBase.");
        }
    }

    private void Start()
    {
        UpdateWinner();
        if (continuePrompt != null) continuePrompt.SetActive(false);

        // Inicia una animación de celebración aleatoria
        animator.SetInteger("Celebration", GetRandomNumber());
    }

    private void Update()
    {
        if (animator == null) return;

        // Capa 0 por defecto
        var state = animator.GetCurrentAnimatorStateInfo(0);

        // Solo cuando NO está en transición y el estado actual está tagueado como "Idle"
        if (!animator.IsInTransition(0) && state.tagHash == Animator.StringToHash("Idle"))
        {
            // Mostramos el prompt si no lo hemos mostrado o si se admite mostrar varias veces
            if (!promptShown || !showOnlyOnce)
            {
                ShowContinuePrompt();
            }
        }
    }

    public void UpdateWinner()
    {
        var color = PlayerChoices.GetWinner();

        // Ajusta el texto del ganador
        winnerText.text = $"{color}";
        winnerText.color = PlayerChoices.GetColorRGBA(color);

        // Configura la skin del jugador ganador
        GameObject newPlayer = CharacterCatalog.Instance.Get(PlayerChoices.GetPlayerSkin(color));
        basePlayer.GetComponentInChildren<SkinnedMeshRenderer>().sharedMesh = newPlayer.GetComponentInChildren<SkinnedMeshRenderer>().sharedMesh;
        basePlayer.GetComponentInChildren<SkinnedMeshRenderer>().SetSharedMaterials(new List<Material>() { PlayerChoices.GetMaterialByColor(color) });

        // Cambia las luces al color del ganador
        foreach(Light light in lights)
        {
            light.color = PlayerChoices.GetColorRGBA(color);
        }
    }

    private void ShowContinuePrompt()
    {
        if (continuePrompt != null)
        {
            continuePrompt.SetActive(true);
            promptShown = true;
        }
        else
        {
            Debug.LogWarning("continuePrompt no asignado en el inspector.");
        }
    }

    public int GetRandomNumber()
    {
        int rNumber = Random.Range(1, 5);
        Debug.Log($"Random number generated for celebration: {rNumber}");
        return rNumber;
    }

}
