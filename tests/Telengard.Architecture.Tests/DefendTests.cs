using Telengard.Core.Combat;
using Telengard.Core.Simulation;
using Telengard.Save;
using Xunit;

namespace Telengard.Architecture.Tests;

public sealed class DefendTests
{
    [Fact]
    public void Defend_resolves_the_selected_action_and_advances_to_enemy_action()
    {
        var result = DefendResolver.Resolve(ActiveCombat(), new DefendCommand());

        Assert.Equal(CombatPhase.EnemyAction, result.State.Combat!.Phase);
        Assert.Equal(CombatAction.Defend, result.State.Combat.SelectedAction);
        var phaseChanged = Assert.IsType<CombatPhaseChangedEvent>(Assert.Single(result.Events));
        Assert.Equal(CombatPhase.Resolution, phaseChanged.From);
        Assert.Equal(CombatPhase.EnemyAction, phaseChanged.To);
        Assert.Equal(CombatAction.Defend, phaseChanged.Action);
    }

    [Fact]
    public void Defend_requires_a_live_expedition_and_selected_defend_resolution()
    {
        var command = new DefendCommand();

        Assert.Throws<ArgumentNullException>(() => DefendResolver.Resolve(null!, command));
        Assert.Throws<ArgumentNullException>(() => DefendResolver.Resolve(ActiveCombat(), null!));
        Assert.Throws<InvalidOperationException>(() => DefendResolver.Resolve(
            ActiveCombat() with { Expedition = new ExpeditionState { Active = false } }, command));
        Assert.Throws<InvalidOperationException>(() => DefendResolver.Resolve(
            ActiveCombat() with { Player = ActiveCombat().Player with { Alive = false } }, command));
        Assert.Throws<InvalidOperationException>(() => DefendResolver.Resolve(
            ActiveCombat() with { Combat = null }, command));
        Assert.Throws<InvalidOperationException>(() => DefendResolver.Resolve(
            ActiveCombat() with { Combat = ActiveCombat().Combat! with { Phase = CombatPhase.PlayerAction } }, command));
        Assert.Throws<InvalidOperationException>(() => DefendResolver.Resolve(
            ActiveCombat() with { Combat = ActiveCombat().Combat! with { SelectedAction = CombatAction.Attack } }, command));
    }

    [Fact]
    public void Equal_defend_inputs_replay_to_equal_state_and_events()
    {
        var first = DefendResolver.Resolve(ActiveCombat(), new DefendCommand());
        var second = DefendResolver.Resolve(ActiveCombat(), new DefendCommand());

        Assert.Equal(first.State, second.State);
        Assert.Equal(first.Events, second.Events);
    }

    [Fact]
    public void A_selected_defend_cannot_bypass_resolution()
    {
        Assert.Throws<InvalidOperationException>(() => CombatStateResolver.Advance(
            ActiveCombat(),
            new AdvanceCombatCommand()));
    }

    [Fact]
    public void Selected_defend_state_round_trips_through_the_existing_save_contract()
    {
        var state = ActiveCombat();
        var restored = SaveGameSerializer.Deserialize(SaveGameSerializer.Serialize(state));

        Assert.Equal(SaveGameSerializer.Serialize(state), SaveGameSerializer.Serialize(restored));
        Assert.Equal(CombatAction.Defend, restored.Combat!.SelectedAction);
    }

    private static GameState ActiveCombat()
    {
        var state = GameState.Create(1234) with
        {
            Expedition = new ExpeditionState { Active = true },
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
                CombatAction.Defend)
        };
    }
}
