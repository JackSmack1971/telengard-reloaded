using Telengard.Core.Economy;
using Telengard.Core.Simulation;
using Telengard.Core.World.Generation;
using Telengard.Save;
using Xunit;

namespace Telengard.Architecture.Tests;

public sealed class Phase2AcceptanceTests
{
    [Fact]
    public void Successful_loop_secures_gold_finishes_and_allows_another_expedition()
    {
        var layout = new FloorLayoutGenerator().Generate(1234, "generator-1", 1);
        var entered = DungeonWalkingResolver.Enter(GameState.Create(1234), new EnterDungeonCommand(), layout);
        var acquired = CarriedGoldResolver.Acquire(entered.State, new AcquireGoldCommand(23));

        Assert.Equal(23, acquired.State.Player.CarriedGold);
        Assert.Equal(23, acquired.State.Expedition.CarriedGold);
        Assert.Equal(0, acquired.State.SecuredProgress.SecuredGold);

        var atSafety = acquired.State with
        {
            Player = acquired.State.Player with { Position = layout.StairsDown }
        };
        var returned = DungeonWalkingResolver.Leave(atSafety, new LeaveDungeonCommand(), layout);

        Assert.True(returned.State.Inn.IsAtInn);
        Assert.False(returned.State.Expedition.Active);
        Assert.Equal(0, returned.State.Player.CarriedGold);
        Assert.Equal(0, returned.State.Expedition.CarriedGold);
        Assert.Equal(23, returned.State.SecuredProgress.SecuredGold);

        var next = DungeonWalkingResolver.Enter(returned.State, new EnterDungeonCommand(), layout);

        Assert.False(next.State.Inn.IsAtInn);
        Assert.True(next.State.Expedition.Active);
        Assert.Equal(0, next.State.Expedition.CarriedGold);
        Assert.Equal([1], next.State.Expedition.FloorsVisited);
        Assert.Equal(1, next.State.Expedition.StartingFloor);
        Assert.Equal(1, next.State.Expedition.DeepestFloorReached);
    }

    [Fact]
    public void Failed_safety_boundary_attempt_does_not_secure_carried_gold()
    {
        var layout = new FloorLayoutGenerator().Generate(1234, "generator-1", 1);
        var entered = DungeonWalkingResolver.Enter(GameState.Create(1234), new EnterDungeonCommand(), layout);
        var state = CarriedGoldResolver.Acquire(entered.State, new AcquireGoldCommand(23)).State;
        var before = state with
        {
            SecuredProgress = new SecuredProgressState { SecuredGold = 11 }
        };

        Assert.Throws<InvalidOperationException>(() => DungeonWalkingResolver.Leave(
            before,
            new LeaveDungeonCommand(),
            layout));

        Assert.Equal(23, before.Player.CarriedGold);
        Assert.Equal(23, before.Expedition.CarriedGold);
        Assert.Equal(11, before.SecuredProgress.SecuredGold);
        Assert.False(before.Inn.IsAtInn);
        Assert.True(before.Expedition.Active);
    }

    [Fact]
    public void Floor_statistics_remain_consistent_through_a_return()
    {
        var generator = new FloorLayoutGenerator();
        var floorOne = generator.Generate(1234, "generator-1", 1);
        var floorTwo = generator.Generate(1234, "generator-1", 2);
        var entered = DungeonWalkingResolver.Enter(GameState.Create(1234), new EnterDungeonCommand(), floorOne);
        var changed = FloorTransitionResolver.Apply(
            entered.State with { Player = entered.State.Player with { Position = floorOne.StairsDown } },
            new ChangeFloorCommand(StairDirection.Down),
            floorOne,
            floorTwo);

        Assert.Equal(1, changed.State.Expedition.StartingFloor);
        Assert.Equal(2, changed.State.Expedition.DeepestFloorReached);
        Assert.Equal([1, 2], changed.State.Expedition.FloorsVisited);
        Assert.Equal(0, changed.State.Expedition.SimulationTicks);
        Assert.Equal(0, changed.State.Expedition.RoomsVisited);
        Assert.Equal(0, changed.State.Expedition.MonstersDefeated);

        var returned = DungeonWalkingResolver.Leave(
            changed.State with { Player = changed.State.Player with { Position = floorOne.StairsDown } },
            new LeaveDungeonCommand(),
            floorOne);

        Assert.Equal(2, returned.State.Expedition.DeepestFloorReached);
        Assert.Equal([1, 2], returned.State.Expedition.FloorsVisited);
        Assert.False(returned.State.Expedition.Active);
    }

    [Fact]
    public void Save_load_during_an_expedition_preserves_the_authoritative_resume_state()
    {
        var layout = new FloorLayoutGenerator().Generate(1234, "generator-1", 1);
        var entered = DungeonWalkingResolver.Enter(GameState.Create(1234), new EnterDungeonCommand(), layout);
        var state = CarriedGoldResolver.Acquire(entered.State, new AcquireGoldCommand(23)).State with
        {
            SecuredProgress = new SecuredProgressState { SecuredGold = 11 }
        };

        var restored = SaveGameSerializer.Deserialize(SaveGameSerializer.Serialize(state));

        Assert.Equal(SaveGameSerializer.Serialize(state), SaveGameSerializer.Serialize(restored));
        Assert.True(restored.Expedition.Active);
        Assert.False(restored.Inn.IsAtInn);
        Assert.Equal(23, restored.Player.CarriedGold);
        Assert.Equal(23, restored.Expedition.CarriedGold);
        Assert.Equal(11, restored.SecuredProgress.SecuredGold);
    }
}
