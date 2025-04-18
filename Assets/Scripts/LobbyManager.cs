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

    private List<string> colorsAdded = new();
    private PlayerChoices playerChoices;
    private int numberOfPlayers;
    private Vector3 position = new(3, -10, 0);

    void Start() 
    {
        playerChoices = Resources.Load<PlayerChoices>("PlayerChoices");
    }

    void Update() { }

    public void AddNewPlayer(string color)
    { 
        PlayerPrefabSet selectedSet = playerPrefabs.Find(p => p.colorName.ToLower() == color.ToLower());

        if (selectedSet != null && selectedSet.astronautPrefab != null)
        {
            if(colorsAdded.Contains(selectedSet.colorName))
            {
                Debug.Log("Player already exists: " + color);
                return;
            }

            Instantiate(selectedSet.astronautPrefab, position, Quaternion.identity);
            selectedSet.visor.SetActive(true);
            selectedSet.questionMark.SetActive(false);
            colorsAdded.Add(color);
        }
        else
        {
            Debug.LogError("Color not found or prefab missing: " + color);
        }

        position += new Vector3(3, 0, 0);

        numberOfPlayers++;
        Debug.Log("New player added! Total: " + numberOfPlayers);
    }

    public void Continue()
    {
        SaveNumberPlayers();
        SceneChanger.Instance.ChangeScene("GameScene");
    }

    private void SaveNumberPlayers()
    {
        playerChoices.SetNumberOfPlayers(numberOfPlayers);
        Debug.Log("Number of players saved: " + numberOfPlayers);
    }
}
