// WeaponDefinition.cs
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Weapon Definition", fileName = "WeaponDef")]
public class WeaponDefinition : ScriptableObject
{
    public string id;           // p.ej. "wpn_laser_01"
    public GameObject prefab;   // puede ser solo el "attachment" o todo el arma
    public Sprite icon;
    public string displayName;
}
