using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine;
using System.Linq;
using System.Collections;
using UnityEditor.SearchService;
using UnityEditor.Build.Reporting;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    
    public enum GameLength 
    { 
        Short, 
        Medium, 
        Long, 
        Marathon 
    }
    public GameLength currentGameLength;

    [Tooltip("Nombres de las escenas de minijuegos (deben estar en Build Settings).")]
    public List<string> selectedMiniGames;
    private List<string> poolMinigames;

    public int currentMiniGameIndex = 0;

    [Header("Flow Scenes")]
    public string postMinigameScene = "MinigameResult";
    public string endGameScene = "EndGame";

    private SceneChanger sceneChanger; 

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            sceneChanger = GetComponent<SceneChanger>();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void StartGame(string gameLengthString)
    {
        if (System.Enum.TryParse(gameLengthString, out GameLength gameLength))
        {
            Debug.Log(gameLengthString + " started!");
            StartGame(gameLength);
        }
        else
        {
            Debug.LogError("Invalid game length string: " + gameLengthString);
        }
    }

    public void StartGame(GameLength gameLength)
    {
        currentGameLength = gameLength;
        poolMinigames = GenerateGameSequence(gameLength);
        PlayerChoices.SetPartyLength(gameLength);

        // Guarda la elección del jugador y pasa a la escena del lobby
        sceneChanger.ApplyTransitionAsync(SceneNames.Lobby, Transitions.Fade);
    }

    public void LoadNextMiniGame()
    {
        if (currentMiniGameIndex < poolMinigames.Count)
        {
            sceneChanger.ApplyTransitionAsync(poolMinigames[currentMiniGameIndex], Transitions.Fade);
        }
        else
        {
            sceneChanger.ApplyTransitionAsync(SceneNames.EndGame, Transitions.Curtain);
        }
    }

    public IEnumerator LoadPostMinigame()
    {
        yield return new WaitForSeconds(1f);
        sceneChanger.ApplyTransitionAsync(SceneNames.MinigameResult, Transitions.FadeText);
    }

    public void MiniGameFinished()
    {
        currentMiniGameIndex++;
        StartCoroutine(LoadPostMinigame());
    }

    private List<string> GenerateGameSequence(GameLength length)
    {
        int count = length switch
        {
            GameLength.Short => 1,
            GameLength.Medium => 2,
            GameLength.Long => 3,
            GameLength.Marathon => 3,
            _ => 1
        };

        return selectedMiniGames.Take(count).ToList();
    }
}
