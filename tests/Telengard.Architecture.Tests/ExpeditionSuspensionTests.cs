using Telengard.Core.Meta;
using Telengard.Core.Simulation;
using Telengard.Save;
using Xunit;

namespace Telengard.Architecture.Tests;

public sealed class ExpeditionSuspensionTests
{
    [Fact]
    public void Active_expedition_can_be_suspended_without_mutating_resume_state()
    {
        var state = ActiveExpedition();

        var result = ExpeditionSuspensionResolver.Suspend(state, new SuspendExpeditionCommand());

        Assert.Equal(state, result.State);
        var suspended = Assert.IsType<GameSuspendedEvent>(Assert.Single(result.Events));
        Assert.Equal(state.Expedition.ExpeditionId, suspended.ExpeditionId);
        Assert.Equal(state.Player.Position, suspended.Position);
    }

    [Fact]
    public void Suspension_requires_a_living_active_expedition_outside_the_inn()
    {
        var command = new SuspendExpeditionCommand();

        Assert.Throws<InvalidOperationException>(() => ExpeditionSuspensionResolver.Suspend(
            GameState.Create(1234), command));
        Assert.Throws<InvalidOperationException>(() => ExpeditionSuspensionResolver.Suspend(
            ActiveExpedition() with { Inn = new InnState { IsAtInn = true } }, command));
        Assert.Throws<InvalidOperationException>(() => ExpeditionSuspensionResolver.Suspend(
            ActiveExpedition() with { Player = new PlayerState { Alive = false } }, command));
    }

    [Fact]
    public void Suspension_is_deterministic_and_the_saved_state_resumes_identically()
    {
        var state = ActiveExpedition() with
        {
            Player = ActiveExpedition().Player with { CarriedGold = 23 },
            Expedition = ActiveExpedition().Expedition with { CarriedGold = 23 },
            SecuredProgress = new SecuredProgressState { SecuredGold = 11 }
        };

        var first = ExpeditionSuspensionResolver.Suspend(state, new SuspendExpeditionCommand());
        var second = ExpeditionSuspensionResolver.Suspend(state, new SuspendExpeditionCommand());
        var restored = SaveGameSerializer.Deserialize(SaveGameSerializer.Serialize(first.State));

        Assert.Equal(first.State, second.State);
        Assert.Equal(first.Events, second.Events);
        Assert.Equal(SaveGameSerializer.Serialize(first.State), SaveGameSerializer.Serialize(restored));
        Assert.True(restored.Expedition.Active);
        Assert.False(restored.Inn.IsAtInn);
        Assert.Equal(23, restored.Expedition.CarriedGold);
        Assert.Equal(23, restored.Player.CarriedGold);
        Assert.Equal(11, restored.SecuredProgress.SecuredGold);
    }

    [Fact]
    public void Suspension_validates_null_inputs()
    {
        Assert.Throws<ArgumentNullException>(() => ExpeditionSuspensionResolver.Suspend(
            null!, new SuspendExpeditionCommand()));
        Assert.Throws<ArgumentNullException>(() => ExpeditionSuspensionResolver.Suspend(
            ActiveExpedition(), null!));
    }

    private static GameState ActiveExpedition() => GameState.Create(1234) with
    {
        Player = new PlayerState { Id = Guid.Parse("00000000-0000-0000-0000-000000000001"), Position = new(3, 8, 9) },
        Expedition = new ExpeditionState
        {
            ExpeditionId = Guid.Parse("00000000-0000-0000-0000-000000000002"),
            StartingFloor = 1,
            DeepestFloorReached = 3,
            FloorsVisited = [1, 3],
            Active = true
        },
        Inn = new InnState { IsAtInn = false }
    };
}
