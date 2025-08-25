using System;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;
using UnityEngine.InputSystem;

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

    // Instancias actuales
    private readonly Dictionary<string, GameObject> spawnedPlayers = new();
    private readonly Dictionary<string, int> currentModelIndex = new();

    private void Start()
    {
        PlayerChoices.Instance.ResetPlayers();
    }

    public void AddNewPlayer(string color, InputDevice device)
    {
        if (!Enum.TryParse(color, true, out PlayerChoices.PlayerColor pc))
        {
            Debug.LogError($"Color inválido: {color}");
            return;
        }

        // Registrar en PlayerChoices
        PlayerChoices.AddPlayer(pc, device);

        // Evitar duplicados visuales
        if (spawnedPlayers.ContainsKey(color))
            Destroy(spawnedPlayers[color]);

        // Crear jugador visual
        var slot = GetSlot(color);
        if (slot == null)
        {
            Debug.LogError($"No hay slot configurado para color {color}");
            return;
        }

        int modelIndex = 0;
        currentModelIndex[color] = modelIndex;

        // Registrar skin en PlayerChoices
        PlayerChoices.SetPlayerSkin(pc, modelIndex);

        // Aplica material
        var prefab = CharacterCatalog.Instance.Get(modelIndex);
        prefab.GetComponentInChildren<SkinnedMeshRenderer>().SetSharedMaterials(new List<Material>() { ApplyMaterial(color) });

        // Instancia y añade a la lista
        var go = Instantiate(prefab, slot.spawnPoint.position, slot.spawnPoint.rotation);
        spawnedPlayers[color] = go;

        //Debug.Log($"Jugador {color} unido ({device.displayName})");
    }

    public void RemovePlayer(string color)
    {
        // Quitar visual
        if (spawnedPlayers.ContainsKey(color))
        {
            Destroy(spawnedPlayers[color]);
            spawnedPlayers.Remove(color);
        }

        currentModelIndex.Remove(color);

        Debug.Log($"Jugador {color} eliminado del lobby.");
    }


    public void Continue()
    {
        if (PlayerChoices.GetNumberOfPlayers() < 2)
        {
            Debug.Log("Venga hombre, siempre es mejor jugar con alguien!");
            return;
        }
        //MiniGameManager.Instance.LoadCurrentMinigame();
        //SceneChanger.Instance.ApplyTransitionAsync(SceneNames.GameScene_1, Transitions.Curtain);
        GameManager.Instance.LoadNextMiniGame();
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
}
