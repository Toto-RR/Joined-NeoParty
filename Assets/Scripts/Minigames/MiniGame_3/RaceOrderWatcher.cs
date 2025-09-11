using System;
using UnityEngine;

public class RaceOrderWatcher : MonoBehaviour
{
    public CarSpawner spawner;                 // arrástralo en el inspector
    public float checkInterval = 0f;           // 0 = cada frame; o pon 0.1f si quieres limitar

    public event Action<PlayerChoices.PlayerColor[]> OnOrderChanged;

    CarController[] _cars;
    PlayerChoices.PlayerColor[] _lastOrder = Array.Empty<PlayerChoices.PlayerColor>();
    float _t;

    void Start()
    {
        if (spawner == null) spawner = FindFirstObjectByType<CarSpawner>();
        _cars = spawner.GetPlayers().ToArray();

        ForceRefresh();
    }

    void Update()
    {
        _t += Time.deltaTime;
        if (checkInterval > 0f && _t < checkInterval) return;
        _t = 0f;

        var order = LeaderboardLogic.GetOrderedColors(_cars);
        if (!Same(order, _lastOrder))
        {
            _lastOrder = order;
            OnOrderChanged?.Invoke(order);
        }
    }

    public void ForceRefresh()
    {
        var order = LeaderboardLogic.GetOrderedColors(_cars);
        _lastOrder = order;
        OnOrderChanged?.Invoke(order);
    }

    bool Same(PlayerChoices.PlayerColor[] a, PlayerChoices.PlayerColor[] b)
    {
        if (a.Length != b.Length) return false;
        for (int i = 0; i < a.Length; i++) if (!a[i].Equals(b[i])) return false;
        return true;
    }
}
