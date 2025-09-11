using System.Collections.Generic;
using System.Linq;

public static class LeaderboardLogic
{
    public static List<CarController> SortByRaceProgress(IList<CarController> cars)
    {
        return cars
            .OrderByDescending(c => c.TotalProgress) // más vueltas + más progreso = primero
            .ToList();
    }

    public static PlayerChoices.PlayerColor[] GetOrderedColors(IList<CarController> cars)
    {
        return SortByRaceProgress(cars).Select(c => c.PlayerColor).ToArray();
    }

    public static PlayerChoices.PlayerColor GetLeader(IList<CarController> cars)
    {
        var sorted = SortByRaceProgress(cars);
        return (sorted.Count > 0) ? sorted[0].PlayerColor : default;
    }
}
