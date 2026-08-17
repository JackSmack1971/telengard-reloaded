using Telengard.Core.Combat;
using Telengard.Core.Simulation;

namespace Telengard.Core.Magic;

public interface ISpellDefinition
{
    string Id { get; }
    int Cost { get; }
}

public sealed record CastSpellCommand : ICommand
{
    public CastSpellCommand(string spellId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(spellId);
        SpellId = spellId;
    }

    public string SpellId { get; }
}

public sealed record SpellCastEvent(
    Guid EncounterId,
    string SpellId,
    int Cost,
    int RemainingSpellPower) : IDomainEvent;

public static class SpellCastResolver
{
    public static CommandResult Resolve(
        GameState state,
        CastSpellCommand command,
        ISpellDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(definition);

        if (!state.Expedition.Active) throw new InvalidOperationException("A spell can be cast only during an active expedition.");
        if (!state.Player.Alive) throw new InvalidOperationException("A dead player cannot cast a spell.");

        var combat = state.Combat ?? throw new InvalidOperationException("A spell can be cast only during combat.");
        if (combat.Phase != CombatPhase.Resolution || combat.SelectedAction != CombatAction.CastSpell)
        {
            throw new InvalidOperationException("A spell can be cast only for a selected spell action in the resolution phase.");
        }

        if (string.IsNullOrWhiteSpace(definition.Id))
        {
            throw new ArgumentException("Spell definition id cannot be empty.", nameof(definition));
        }

        if (definition.Cost < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(definition), definition.Cost, "Spell cost cannot be negative.");
        }

        if (!string.Equals(command.SpellId, definition.Id, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("The spell command does not target the supplied definition.");
        }

        if (!state.Player.Spells.Contains(command.SpellId, StringComparer.Ordinal))
        {
            throw new InvalidOperationException("The player has not learned the requested spell.");
        }

        if (state.Player.SpellPower < 0)
        {
            throw new InvalidOperationException("Spell power cannot be negative.");
        }

        if (state.Player.SpellPower < definition.Cost)
        {
            throw new InvalidOperationException("The player does not have enough spell power.");
        }

        var remainingSpellPower = state.Player.SpellPower - definition.Cost;
        var nextCombat = combat with { Phase = CombatPhase.EnemyAction };
        var nextState = state with
        {
            Player = state.Player with { SpellPower = remainingSpellPower },
            Combat = nextCombat
        };

        return new CommandResult(
            nextState,
            [
                new SpellCastEvent(combat.EncounterId, command.SpellId, definition.Cost, remainingSpellPower),
                new CombatPhaseChangedEvent(
                    combat.EncounterId,
                    combat.Phase,
                    nextCombat.Phase,
                    nextCombat.Round,
                    nextCombat.SelectedAction)
            ]);
    }
}
