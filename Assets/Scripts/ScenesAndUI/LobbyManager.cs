using System;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[System.Serializable]
public class LobbySlot
{
    public string colorName;
    public Transform spawnPoint;
}

public class LobbyManager : MonoBehaviour
{
    // Lista de todos los prefabs de personajes
    [SerializeField] private List<GameObject> characterPrefabs = new();

    // Punto de spawn de cada color
    [SerializeField] private List<LobbySlot> slots = new();

    // Referencia interna al manager de UI
    private LobbyUIManager uiManager;

    // Instancias actuales
    private readonly Dictionary<string, GameObject> spawnedPlayers = new();
    private readonly Dictionary<string, int> currentModelIndex = new();

    public bool DebugMode = false;
    public string Dbg_SceneToLoad = "GameScene_1";

    private void Awake()
    {
        uiManager = GetComponent<LobbyUIManager>();
    }

    private void Start()
    {
        PlayerChoices.Instance.ResetPlayers();

        SoundManager.PlayMusic(10); // Lobby_Theme
        SoundManager.FadeInMusic(1f);
    }

    public void AddNewPlayer(string color, InputDevice device)
    {
        if (!Enum.TryParse(color, true, out PlayerChoices.PlayerColor pc))
        {
            Debug.LogError($"Color inválido: {color}");
            return;
        }

        bool wasEmpty = spawnedPlayers.Count == 0;

        PlayerChoices.AddPlayer(pc, device);

        if (spawnedPlayers.ContainsKey(color))
            Destroy(spawnedPlayers[color]);

        var slot = GetSlot(color);
        if (slot == null)
        {
            Debug.LogError($"No hay slot configurado para color {color}");
            return;
        }

        int modelIndex = 0;
        currentModelIndex[color] = modelIndex;

        PlayerChoices.SetPlayerSkin(pc, modelIndex);

        var prefab = CharacterCatalog.Instance.Get(modelIndex);
        prefab.GetComponentInChildren<SkinnedMeshRenderer>()
              .SetSharedMaterials(new List<Material>() { ApplyMaterial(color) });

        var go = Instantiate(prefab, slot.spawnPoint.position, slot.spawnPoint.rotation);
        spawnedPlayers[color] = go;

        // --- UI: general y layout del color ---
        if (uiManager != null)
        {
            if (wasEmpty)
            {
                uiManager.SetGeneralStep(2);   // el general pasa a step 2
                uiManager.SetLayoutStep(color, 2); // el jugador que se une también pasa a step 2
            }
            else
            {
                uiManager.SetLayoutStep(color, 2); // los demás solo actualizan su layout
            }
        }
    }

    public void RemovePlayer(string color)
    {
        // UI: primero retrocede el layout del color
        if (uiManager != null)
            uiManager.SetLayoutStep(color, 1);

        if (spawnedPlayers.ContainsKey(color))
        {
            Destroy(spawnedPlayers[color]);
            spawnedPlayers.Remove(color);
            SoundManager.PlayFX(5);
        }

        currentModelIndex.Remove(color);
        Debug.Log($"Jugador {color} eliminado del lobby.");

        // UI: si ya no queda nadie, retrocede el general
        if (uiManager != null && spawnedPlayers.Count == 0)
            uiManager.SetGeneralStep(1);
    }

    public void OnPlayerReadyChanged(string color, bool isReady)
    {
        if (uiManager == null) return;
        if (isReady) uiManager.SetLayoutStep(color, 3);
        else uiManager.SetLayoutStep(color, 2); ;
    }

    public void Continue()
    {
        if (PlayerChoices.GetNumberOfPlayers() < 2)
        {
            Debug.Log("Venga hombre, siempre es mejor jugar con alguien!");
            return;
        }

        if (!DebugMode) GameManager.Instance.LoadNextMiniGame();
        else SceneChanger.Instance.ApplyTransitionAsync(Dbg_SceneToLoad, Transitions.Curtain);
    }

    public void CyclePlayerModel(string color)
    {
        if (!spawnedPlayers.ContainsKey(color))
        {
            Debug.LogWarning($"No hay jugador en {color} para cambiar modelo.");
            return;
        }

        int index = currentModelIndex.ContainsKey(color) ? currentModelIndex[color] : 0;
        index = (index + 1) % characterPrefabs.Count;
        currentModelIndex[color] = index;

        // Cambiamos la skin elegida al ciclar
        if (Enum.TryParse(color, true, out PlayerChoices.PlayerColor pc))
        {
            PlayerChoices.SetPlayerSkin(pc, index);
        }

        var slot = GetSlot(color);
        if (slot == null) return;

        // Eliminar modelo actual
        Destroy(spawnedPlayers[color]);

        // Instanciar el nuevo modelo y aplicar el material
        var prefab = CharacterCatalog.Instance.Get(index);
        prefab.GetComponentInChildren<SkinnedMeshRenderer>().SetSharedMaterials(new List<Material>() { ApplyMaterial(color) });

        var go = Instantiate(prefab, slot.spawnPoint.position, slot.spawnPoint.rotation);
        spawnedPlayers[color] = go;

        SoundManager.PlayFX(3); // sonido de unión (AddPlayer_Lobby)

        //Debug.Log($"Modelo cambiado para {color} → {index}");
    }

    // Helper para encontrar el slot por color
    private LobbySlot GetSlot(string color)
    {
        return slots.Find(s => s.colorName.Equals(color, StringComparison.OrdinalIgnoreCase));
    }

    private Material ApplyMaterial(string color)
    {
        switch (color.ToLower())
        {
            case "azul":
                return PlayerChoices.Instance.colorMaterials[0];
            case "naranja":
                return PlayerChoices.Instance.colorMaterials[1];
            case "verde":
                return PlayerChoices.Instance.colorMaterials[2];
            case "amarillo":
                return PlayerChoices.Instance.colorMaterials[3];
            default:
                Debug.LogError($"Color {color} no reconocido para aplicar material.");
                return null;
        }
    }

    public void GoBackToPreviousScene()
    {
        if (SceneChanger.Instance != null)
        {
            SoundManager.FadeOutMusic(1f);
            SoundManager.ResetMusic();
            SceneChanger.Instance.ApplyTransitionAsync(SceneNames.MainMenu, Transitions.Fade);
            return;
        }
    }

}
