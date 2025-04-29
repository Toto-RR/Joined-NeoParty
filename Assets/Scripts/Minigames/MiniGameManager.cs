using UnityEngine;
using System.Collections.Generic;

public class MiniGameManager : MonoBehaviour
{
    public List<GameObject> miniGamePrefabs;
    private int currentMiniGameIndex = 0;
    private GameObject currentMiniGameInstance;

    private void Start()
    {
        StartNextMiniGame();
    }

    private void StartNextMiniGame()
    {
        if (currentMiniGameInstance != null)
        {
            Destroy(currentMiniGameInstance);
        }

        if (currentMiniGameIndex < miniGamePrefabs.Count)
        {
            currentMiniGameInstance = Instantiate(miniGamePrefabs[currentMiniGameIndex], Vector3.zero, Quaternion.identity);
            currentMiniGameIndex++;
        }
        else
        {
            Debug.Log("Todos los minijuegos han terminado.");
        }
    }

    public void OnMiniGameEnded()
    {
        StartNextMiniGame();
    }
}
