using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using static PlayerChoices;
using static UnityEditor.Experimental.GraphView.GraphView;

public class CrosshairSpawner : MonoBehaviour
{
    private List<CrosshairController> players = new();

    [Header("Player Prefab")]
    public GameObject playerPrefab;

    [Header("Crosshair Prefab")]
    public GameObject crosshairPrefab;

    [Header("Spawn Points")]
    public Transform[] spawnPoints;

    private List<GameObject> crosshairs = new();

    public void SpawnCrosshairs()
    {
        List<PlayerChoices.PlayerData> activePlayers = PlayerChoices.GetActivePlayers();
        int playerCount = PlayerChoices.GetNumberOfPlayers();

        for (int i = 0; i < playerCount; i++)
        {
            if (i >= activePlayers.Count)
            {
                Debug.LogWarning("No hay suficientes jugadores activos para todos los crosshairs.");
                break;
            }

            var player = activePlayers[i];
            GameObject configuredCrosshair = ConfigureCrosshair(player.Color);

            if (configuredCrosshair != null)
            {
                // Asigna spawn point
                Transform spawnPoint = spawnPoints[i];

                string controlScheme = PlayerChoices.GetSchemaFromDevice(player.Device);
                PlayerInput playerInput = PlayerInput.Instantiate(
                    configuredCrosshair,
                    controlScheme: controlScheme,
                    pairWithDevice: player.Device
                );

                playerInput.transform.SetParent(gameObject.transform);
                playerInput.transform.SetPositionAndRotation(spawnPoint.position, Quaternion.identity);

                if (playerInput.TryGetComponent<CrosshairController>(out var controller))
                {
                    controller.Init(activePlayers[i].Schema, activePlayers[i].Color);
                    players.Add(controller);
                }
            }
        }

        foreach (var spawnPoint in spawnPoints)
            Destroy(spawnPoint.gameObject); // Destruye los puntos de spawn
    }

    private GameObject ConfigureCrosshair(PlayerChoices.PlayerColor color)
    {
        GameObject baseCrosshair = crosshairPrefab;

        foreach (var crosshairImage in baseCrosshair.GetComponentsInChildren<Image>())
        {
            crosshairImage.color = color switch
            {
                PlayerChoices.PlayerColor.Azul => PlayerChoices.GetColorRGBA(color),
                PlayerChoices.PlayerColor.Naranja => PlayerChoices.GetColorRGBA(color),
                PlayerChoices.PlayerColor.Verde => PlayerChoices.GetColorRGBA(color),
                PlayerChoices.PlayerColor.Amarillo => PlayerChoices.GetColorRGBA(color),
                _ => Color.white
            };
            crosshairImage.canvasRenderer.SetAlpha(0f);
        }

        crosshairs.Add(baseCrosshair);
        return baseCrosshair;
    }

    public IEnumerator ShowCrosshairs()
    {
        float fadeTime = 2f;

        foreach (var crosshair in crosshairs)
        {
            foreach (var img in crosshair.GetComponentsInChildren<Image>())
            {
                img.CrossFadeAlpha(1f, fadeTime, true);
            }
        }

        yield return new WaitForSeconds(fadeTime);
    }

    public List<CrosshairController> GetPlayers()
    {
        return players;
    }

}
