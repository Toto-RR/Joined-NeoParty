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

    // Materiales para cada jugador
    [SerializeField] private Material blueMaterial;
    [SerializeField] private Material orangeMaterial;
    [SerializeField] private Material greenMaterial;
    [SerializeField] private Material yellowMaterial;

    // Instancias actuales
    private readonly Dictionary<string, GameObject> spawnedPlayers = new();
    private readonly Dictionary<string, int> currentModelIndex = new();

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

        // Aplica material
        var prefab = characterPrefabs[modelIndex];
        prefab.GetComponentInChildren<SkinnedMeshRenderer>().SetSharedMaterials(new List<Material>() { ApplyMaterial(color) });

        // Instancia y añade a la lista
        var go = Instantiate(prefab, slot.spawnPoint.position, slot.spawnPoint.rotation);
        spawnedPlayers[color] = go;

        Debug.Log($"Jugador {color} unido ({device.displayName})");
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
            
        }
        SceneChanger.Instance.ApplyTransitionAsync(SceneNames.GameScene, Transitions.Doors);
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

        var slot = GetSlot(color);
        if (slot == null) return;

        // Eliminar modelo actual
        Destroy(spawnedPlayers[color]);

        // Instanciar el nuevo modelo y aplicar el material
        var prefab = characterPrefabs[index];
        prefab.GetComponentInChildren<SkinnedMeshRenderer>().SetSharedMaterials(new List<Material>() { ApplyMaterial(color) });

        var go = Instantiate(prefab, slot.spawnPoint.position, slot.spawnPoint.rotation);
        Debug.Log("Rotation: " + slot.spawnPoint.rotation);
        spawnedPlayers[color] = go;

        Debug.Log($"Modelo cambiado para {color} → {index}");
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
            case "blue":
                return blueMaterial;
            case "orange":
                return orangeMaterial;
            case "green":
                return greenMaterial;
            case "yellow":
                return yellowMaterial;
            default:
                Debug.LogError($"Color {color} no reconocido para aplicar material.");
                return null;
        }
    }
}
