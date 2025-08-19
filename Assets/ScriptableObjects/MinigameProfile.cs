using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[CreateAssetMenu(menuName = "Game/Minigame Profile")]
public class MinigameProfile : ScriptableObject
{
    [Header("Identidad")]
    public string minigameId;
    public GameObject minigameRootPrefab;

    [Header("Input por minijuego")]
    public InputActionAsset inputActions;
    public string defaultActionMap = "Gameplay";

    [Header("Overrides de jugador (opcionales)")]
    public GameObject defaultPlayerPrefabOverride; // coche, torreta, etc.
    public GameObject playerAddonPrefab;           // scripts específicos del MG

    [System.Serializable] public class SkinOverride { public int skinIndex; public GameObject prefab; }
    public List<SkinOverride> skinOverrides = new();

    public GameObject GetSkinOverride(int skin) =>
        skinOverrides.Find(x => x.skinIndex == skin)?.prefab;
}
