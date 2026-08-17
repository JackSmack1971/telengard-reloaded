using Telengard.Core.Combat;
using Telengard.Core.Simulation;
using Telengard.Save;
using Xunit;

namespace Telengard.Architecture.Tests;

public sealed class AttackTests
{
    [Fact]
    public void Attack_damages_the_monster_and_advances_surviving_combat()
    {
        var result = AttackResolver.Resolve(
            ActiveCombat(5),
            new AttackCommand(),
            new AttackConfiguration(2));

        Assert.Equal(3, result.State.Combat!.Monster.CurrentHitPoints);
        Assert.Equal(CombatPhase.EnemyAction, result.State.Combat.Phase);
        Assert.Equal(CombatAction.Attack, result.State.Combat.SelectedAction);
        Assert.Equal(0, result.State.Expedition.MonstersDefeated);

        var damaged = Assert.IsType<MonsterDamagedEvent>(result.Events[0]);
        Assert.Equal(2, damaged.Amount);
        Assert.Equal(3, damaged.RemainingHitPoints);
        var phaseChanged = Assert.IsType<CombatPhaseChangedEvent>(result.Events[1]);
        Assert.Equal(CombatPhase.Resolution, phaseChanged.From);
        Assert.Equal(CombatPhase.EnemyAction, phaseChanged.To);
    }

    [Fact]
    public void Attack_kills_the_monster_and_closes_combat()
    {
        var result = AttackResolver.Resolve(
            ActiveCombat(3),
            new AttackCommand(),
            new AttackConfiguration(10));

        Assert.Null(result.State.Combat);
        Assert.Equal(1, result.State.Expedition.MonstersDefeated);
        var damaged = Assert.IsType<MonsterDamagedEvent>(result.Events[0]);
        Assert.Equal(3, damaged.Amount);
        Assert.Equal(0, damaged.RemainingHitPoints);
        var killed = Assert.IsType<MonsterKilledEvent>(result.Events[1]);
        Assert.Equal(damaged.MonsterInstanceId, killed.MonsterInstanceId);
    }

    [Fact]
    public void Equal_attack_inputs_replay_to_equal_state_and_events()
    {
        var first = AttackResolver.Resolve(ActiveCombat(9), new AttackCommand(), new AttackConfiguration(4));
        var second = AttackResolver.Resolve(ActiveCombat(9), new AttackCommand(), new AttackConfiguration(4));

        Assert.Equal(first.State, second.State);
        Assert.Equal(first.Events, second.Events);
    }

    [Fact]
    public void Attack_requires_a_live_expedition_and_selected_attack_resolution()
    {
        var command = new AttackCommand();
        var configuration = new AttackConfiguration(1);

        Assert.Throws<InvalidOperationException>(() => AttackResolver.Resolve(
            ActiveCombat(3) with { Expedition = new ExpeditionState { Active = false } }, command, configuration));
        Assert.Throws<InvalidOperationException>(() => AttackResolver.Resolve(
            ActiveCombat(3) with { Player = ActiveCombat(3).Player with { Alive = false } }, command, configuration));
        Assert.Throws<InvalidOperationException>(() => AttackResolver.Resolve(
            ActiveCombat(3) with { Combat = null }, command, configuration));
        Assert.Throws<InvalidOperationException>(() => AttackResolver.Resolve(
            ActiveCombat(3) with { Combat = ActiveCombat(3).Combat! with { Phase = CombatPhase.PlayerAction } },
            command,
            configuration));
        Assert.Throws<InvalidOperationException>(() => AttackResolver.Resolve(
            ActiveCombat(3) with { Combat = ActiveCombat(3).Combat! with { SelectedAction = CombatAction.Defend } },
            command,
            configuration));
    }

    [Fact]
    public void Attack_rejects_null_arguments_and_checked_defeat_overflow()
    {
        var state = ActiveCombat(3);
        var command = new AttackCommand();
        var configuration = new AttackConfiguration(1);

        Assert.Throws<ArgumentNullException>(() => AttackResolver.Resolve(null!, command, configuration));
        Assert.Throws<ArgumentNullException>(() => AttackResolver.Resolve(state, null!, configuration));
        Assert.Throws<ArgumentNullException>(() => AttackResolver.Resolve(state, command, null!));

        var overflowing = state with
        {
            Expedition = state.Expedition with { MonstersDefeated = int.MaxValue }
        };
        Assert.Throws<OverflowException>(() => AttackResolver.Resolve(overflowing, command, new AttackConfiguration(3)));
    }

    [Fact]
    public void A_selected_attack_cannot_advance_without_resolution()
    {
        Assert.Throws<InvalidOperationException>(() => CombatStateResolver.Advance(
            ActiveCombat(3),
            new AdvanceCombatCommand()));
    }

    [Fact]
    public void Attack_rejects_a_defeated_monster_before_mutation()
    {
        var state = ActiveCombat(0);

        Assert.Throws<InvalidOperationException>(() => AttackResolver.Resolve(
            state,
            new AttackCommand(),
            new AttackConfiguration(1)));
        Assert.Equal(0, state.Combat!.Monster.CurrentHitPoints);
    }

    [Fact]
    public void Attack_configuration_requires_positive_damage()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new AttackConfiguration(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new AttackConfiguration(-1));
    }

    [Fact]
    public void Attack_result_round_trips_through_the_existing_explicit_save_contract()
    {
        var result = AttackResolver.Resolve(
            ActiveCombat(8),
            new AttackCommand(),
            new AttackConfiguration(3));

        var restored = SaveGameSerializer.Deserialize(SaveGameSerializer.Serialize(result.State));

        Assert.Equal(SaveGameSerializer.Serialize(result.State), SaveGameSerializer.Serialize(restored));
        Assert.Equal(5, restored.Combat!.Monster.CurrentHitPoints);
        Assert.Equal(GameState.CurrentSaveVersion, restored.SaveVersion);
    }

    private static GameState ActiveCombat(int hitPoints)
    {
        var state = GameState.Create(1234) with
        {
            Inn = new InnState { IsAtInn = false },
            Expedition = new ExpeditionState { Active = true },
            Player = new PlayerState { Position = new DungeonPosition(1, 0, 0) }
        };

        var monster = new MonsterInstance(
            Guid.Parse("00000000-0000-0000-0000-000000000001"),
            "rat",
            1,
            hitPoints,
            state.Player.Position);
        return state with
        {
            Combat = new CombatState(monster, CombatPhase.Resolution, 1, CombatAction.Attack)
        };
    }
}
