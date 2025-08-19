using UnityEngine;

public static class PlayerPrefabResolver
{
    public static GameObject Resolve(int skinIndex)
    {
        var profile = GameContext.CurrentProfile;

        // 1) Override por skin
        if (profile != null)
        {
            var bySkin = profile.GetSkinOverride(skinIndex);
            if (bySkin != null) return bySkin;

            // 2) Override por defecto para todo el minijuego
            if (profile.defaultPlayerPrefabOverride != null)
                return profile.defaultPlayerPrefabOverride;
        }

        // 3) Prefab base del catálogo (tu flujo actual)
        return CharacterCatalog.Instance.Get(skinIndex);
    }

    public static GameObject AttachAddonIfAny(GameObject playerRoot)
    {
        var profile = GameContext.CurrentProfile;
        if (profile != null && profile.playerAddonPrefab != null)
        {
            var addon = Object.Instantiate(profile.playerAddonPrefab, playerRoot.transform);
            // Llamada opcional a hooks si el add-on implementa IMinigameAddon:
            foreach (var a in addon.GetComponentsInChildren<IMinigameAddon>(true))
                a.OnAttach(playerRoot);
            return addon;
        }
        return null;
    }
}

// Interfaces opcionales para auto-enganchar referencias si las necesitas:
public interface IMinigameAddon { void OnAttach(GameObject playerRoot); }

public interface IMinigameSpawnSetup
{
    // El spawner llamará a esto si encuentra un componente que implemente la interfaz.
    void SetupSpawn(PlayerChoices.PlayerColor color, Transform[] carriles, int laneIndex, Camera cam, GameObject playerRoot);
}
