using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Splines;

public class CarSpawner : MonoBehaviour
{
    [SerializeField] private GameObject carPrefab;
    [SerializeField] private float speedMultiplier = 100f;
    [SerializeField] private SplineContainer splineContainer;

    private List<CarController> cars = new();

    public void SpawnCars()
    {
        List<PlayerChoices.PlayerData> activePlayers = PlayerChoices.GetActivePlayers();
        int playerCount = PlayerChoices.GetNumberOfPlayers();
        for (int i = 0; i < playerCount; i++)
        {
            if (i >= activePlayers.Count)
            {
                Debug.LogWarning("No hay suficientes jugadores activos para todos los coches.");
                break;
            }

            var player = activePlayers[i];
            GameObject configuredCar = ConfigureCar(player.Color);

            if (configuredCar != null)
            {
                string controlScheme = PlayerChoices.GetSchemaFromDevice(player.Device);
                PlayerInput playerInput = PlayerInput.Instantiate(
                    configuredCar,
                    controlScheme: controlScheme,
                    pairWithDevice: player.Device
                );

                if (playerInput.TryGetComponent<CarController>(out var controller))
                {
                    // Pasa los parametros al coche para configurarlo
                    controller.Setup(player.Color, splineContainer, i, speedMultiplier);
                    cars.Add(controller);
                }
            }
        }
    }

    private GameObject ConfigureCar(PlayerChoices.PlayerColor color)
    {
        GameObject car = carPrefab;
        // El componente mesh renderer está en el hijo del prefab
        car.GetComponentInChildren<MeshRenderer>().SetMaterials(new List<Material>() { PlayerChoices.GetMaterialByColor(color) });
        return car;
    }

    public List<CarController> GetPlayers()
    {
        return cars;
    }
}

