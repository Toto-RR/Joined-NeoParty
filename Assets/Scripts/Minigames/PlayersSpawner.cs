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
    public PlayerChoices playerChoices;

    [Header("Carriles por jugador")]
    public Transform[] carrilesJugador1;
    public Transform[] carrilesJugador2;
    public Transform[] carrilesJugador3;
    public Transform[] carrilesJugador4;

    private List<Camera> playerCameras = new();
    private List<PlayerController> players = new();

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
                Transform[] carrilesAsignados = ObtenerCarrilesParaJugador(i);
                Vector3 spawnPos = spawnPoints[i].position;/* +new Vector3(-2.5f, 0.5f, -20);*/

                GameObject player = Instantiate(prefabToSpawn, spawnPos, spawnPoints[i].rotation, gameObject.transform);
                Debug.Log("Instanciando en: " + spawnPos + " con rotacion: " + spawnPoints[i].rotation);

                PlayerController controller = player.GetComponent<PlayerController>();
                controller.playerColor = activePlayers[i];
                controller.AsignarCarriles(carrilesAsignados, i);

                players.Add(controller);

                Camera playerCam = player.GetComponentInChildren<Camera>();
                if (playerCam != null)
                {
                    playerCameras.Add(playerCam);
                    playerCam.enabled = false;

                    float width = 1f / playerCount;
                    playerCam.rect = new Rect(i * width, 0f, width, 1f);
                }

                Debug.Log("Jugador " + i + " instanciado con color: " + activePlayers[i] + " en la posicion " + player.transform.position);

            }
            else
            {
                Debug.LogWarning("No hay prefab asignado para el color " + activePlayers[i]);
            }

        }

        foreach (var spawnPoint in spawnPoints)
        {
            spawnPoint.gameObject.SetActive(false);
        }
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

    private GameObject GetPrefabForColor(PlayerChoices.PlayerColor color)
    {
        return color switch
        {
            PlayerChoices.PlayerColor.Blue => bluePlayerPrefab,
            PlayerChoices.PlayerColor.Orange => orangePlayerPrefab,
            PlayerChoices.PlayerColor.Green => greenPlayerPrefab,
            PlayerChoices.PlayerColor.Yellow => yellowPlayerPrefab,
            _ => null
        };
    }

    public List<Camera> GetPlayerCameras() => playerCameras;

    public IEnumerator ExpandCameras(List<Camera> playerCameras, float duration = 1f, float separation = 0.01f)
    {
        ActivatePlayerCameras();

        yield return new WaitForSeconds(1f);

        int count = playerCameras.Count;
        List<Rect> startRects = new();
        List<Rect> targetRects = new();

        for (int i = 0; i < count; i++)
        {
            startRects.Add(new Rect(0f, 0f, 1f, 1f));
            float totalSeparation = separation * (count - 1);
            float width = (1f - totalSeparation) / count;
            float xOffset = i * (width + separation);
            targetRects.Add(new Rect(xOffset, 0f, width, 1f));
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
                    cam.rect = LerpRect(startRects[i], targetRects[i], lerp);
                }
            }

            yield return null;
        }

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

    private void ActivatePlayerCameras()
    {
        foreach (Camera cam in playerCameras)
        {
            cam.enabled = true;
        }
    }
}
