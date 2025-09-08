using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Character Catalog", fileName = "CharacterCatalog")]
public class CharacterCatalog : ScriptableObject
{
    [Tooltip("Opcional. Si está vacío, se autocargan prefabs desde Resources/Characters")]
    public List<GameObject> characterPrefabs = new();

    private static CharacterCatalog _instance;
    public static CharacterCatalog Instance
    {
        get
        {
            if (_instance == null) EnsureLoaded();
            return _instance;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap() => EnsureLoaded();

    private static void EnsureLoaded()
    {
        if (_instance != null) return;

        // 1) Intentar cargar un asset único en Resources
        _instance = Resources.Load<CharacterCatalog>("CharacterCatalog");

        // 2) Si no existe, crear uno temporal en runtime y autollenarlo desde carpeta
        if (_instance == null)
        {
            _instance = CreateInstance<CharacterCatalog>();
            _instance.AutoScanResourcesFolder(); // Resources/Characters/*
#if UNITY_EDITOR
            Debug.LogWarning("CharacterCatalog.asset no encontrado en Resources. Se usará auto-scan de Resources/Characters en runtime.");
#endif
        }

        // Asegurar que hay contenido, aunque sea por auto-scan
        if (_instance.characterPrefabs == null || _instance.characterPrefabs.Count == 0)
            _instance.AutoScanResourcesFolder();
    }

    private void AutoScanResourcesFolder()
    {
        // Carga todos los prefabs de la carpeta Resources/Characters (subcarpetas incluidas)
        var loaded = Resources.LoadAll<GameObject>("Characters");
        characterPrefabs = loaded?.ToList() ?? new List<GameObject>();

        // Sugerencia: si quieres un orden concreto, nómbralos "01_Name", "02_Name", etc.
        characterPrefabs.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.Ordinal));
    }

    public GameObject Get(int index)
    {
        if (characterPrefabs == null || characterPrefabs.Count == 0) return null;
        index = Mathf.Clamp(index, 0, characterPrefabs.Count - 1);
        return characterPrefabs[index];
    }

    public int Count => characterPrefabs?.Count ?? 0;
}
