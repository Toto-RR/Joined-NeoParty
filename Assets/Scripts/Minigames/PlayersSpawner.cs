using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class PlayersSpawner : MonoBehaviour
{
    [Header("Player Prefabs")]
    public GameObject bluePlayerPrefab;
    public GameObject orangePlayerPrefab;
    public GameObject greenPlayerPrefab;
    public GameObject yellowPlayerPrefab;

    [Header("Spawn Points")]
    public Transform[] spawnPoints;

    [Header("Player Choices")]
    public PlayerChoices playerChoices; // Asignar en el inspector

    private List<Camera> playerCameras = new();

    private Rect[] finalRects = new Rect[]
    {
        new Rect(0f, 0f, 0.25f, 1f),
        new Rect(0.25f, 0f, 0.25f, 1f),
        new Rect(0.5f, 0f, 0.25f, 1f),
        new Rect(0.75f, 0f, 0.25f, 1f)
    };

    private void Start()
    {
        //SpawnPlayers();
    }

    public void SpawnPlayers()
    {
        List<PlayerChoices.PlayerColor> activePlayers = playerChoices.GetActivePlayers();
        int playerCount = activePlayers.Count;

        for (int i = 0; i < playerCount; i++)
        {
            if (i >= spawnPoints.Length)
            {
                Debug.LogWarning("No hay suficientes spawn points para todos los jugadores activos.");
                break;
            }

            GameObject prefabToSpawn = GetPrefabForColor(activePlayers[i]);

            if (prefabToSpawn != null)
            {
                GameObject player = Instantiate(prefabToSpawn, spawnPoints[i].position, spawnPoints[i].rotation, gameObject.transform);

                Camera playerCam = player.GetComponentInChildren<Camera>();

                if (playerCam != null)
                {
                    playerCameras.Add(playerCam);
                    playerCam.enabled = false; // NO activarla todavía
                }

                if (playerCam != null)
                {
                    // Calcular viewport dinámicamente
                    float width = 1f / playerCount; // Dividimos la pantalla en columnas
                    playerCam.rect = new Rect(i * width, 0f, width, 1f);
                }
                else
                {
                    Debug.LogWarning("No se encontró la cámara en el prefab del jugador.");
                }
            }
            else
            {
                Debug.LogWarning("No hay prefab asignado para el color " + activePlayers[i]);
            }
        }

        foreach (var spawnPoint in spawnPoints)
        {
            spawnPoint.gameObject.SetActive(false); // Desactivar los puntos de spawn
        }

    }

    private GameObject GetPrefabForColor(PlayerChoices.PlayerColor color)
    {
        switch (color)
        {
            case PlayerChoices.PlayerColor.Blue:
                return bluePlayerPrefab;
            case PlayerChoices.PlayerColor.Orange:
                return orangePlayerPrefab;
            case PlayerChoices.PlayerColor.Green:
                return greenPlayerPrefab;
            case PlayerChoices.PlayerColor.Yellow:
                return yellowPlayerPrefab;
            default:
                return null;
        }
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

                // Asigna viewport en columna
                // Calcula el ancho de cada cámara en función del número de jugadores
                // 1 / 2 = 0.5 - 2 jugadores (mitades)
                // 1 / 3 = 0.333 - 3 jugadores (tercios)
                // 1 / 4 = 0.25 - 4 jugadores (cuartos)
                float width = 1f / playerCount;
                cam.rect = new Rect(0f, 0f, 1f, 1f); // pantalla completa, centradas

                // Asigna depth según orden (el primero tiene más prioridad)
                cam.depth = playerCount - 1 - i; // Azul = 0, Naranja = 1, Verde = 2, Amarillo = 3
            }
        }
    }

    public List<Camera> GetPlayerCameras()
    {
        return playerCameras;
    }

    public IEnumerator ExpandCameras(List<Camera> playerCameras, float duration = 2f)
    {
        yield return new WaitForSeconds(0.5f);
        ActivatePlayerCameras();
        yield return new WaitForSeconds(1f);

        int count = playerCameras.Count;
        List<Rect> startRects = new List<Rect>();
        List<Rect> targetRects = new List<Rect>();

        for (int i = 0; i < count; i++)
        {
            startRects.Add(new Rect(0f, 0f, 1f, 1f)); // todas apiladas al inicio

            float width = 1f / count;
            Rect target = new Rect(i * width, 0f, width, 1f);
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

        // Forzar valores finales
        for (int i = 0; i < count; i++)
        {
            playerCameras[i].rect = targetRects[i];
        }
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

}
