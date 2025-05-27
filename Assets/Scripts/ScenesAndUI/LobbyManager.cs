using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PlayerPrefabSet
{
    public string colorName;
    public GameObject astronautPrefab;
    public GameObject visor;
    public GameObject questionMark;
}

public class LobbyManager : MonoBehaviour
{
    [SerializeField] private List<PlayerPrefabSet> playerPrefabs = new();
    private Vector3 position = new(3, -10, 0);

    public void AddNewPlayer(string color)
    {
        // Encuentra el prefab correspondiente al color
        PlayerPrefabSet selectedSet = playerPrefabs.Find(p => p.colorName.ToLower() == color.ToLower());

        if (selectedSet != null && selectedSet.astronautPrefab != null)
        {
            // Instancia el prefab y activa el visor
            Instantiate(selectedSet.astronautPrefab, position, Quaternion.identity);
            selectedSet.visor.SetActive(true);
            selectedSet.questionMark.SetActive(false);
        }
        else Debug.LogError("Color not found or prefab missing: " + color);

        position += new Vector3(3, 0, 0);
        Debug.Log("New player added! Total: " + PlayerChoices.GetNumberOfPlayers());
    }

    public void Continue()
    {
        if(PlayerChoices.GetNumberOfPlayers() < 2)
        {
            Debug.Log("Venga hombre, siempre es mejor jugar con alguien!");
            return;
        }

        SceneChanger.Instance.ChangeScene("GameScene");
    }

}
