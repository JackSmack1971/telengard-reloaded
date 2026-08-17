using Telengard.Core.Simulation;

namespace Telengard.Core.Combat;

public sealed record AttackConfiguration
{
    public AttackConfiguration(int damage)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(damage, 1);
        Damage = damage;
    }

    public int Damage { get; }
}

public sealed record AttackCommand : ICommand;

public sealed record MonsterDamagedEvent(
    Guid MonsterInstanceId,
    int Amount,
    int RemainingHitPoints) : IDomainEvent;

public sealed record MonsterKilledEvent(Guid MonsterInstanceId) : IDomainEvent;

public static class AttackResolver
{
    public static CommandResult Resolve(
        GameState state,
        AttackCommand command,
        AttackConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(configuration);

        if (!state.Expedition.Active) throw new InvalidOperationException("An attack requires an active expedition.");
        if (!state.Player.Alive) throw new InvalidOperationException("A dead player cannot attack.");

        var combat = state.Combat ?? throw new InvalidOperationException("No combat is active.");
        if (combat.Phase != CombatPhase.Resolution || combat.SelectedAction != CombatAction.Attack)
        {
            throw new InvalidOperationException("An attack can be resolved only for a selected attack in the resolution phase.");
        }

        if (combat.Monster.CurrentHitPoints <= 0)
        {
            throw new InvalidOperationException("A defeated monster cannot be attacked.");
        }

        var damage = Math.Min(configuration.Damage, combat.Monster.CurrentHitPoints);
        var remainingHitPoints = combat.Monster.CurrentHitPoints - damage;
        var monsterInstanceId = combat.Monster.InstanceId;
        var events = new List<IDomainEvent>
        {
            new MonsterDamagedEvent(monsterInstanceId, damage, remainingHitPoints)
        };

        if (remainingHitPoints == 0)
        {
            var defeated = checked(state.Expedition.MonstersDefeated + 1);
            events.Add(new MonsterKilledEvent(monsterInstanceId));
            return new CommandResult(
                state with
                {
                    Combat = null,
                    Expedition = state.Expedition with { MonstersDefeated = defeated }
                },
                events);
        }

        var damagedMonster = combat.Monster with { CurrentHitPoints = remainingHitPoints };
        var nextCombat = combat with { Monster = damagedMonster, Phase = CombatPhase.EnemyAction };
        events.Add(new CombatPhaseChangedEvent(
            combat.EncounterId,
            combat.Phase,
            nextCombat.Phase,
            nextCombat.Round,
            nextCombat.SelectedAction));

        return new CommandResult(state with { Combat = nextCombat }, events);
    }
}
