using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "PlayerChoices", menuName = "Scriptable Objects/PlayersChoices")]
public class PlayerChoices : ScriptableObject
{
    private GameManager.GameLength gameLength;
    private int numberOfPlayers = 0;

    public void SetPartyLength(GameManager.GameLength gameLength_) { gameLength = gameLength_; }
    public void SetNumberOfPlayers(int numberOfPlayers_) { numberOfPlayers = numberOfPlayers_; }
    public GameManager.GameLength GetPartyLengthEnum() { return gameLength; }
    public int GetNumberOfPlayers() { return numberOfPlayers; }
}

#if UNITY_EDITOR

[CustomEditor(typeof(PlayerChoices))]
public class PlayerChoicesEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (GUILayout.Button("Test Log"))
        {
            Debug.Log("ScriptableObject is working!");
        }
    }
}
#endif
