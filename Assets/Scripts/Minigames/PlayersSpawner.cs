using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.InputSystem;

public class PlayersSpawner : MonoBehaviour
{
    [Header("Base Player")]
    public GameObject player;

    [Header("Spawn Points")]
    public Transform[] spawnPoints;

    [Header("Carriles por jugador")]
    public Transform[] carrilesJugador1;
    public Transform[] carrilesJugador2;
    public Transform[] carrilesJugador3;
    public Transform[] carrilesJugador4;

    private List<Camera> playerCameras = new();
    private List<PlayerController> players = new();


    public void SpawnPlayers()
    {
        List<PlayerChoices.PlayerData> activePlayers = PlayerChoices.GetActivePlayers();
        int playerCount = PlayerChoices.GetNumberOfPlayers();

        for (int i = 0; i < playerCount; i++)
        {
            if (i >= spawnPoints.Length)
            {
                Debug.LogWarning("No hay suficientes spawn points para todos los jugadores activos.");
                break;
            }

            var player = activePlayers[i];
            GameObject prefabToSpawn = GetPrefabForColor(activePlayers[i].Color);
            if (prefabToSpawn != null)
            {
                Transform[] carrilesAsignados = ObtenerCarrilesParaJugador(i);
                Transform laneTransform = carrilesAsignados[LaneByColor(player.Color)];

                string controlScheme = PlayerChoices.GetSchemaFromDevice(player.Device);
                PlayerInput playerInput = PlayerInput.Instantiate(
                    prefabToSpawn,
                    controlScheme: controlScheme,
                    pairWithDevice: player.Device
                );

                Vector3 spawnPos = laneTransform.position + new Vector3(-0.5f, 0.5f, -20);
                playerInput.transform.SetPositionAndRotation(spawnPos, laneTransform.rotation);
                playerInput.transform.SetParent(gameObject.transform);

                if (playerInput.TryGetComponent<PlayerController>(out var controller))
                {
                    controller.playerColor = activePlayers[i].Color;
                    controller.AsignarCarriles(carrilesAsignados, LaneByColor(activePlayers[i].Color));

                    Camera playerCam = playerInput.GetComponentInChildren<Camera>();
                    if (playerCam != null)
                    {
                        playerCam.enabled = false; // NO activarla todavía
                        float width = 1f / playerCount;
                        playerCam.rect = new Rect(i * width, 0f, width, 1f);
                    }
                    else Debug.LogWarning("No se encontró la cámara en el prefab del jugador.");

                    var hud = playerInput.GetComponentInChildren<PlayerScreenHUD>();
                    if (hud != null)
                    {
                        hud.SetPlayer(controller);

                        var canvas = hud.GetComponent<Canvas>();
                        if (canvas != null)
                            canvas.worldCamera = playerCam;
                    }

                    playerCameras.Add(playerCam);
                    players.Add(controller);
                }
            }
            else Debug.LogWarning("No hay prefab asignado para el color " + activePlayers[i]);
        }

        foreach (var spawnPoint in spawnPoints)
            Destroy(spawnPoint.gameObject); // Destruye los puntos de spawn
    }

    public void ActivatePlayerCameras()
    {
        int playerCount = playerCameras.Count;

        for (int i = 0; i < playerCount; i++)
        {
            Debug.Log("Activando cámaras del jugador: " + playerCameras[i].transform.parent.name);

            Camera cam = playerCameras[i];

            if (cam != null)
            {
                cam.enabled = true;

                float width = 1f / playerCount;
                cam.rect = new Rect(0f, 0f, 1f, 1f);
                cam.depth = playerCount - 1 - i;
            }
        }
    }

    public IEnumerator ExpandCameras(List<Camera> playerCameras, float duration = 1f, float separation = 0.01f)
    {
        ActivatePlayerCameras();

        yield return new WaitForSeconds(1f);

        int count = playerCameras.Count;
        List<Rect> startRects = new List<Rect>();
        List<Rect> targetRects = new List<Rect>();

        for (int i = 0; i < count; i++)
        {
            startRects.Add(new Rect(0f, 0f, 1f, 1f));

            float totalSeparation = separation * (count - 1);
            float width = (1f - totalSeparation) / count;
            float xOffset = i * (width + separation);

            Rect target = new Rect(xOffset, 0f, width, 1f);
            targetRects.Add(target);
        }

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float lerp = Mathf.Clamp01(t / duration);

            for (int i = 0; i < count; i++)
            {
                Camera cam = playerCameras[i];
                if (cam != null)
                {
                    Rect interpolated = LerpRect(startRects[i], targetRects[i], lerp);
                    cam.rect = interpolated;
                }
            }

            yield return null;
        }

        for (int i = 0; i < count; i++)
        {
            playerCameras[i].rect = targetRects[i];
        }
    }

    public List<Camera> GetPlayerCameras()
    {
        Debug.Log("Cameras: " + playerCameras.Count);
        return playerCameras;
    }
    private Rect LerpRect(Rect a, Rect b, float t)
    {
        return new Rect(
            Mathf.Lerp(a.x, b.x, t),
            Mathf.Lerp(a.y, b.y, t),
            Mathf.Lerp(a.width, b.width, t),
            Mathf.Lerp(a.height, b.height, t)
        );
    }

    private GameObject GetPrefabForColor(PlayerChoices.PlayerColor color)
    {
        Debug.Log("Obteniendo prefab para el color: " + color);
        GameObject copyBase = player;
        return ConfigurePlayer(copyBase, color);
    }

    private Transform[] ObtenerCarrilesParaJugador(int index)
    {
        return index switch
        {
            0 => carrilesJugador1,
            1 => carrilesJugador2,
            2 => carrilesJugador3,
            3 => carrilesJugador4,
            _ => null
        };
    }

    private int LaneByColor(PlayerChoices.PlayerColor color)
    {
        return color switch
        {
            PlayerChoices.PlayerColor.Azul => 0,
            PlayerChoices.PlayerColor.Naranja => 1,
            PlayerChoices.PlayerColor.Verde => 2,
            PlayerChoices.PlayerColor.Amarillo => 3,
            _ => 0
        };
    }

    public List<PlayerController> GetPlayers()
    {
        return players;
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