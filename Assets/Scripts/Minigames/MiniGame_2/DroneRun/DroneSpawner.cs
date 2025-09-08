using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class DroneSpawner : MonoBehaviour
{
    [Header("Drone Prefab")]
    public GameObject dronePrefab;

    [Header("Spawn Points")]
    public Transform[] spawnPoints;

    [Header("Spawn Distance From Camera")]
    public GameObject gameCamera;
    public float spawnDistance = 50f;

    [Space(10)]
    public bool useSpawnPoints = false;

    private List<DroneController> drones = new();

    public void SpawnDrone()
    {
        List<PlayerChoices.PlayerData> activePlayers = PlayerChoices.GetActivePlayers();
        int playerCount = PlayerChoices.GetNumberOfPlayers();
        for (int i = 0; i < playerCount; i++)
        {
            if (i >= activePlayers.Count)
            {
                Debug.LogWarning("No hay suficientes jugadores activos para todos los drones.");
                break;
            }

            var player = activePlayers[i];
            GameObject configuredDrone = ConfigureDrone(player.Color);

            if (configuredDrone != null)
            {
                // Asigna spawn point
                Transform spawnPoint = spawnPoints[i];

                if(!useSpawnPoints)
                {
                    Vector3 spawnPos = gameCamera.transform.position + gameCamera.transform.forward * spawnDistance;
                    configuredDrone.transform.SetPositionAndRotation(spawnPos, gameCamera.transform.rotation);
                }
                else 
                    configuredDrone.transform.SetPositionAndRotation(spawnPoint.position, spawnPoint.rotation);

                string controlScheme = PlayerChoices.GetSchemaFromDevice(player.Device);
                PlayerInput playerInput = PlayerInput.Instantiate(
                    configuredDrone,
                    controlScheme: controlScheme,
                    pairWithDevice: player.Device
                );

                if (playerInput.TryGetComponent<DroneController>(out var controller))
                {
                    controller.Init(
                        activePlayers[i].Schema,
                        activePlayers[i].Color,
                        Minigame_2.Instance.matrixSystem.halfExtents,
                        Minigame_2.Instance.matrixSystem.transform.position
                        );

                    drones.Add(controller);
                }
            }
            else
            {
                Debug.LogError("El prefab del dron no está asignado.");
            }
        }

        foreach (var spawnPoint in spawnPoints)
            Destroy(spawnPoint.gameObject); // Destruye los puntos de spawn
    }

    private GameObject ConfigureDrone(PlayerChoices.PlayerColor color)
    {
        GameObject drone = dronePrefab;
        drone.GetComponent<MeshRenderer>().SetMaterials(new List<Material>() { PlayerChoices.GetMaterialByColor(color) });
        return drone;
    }

    public List<DroneController> GetPlayers()
    {
        return drones;
    }
}
