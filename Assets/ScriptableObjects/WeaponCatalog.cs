// WeaponCatalog.cs
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Weapon Catalog")]
public class WeaponCatalog : ScriptableObject
{
    public List<WeaponDefinition> weapons = new();

    private static WeaponCatalog _instance;
    public static WeaponCatalog Instance { get { if (_instance == null) EnsureLoaded(); return _instance; } }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap() => EnsureLoaded();

    private static void EnsureLoaded()
    {
        if (_instance) return;
        _instance = Resources.Load<WeaponCatalog>("WeaponCatalog");
        if (_instance == null)
        {
            _instance = CreateInstance<WeaponCatalog>();
            _instance.weapons = Resources.LoadAll<WeaponDefinition>("Weapons").ToList();
        }
        if (_instance.weapons == null) _instance.weapons = new List<WeaponDefinition>();
    }

    public WeaponDefinition GetById(string id) => weapons?.FirstOrDefault(w => w.id == id);
}
