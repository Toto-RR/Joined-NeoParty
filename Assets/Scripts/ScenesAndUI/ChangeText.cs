using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ChangeText : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI text;
    [SerializeField] private Button backButton;

    void Start()
    {
        GameManager.GameLength lenght = PlayerChoices.GetPartyLengthEnum(); 
        text.text = GetLengthDescription(lenght);

        backButton.onClick.AddListener(() =>
        {
            SceneChanger sceneManager = FindFirstObjectByType<SceneChanger>();
            if (sceneManager != null)
            {
                sceneManager.ChangeScene("TitleScene");
            }
        });
    }

    private string GetLengthDescription(GameManager.GameLength length)
    {
        switch (length)
        {
            case GameManager.GameLength.Short: return "Partida corta";
            case GameManager.GameLength.Medium: return "Partida media";
            case GameManager.GameLength.Long: return "Partida larga";
            case GameManager.GameLength.Marathon: return "Maratón";
            default: return "Tipo de partida desconocido";
        }
    }
}
