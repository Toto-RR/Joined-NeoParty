using UnityEngine;
using TMPro;

public class PlayerRow : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI playerNameText;
    public GameObject checkMark;

    private TutorialManager tutorialManager;
    public bool isReady = false;
    private KeyCode assignedKey;

    public void Setup(PlayerChoices.PlayerColor color, TutorialManager manager)
    {
        playerNameText.text = color.ToString();
        tutorialManager = manager;
        checkMark.SetActive(false);

        assignedKey = GetKeyForPlayer(color);
    }

    private void Update()
    {
        if (!isReady && Input.GetKeyDown(assignedKey))
        {
            tutorialManager.PlayerReady(this);
        }
    }

    public void SetReady()
    {
        isReady = true;
        checkMark.SetActive(true);
    }

    private KeyCode GetKeyForPlayer(PlayerChoices.PlayerColor color)
    {
        switch (color)
        {
            case PlayerChoices.PlayerColor.Blue:
                return KeyCode.Space;  // Azul pulsa Espacio
            case PlayerChoices.PlayerColor.Orange:
                return KeyCode.Return; // Naranja pulsa Enter
            case PlayerChoices.PlayerColor.Green:
                return KeyCode.LeftShift; // Verde pulsa Shift Izquierdo
            case PlayerChoices.PlayerColor.Yellow:
                return KeyCode.RightShift; // Amarillo pulsa Shift Derecho
            default:
                return KeyCode.None;
        }
    }
}
