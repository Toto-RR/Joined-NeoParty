using UnityEngine;
using System.Collections.Generic;

public class TutorialManager : MonoBehaviour
{
    [Header("Setup")]
    public Transform playersPanel;              // Panel donde instanciar los PlayerRow
    public GameObject playerRowPrefab;          // Prefab de la fila de jugador
    public PlayerChoices playerChoices;         // ScriptableObject con jugadores activos
    public MiniGameBase minigameBase;           // Referencia al minijuego actual

    private int readyPlayers = 0;
    private int totalPlayers = 0;
    private List<PlayerRow> playerRows = new();

    private void Start()
    {
        totalPlayers = playerChoices.GetNumberOfPlayers();
        SetupPlayerRows();
    }

    private void SetupPlayerRows()
    {
        foreach (var playerColor in playerChoices.GetActivePlayers())
        {
            GameObject rowObj = Instantiate(playerRowPrefab, playersPanel);
            PlayerRow playerRow = rowObj.GetComponent<PlayerRow>();
            playerRow.Setup(playerColor, this);
            playerRows.Add(playerRow);
        }
    }

    public void PlayerReady(PlayerRow row)
    {
        if (!row.isReady)
        {
            row.SetReady();
            readyPlayers++;

            if (readyPlayers >= totalPlayers)
            {
                Debug.Log("Todos los jugadores listos en el tutorial.");
                minigameBase.OnAllPlayersReady(); // Avisa al minijuego
            }
        }
    }
}
