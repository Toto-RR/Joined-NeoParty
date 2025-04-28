using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "PlayerChoices", menuName = "Scriptable Objects/PlayersChoices")]
public class PlayerChoices : ScriptableObject
{
    private GameManager.GameLength gameLength;
    private int numberOfPlayers = 0;
    private List<PlayerColor> activePlayers = new List<PlayerColor>();

    public enum PlayerColor
    {
        Blue,
        Orange,
        Green,
        Yellow
    }

    // Setters
    public void SetPartyLength(GameManager.GameLength gameLength_) { gameLength = gameLength_; }
    public void SetNumberOfPlayers(int numberOfPlayers_) { numberOfPlayers = numberOfPlayers_; }
    public void SetActivePlayers(List<PlayerColor> players) { activePlayers = players; }

    // Getters
    public GameManager.GameLength GetPartyLengthEnum() { return gameLength; }
    public int GetNumberOfPlayers() { return numberOfPlayers; }
    public List<PlayerColor> GetActivePlayers() { return activePlayers; }
}
