using Telengard.Core.Simulation;

namespace Telengard.Core.Combat;

public sealed record DefendCommand : ICommand;

public static class DefendResolver
{
    public static CommandResult Resolve(GameState state, DefendCommand command)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(command);

        if (!state.Expedition.Active) throw new InvalidOperationException("A defend action requires an active expedition.");
        if (!state.Player.Alive) throw new InvalidOperationException("A dead player cannot defend.");

        var combat = state.Combat ?? throw new InvalidOperationException("No combat is active.");
        if (combat.Phase != CombatPhase.Resolution || combat.SelectedAction != CombatAction.Defend)
        {
            throw new InvalidOperationException("Defend can be resolved only for a selected defend action in the resolution phase.");
        }

        var nextCombat = combat with { Phase = CombatPhase.EnemyAction };
        return new CommandResult(
            state with { Combat = nextCombat },
            [new CombatPhaseChangedEvent(
                combat.EncounterId,
                combat.Phase,
                nextCombat.Phase,
                nextCombat.Round,
                nextCombat.SelectedAction)]);
    }
}
