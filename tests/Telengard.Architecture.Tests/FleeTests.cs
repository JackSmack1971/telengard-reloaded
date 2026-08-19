using Telengard.Core.Combat;
using Telengard.Core.Rng;
using Telengard.Core.Simulation;
using Telengard.Save;
using Xunit;

namespace Telengard.Architecture.Tests;

public sealed class FleeTests
{
    [Fact]
    public void Successful_flee_closes_combat_and_emits_encounter_end()
    {
        var result = FleeResolver.Resolve(
            ActiveCombat(),
            new FleeCommand(),
            new FleeConfiguration(1));

        Assert.Null(result.State.Combat);
        var ended = Assert.IsType<EncounterEndedEvent>(Assert.Single(result.Events));
        Assert.Equal(Guid.Parse("00000000-0000-0000-0000-000000000001"), ended.EncounterId);
    }

    [Fact]
    public void Failed_flee_keeps_combat_active_and_advances_to_enemy_action()
    {
        var result = FleeResolver.Resolve(
            ActiveCombat(),
            new FleeCommand(),
            new FleeConfiguration(0));

        Assert.Equal(CombatPhase.EnemyAction, result.State.Combat!.Phase);
        Assert.Equal(CombatAction.Flee, result.State.Combat.SelectedAction);
        var phaseChanged = Assert.IsType<CombatPhaseChangedEvent>(Assert.Single(result.Events));
        Assert.Equal(CombatPhase.Resolution, phaseChanged.From);
        Assert.Equal(CombatPhase.EnemyAction, phaseChanged.To);
        Assert.Equal(CombatAction.Flee, phaseChanged.Action);
    }

    [Fact]
    public void Equal_flee_inputs_replay_to_equal_state_and_events()
    {
        var first = FleeResolver.Resolve(ActiveCombat(), new FleeCommand(), new FleeConfiguration(0.5));
        var second = FleeResolver.Resolve(ActiveCombat(), new FleeCommand(), new FleeConfiguration(0.5));

        Assert.Equal(first.State, second.State);
        Assert.Equal(first.Events, second.Events);
    }

    [Fact]
    public void Flee_requires_a_live_expedition_and_selected_flee_resolution()
    {
        var command = new FleeCommand();
        var configuration = new FleeConfiguration(1);

        Assert.Throws<ArgumentNullException>(() => FleeResolver.Resolve(null!, command, configuration));
        Assert.Throws<ArgumentNullException>(() => FleeResolver.Resolve(ActiveCombat(), null!, configuration));
        Assert.Throws<ArgumentNullException>(() => FleeResolver.Resolve(ActiveCombat(), command, null!));
        Assert.Throws<InvalidOperationException>(() => FleeResolver.Resolve(
            ActiveCombat() with { Expedition = new ExpeditionState { Active = false } }, command, configuration));
        Assert.Throws<InvalidOperationException>(() => FleeResolver.Resolve(
            ActiveCombat() with { Player = ActiveCombat().Player with { Alive = false } }, command, configuration));
        Assert.Throws<InvalidOperationException>(() => FleeResolver.Resolve(
            ActiveCombat() with { Combat = null }, command, configuration));
        Assert.Throws<InvalidOperationException>(() => FleeResolver.Resolve(
            ActiveCombat() with { Combat = ActiveCombat().Combat! with { Phase = CombatPhase.PlayerAction } },
            command,
            configuration));
        Assert.Throws<InvalidOperationException>(() => FleeResolver.Resolve(
            ActiveCombat() with { Combat = ActiveCombat().Combat! with { SelectedAction = CombatAction.Defend } },
            command,
            configuration));
    }

    [Fact]
    public void A_selected_flee_cannot_bypass_resolution()
    {
        Assert.Throws<InvalidOperationException>(() => CombatStateResolver.Advance(
            ActiveCombat(),
            new AdvanceCombatCommand()));
    }

    [Fact]
    public void Flee_configuration_requires_a_probability_between_zero_and_one()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new FleeConfiguration(-0.1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new FleeConfiguration(double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => new FleeConfiguration(1.1));
    }

    [Fact]
    public void Flee_probability_equal_to_the_roll_is_a_failed_flee()
    {
        var state = ActiveCombat();
        var roll = new DeterministicRng(state.WorldSeed, state.Versions.GeneratorVersion)
            .CreateStream("flee", $"encounter:{state.Combat!.EncounterId}", $"round:{state.Combat.Round}")
            .NextDouble();

        Assert.True(roll > 0);
        var result = FleeResolver.Resolve(state, new FleeCommand(), new FleeConfiguration(roll));

        Assert.Equal(CombatPhase.EnemyAction, result.State.Combat!.Phase);
        Assert.Single(result.Events);
    }

    [Fact]
    public void Selected_flee_state_round_trips_through_the_existing_save_contract()
    {
        var state = ActiveCombat();
        var restored = SaveGameSerializer.Deserialize(SaveGameSerializer.Serialize(state));

        Assert.Equal(SaveGameSerializer.Serialize(state), SaveGameSerializer.Serialize(restored));
        Assert.Equal(CombatAction.Flee, restored.Combat!.SelectedAction);
        Assert.Equal(GameState.CurrentSaveVersion, restored.SaveVersion);
    }

    private static GameState ActiveCombat()
    {
        var state = GameState.Create(1234) with
        {
            Expedition = new ExpeditionState { Active = true },
            Inn = new InnState { IsAtInn = false },
            Player = new PlayerState { Position = new DungeonPosition(1, 0, 0) }
        };
        return state with
        {
            Combat = new CombatState(
                new MonsterInstance(
                    Guid.Parse("00000000-0000-0000-0000-000000000001"),
                    "rat",
                    1,
                    3,
                    state.Player.Position),
                CombatPhase.Resolution,
                1,
                CombatAction.Flee)
        };
    }
}
