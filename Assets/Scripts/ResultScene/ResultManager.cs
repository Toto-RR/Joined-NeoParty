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

    private Camera mainCamera;
    private Animator animator;

    private void Awake()
    {
        mainCamera = Camera.main;
        animator = basePlayer.GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError("Animator component not found on playerBase.");
        }
    }

    private void Start()
    {
        UpdateWinner();

        // Inicia una animación de celebración aleatoria
        animator.SetInteger("Celebration", GetRandomNumber());
    }

    public void UpdateWinner()
    {
        var color = PlayerChoices.GetWinner();

        // Ajusta el texto del ganador
        winnerText.text = $"¡{color}!";
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

    public int GetRandomNumber()
    {
        int rNumber = Random.Range(1, 5);
        Debug.Log($"Random number generated for celebration: {rNumber}");
        return rNumber;
    }

}
