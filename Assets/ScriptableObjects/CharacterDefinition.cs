using UnityEngine;

[CreateAssetMenu(fileName = "CharacterDefinition", menuName = "Scriptable Objects/CharacterDefinition")]
public class CharacterDefinition : ScriptableObject
{
    public string id;                 // p.ej. "neo_01" (�nico, estable)
    public GameObject prefab;         // el prefab del personaje
    public Sprite icon;               // opcional (UI)
    public string displayName;        // opcional
}
