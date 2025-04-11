using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine;
using System.Linq;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public enum GameLength { Short, Medium, Long, Marathon }
    public GameLength currentGameLength;

    public List<string> selectedMiniGames;
    public int currentMiniGameIndex = 0;

    public Dictionary<int, int> playerScores = new Dictionary<int, int>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void StartGame(GameLength gameLength)
    {
        currentGameLength = gameLength;
        selectedMiniGames = GenerateGameSequence(gameLength);
        currentMiniGameIndex = 0;
        LoadNextMiniGame();
    }

    public void LoadNextMiniGame()
    {
        if (currentMiniGameIndex < selectedMiniGames.Count)
        {
            SceneManager.LoadScene(selectedMiniGames[currentMiniGameIndex]);
        }
        else
        {
            SceneManager.LoadScene("ResultsScene");
        }
    }

    public void MiniGameFinished(int winnerPlayerId)
    {
        if (!playerScores.ContainsKey(winnerPlayerId))
            playerScores[winnerPlayerId] = 0;

        playerScores[winnerPlayerId]++;
        currentMiniGameIndex++;
        LoadNextMiniGame();
    }

    private List<string> GenerateGameSequence(GameLength length)
    {
        List<string> allMiniGames = new List<string>() { "MiniGame_1", "MiniGame_2", "MiniGame_3", "MiniGame_4" };
        int count = length switch
        {
            GameLength.Short => 3,
            GameLength.Medium => 5,
            GameLength.Long => 8,
            GameLength.Marathon => 12,
            _ => 3
        };

        return allMiniGames.OrderBy(x => Random.value).Take(count).ToList();
    }
}
