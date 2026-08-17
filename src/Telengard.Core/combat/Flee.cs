using Telengard.Core.Rng;
using Telengard.Core.Simulation;

namespace Telengard.Core.Combat;

public sealed record FleeConfiguration
{
    public FleeConfiguration(double successChance)
    {
        if (double.IsNaN(successChance) || successChance is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(successChance), successChance, "Flee success chance must be between zero and one.");
        }

        SuccessChance = successChance;
    }

    public double SuccessChance { get; }
}

public sealed record FleeCommand : ICommand;

public sealed record EncounterEndedEvent(Guid EncounterId) : IDomainEvent;

public static class FleeResolver
{
    public static CommandResult Resolve(
        GameState state,
        FleeCommand command,
        FleeConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(configuration);

        if (!state.Expedition.Active) throw new InvalidOperationException("A flee attempt requires an active expedition.");
        if (!state.Player.Alive) throw new InvalidOperationException("A dead player cannot flee.");

        var combat = state.Combat ?? throw new InvalidOperationException("No combat is active.");
        if (combat.Phase != CombatPhase.Resolution || combat.SelectedAction != CombatAction.Flee)
        {
            throw new InvalidOperationException("Flee can be resolved only for a selected flee action in the resolution phase.");
        }

        var stream = new DeterministicRng(state.WorldSeed, state.Versions.GeneratorVersion)
            .CreateStream("flee", $"encounter:{combat.EncounterId}", $"round:{combat.Round}");
        if (stream.NextDouble() < configuration.SuccessChance)
        {
            return new CommandResult(
                state with { Combat = null },
                [new EncounterEndedEvent(combat.EncounterId)]);
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
