// VehicleDefinition.cs
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Vehicle Definition", fileName = "VehicleDef")]
public class VehicleDefinition : ScriptableObject
{
    public string id;           // p.ej. "veh_hover_01"
    public GameObject prefab;
    public Sprite icon;
    public string displayName;
}
