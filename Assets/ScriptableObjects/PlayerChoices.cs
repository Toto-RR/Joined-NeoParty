using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "PlayerChoices", menuName = "Scriptable Objects/PlayersChoices")]
public class PlayerChoices : ScriptableObject
{
    private GameManager.GameLength gameLength;

    public void SetPartyLength(GameManager.GameLength gameLength_)
    {
        // Guarda la eleccion del jugador
        gameLength = gameLength_;
    }

    public GameManager.GameLength GetPartyLengthEnum()
    {
        // Devuelve el valor del modo escogido como un ENUM
        return gameLength;
    }
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
