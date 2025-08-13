using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PlayerPrefabSet
{
    public string colorName;
    public GameObject playerPrefab;
    public GameObject visor;
    public GameObject questionMark;
}

public class LobbyManager : MonoBehaviour
{
    [SerializeField] private List<PlayerPrefabSet> playerPrefabs = new();
    private Vector3 position = new(3, -10, 0);

    private Dictionary<string, GameObject> playersInLobby = new Dictionary<string, GameObject>();

    public void AddNewPlayer(string color)
    {
        // Encuentra el prefab correspondiente al color
        PlayerPrefabSet selectedSet = playerPrefabs.Find(p => p.colorName.ToLower() == color.ToLower());

        if (selectedSet != null && selectedSet.playerPrefab != null)
        {
            // Instancia el prefab y activa el visor
            GameObject playerPrefab = Instantiate(selectedSet.playerPrefab, position, Quaternion.identity);
            selectedSet.visor.SetActive(true);
            selectedSet.questionMark.SetActive(false);
            playersInLobby[color] = playerPrefab;
        }
        else Debug.LogError("Color not found or prefab missing: " + color);

        position += new Vector3(3, 0, 0);
        Debug.Log("New player added! Total: " + PlayerChoices.GetNumberOfPlayers());
    }

    public void RemovePlayer(string colorName)
    {
        // Oculta visor y muestra interrogante si existen
        PlayerPrefabSet selectedSet = playerPrefabs.Find(p => p.colorName.ToLower() == colorName.ToLower());
        if (selectedSet != null)
        {
            if (selectedSet.visor != null) selectedSet.visor.SetActive(false);
            if (selectedSet.questionMark != null) selectedSet.questionMark.SetActive(true);
        }

        // Elimina el astronauta instanciado
        if (playersInLobby.TryGetValue(colorName, out GameObject astronaut))
        {
            Destroy(astronaut);
            playersInLobby.Remove(colorName);
            Debug.Log($"Jugador {colorName} eliminado visualmente del lobby.");
        }
        else
        {
            Debug.LogWarning($"No se encontró jugador visual con color {colorName}.");
        }
    }


    public void Continue()
    {
        if(PlayerChoices.GetNumberOfPlayers() < 2)
        {
            Debug.Log("Venga hombre, siempre es mejor jugar con alguien!");
            return;
        }

        //SceneChanger.Instance.ChangeScene("GameScene");
        SceneChanger.Instance.ApplyTransitionAsync(SceneNames.GameScene, Transitions.Doors);
    }

}
