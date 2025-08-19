// VehicleCatalog.cs
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Vehicle Catalog")]
public class VehicleCatalog : ScriptableObject
{
    public List<VehicleDefinition> vehicles = new();

    private static VehicleCatalog _instance;
    public static VehicleCatalog Instance { get { if (_instance == null) EnsureLoaded(); return _instance; } }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap() => EnsureLoaded();

    private static void EnsureLoaded()
    {
        if (_instance) return;
        _instance = Resources.Load<VehicleCatalog>("VehicleCatalog");
        if (_instance == null)
        {
            _instance = CreateInstance<VehicleCatalog>();
            _instance.vehicles = Resources.LoadAll<VehicleDefinition>("Vehicles").ToList();
        }
        if (_instance.vehicles == null) _instance.vehicles = new List<VehicleDefinition>();
    }

    public VehicleDefinition GetById(string id) => vehicles?.FirstOrDefault(v => v.id == id);
}
