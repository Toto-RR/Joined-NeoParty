using UnityEngine;
using TMPro;

public class PlayerScreenHUD : MonoBehaviour
{
    public TextMeshProUGUI scoreText;
    private PlayerController player;

    public void SetPlayer(PlayerController p)
    {
        player = p;
    }

    void Update()
    {
        if (player != null && scoreText != null)
        {
            scoreText.text = $"Puntos: {player.GetPoints()}";
        }
    }
}
