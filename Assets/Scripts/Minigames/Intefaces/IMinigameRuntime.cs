using System;
using System.Collections.Generic;

public interface IMinigameRuntime
{
    // Llamado justo tras instanciar el prefab raíz
    void Setup(MinigameProfile profile, List<PlayerChoices.PlayerData> roster);

    // Dispara la lógica de la ronda (intro, instrucciones, etc.)
    void RunRound();

    // Evento cuando la ronda termine: lista de ganadores por color
    event Action<List<PlayerChoices.PlayerColor>> OnRoundEnded;

    // Opcional: abortar/forzar final (para debug/skip)
    void ForceEndRound();
}
