using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class SimpleSpawnPlayer : MonoBehaviour
{
    [Header("Base Player")]
    public GameObject player;

    [Header("Spawn Points")]
    public Transform[] spawnPoints;

    [Header("Lights")]
    public Light[] lightsToEnable;

    public void Start()
    {
        List<PlayerChoices.PlayerData> activePlayers = PlayerChoices.GetActivePlayers();
        int playerCount = PlayerChoices.GetNumberOfPlayers();

        for (int i = 0; i < playerCount; i++)
        {
            Transform spawnPoint = spawnPoints[i];

            if (i >= spawnPoints.Length)
            {
                Debug.LogWarning("No hay suficientes spawn points para todos los jugadores activos.");
                break;
            }

            var player = activePlayers[i];
            GameObject prefabToSpawn = Instantiate(GetPrefabForColor(activePlayers[i].Color), gameObject.transform);

            lightsToEnable[i].enabled = true;
            lightsToEnable[i].color = PlayerChoices.GetColorRGBA(activePlayers[i].Color);

            Vector3 spawnPos = spawnPoint.position;
            prefabToSpawn.transform.SetPositionAndRotation(spawnPos, spawnPoint.rotation);
            prefabToSpawn.transform.SetParent(gameObject.transform);
        }

        foreach (var spawnPoint in spawnPoints)
            Destroy(spawnPoint.gameObject); // Destruye los puntos de spawn
    }

    private GameObject GetPrefabForColor(PlayerChoices.PlayerColor color)
    {
        Debug.Log("Obteniendo prefab para el color: " + color);
        GameObject copyBase = player;
        return ConfigurePlayer(copyBase, color);
    }

    private GameObject ConfigurePlayer(GameObject basePlayer, PlayerChoices.PlayerColor color)
    {
        // Obtiene la skin del personaje del jugador (según el color)
        GameObject newPlayer = CharacterCatalog.Instance.Get(PlayerChoices.GetPlayerSkin(color));

        // Configura el mesh y material del jugador base
        basePlayer.GetComponentInChildren<SkinnedMeshRenderer>().sharedMesh = newPlayer.GetComponentInChildren<SkinnedMeshRenderer>().sharedMesh;
        basePlayer.GetComponentInChildren<SkinnedMeshRenderer>().SetSharedMaterials(new List<Material>() { PlayerChoices.GetMaterialByColor(color) });

        // Devuelve el jugador configurado
        return basePlayer;
    }
}
    
