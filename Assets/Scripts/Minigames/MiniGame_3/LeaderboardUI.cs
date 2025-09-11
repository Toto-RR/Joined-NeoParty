using UnityEngine;
using UnityEngine.UI;

public class LeaderboardUI : MonoBehaviour
{
    public Image[] slots; // arrastra en el inspector las imágenes de tus jugadores

    void Start()
    {
        var watcher = FindFirstObjectByType<RaceOrderWatcher>();
        watcher.OnOrderChanged += UpdateUI;
    }

    void UpdateUI(PlayerChoices.PlayerColor[] order)
    {
        for (int i = 0; i < slots.Length && i < order.Length; i++)
        {
            slots[i].color = PlayerChoices.GetColorRGBA(order[i]);
            slots[i].GetComponentInChildren<TMPro.TextMeshProUGUI>().text = order[i].ToString();
        }
    }
}
