using Telengard.Core.Combat;
using Telengard.Core.Simulation;
using Telengard.Core.World.Generation;
using Xunit;

namespace Telengard.Architecture.Tests;

public sealed class FloorTransitionTests
{
    [Fact]
    public void Down_stairs_move_the_player_to_the_next_floor_and_emit_an_event()
    {
        var generator = new FloorLayoutGenerator();
        var current = generator.Generate(1234, "generator-1", 1);
        var next = generator.Generate(1234, "generator-1", 2);
        var entered = DungeonWalkingResolver.Enter(GameState.Create(1234), new EnterDungeonCommand(), current);
        var dispatcher = new CommandDispatcher(entered.State with
        {
            Player = entered.State.Player with { Position = current.StairsDown }
        });
        dispatcher.Register<ChangeFloorCommand>((state, command) =>
            FloorTransitionResolver.Apply(state, command, current, next));

        var result = dispatcher.Dispatch(new ChangeFloorCommand(StairDirection.Down));

        Assert.Equal(next.StairsUp, result.State.Player.Position);
        var changed = Assert.IsType<FloorChangedEvent>(Assert.Single(result.Events));
        Assert.Equal(current.StairsDown, changed.From);
        Assert.Equal(next.StairsUp, changed.To);
        Assert.Equal(StairDirection.Down, changed.Direction);
    }

    [Fact]
    public void Up_stairs_return_the_player_to_the_previous_floor()
    {
        var generator = new FloorLayoutGenerator();
        var previous = generator.Generate(1234, "generator-1", 1);
        var current = generator.Generate(1234, "generator-1", 2);
        var entered = DungeonWalkingResolver.Enter(GameState.Create(1234), new EnterDungeonCommand(), previous);
        var descended = FloorTransitionResolver.Apply(
            entered.State with { Player = entered.State.Player with { Position = previous.StairsDown } },
            new ChangeFloorCommand(StairDirection.Down),
            previous,
            current);
        var state = descended.State with
        {
            Player = new PlayerState { Position = current.StairsUp }
        };

        var result = FloorTransitionResolver.Apply(
            state,
            new ChangeFloorCommand(StairDirection.Up),
            current,
            previous);

        Assert.Equal(previous.StairsDown, result.State.Player.Position);
    }

    [Fact]
    public void Transition_rejects_wrong_stairs_and_floor_boundaries_without_mutation()
    {
        var generator = new FloorLayoutGenerator();
        var first = generator.Generate(1234, "generator-1", 1);
        var second = generator.Generate(1234, "generator-1", 2);
        var entered = DungeonWalkingResolver.Enter(GameState.Create(1234), new EnterDungeonCommand(), first);
        var state = entered.State with
        {
            Player = entered.State.Player with { Position = first.StairsUp }
        };

        Assert.Throws<InvalidOperationException>(() => FloorTransitionResolver.Apply(
            state, new ChangeFloorCommand(StairDirection.Down), first, second));
        Assert.Equal(first.StairsUp, state.Player.Position);

        Assert.Throws<InvalidOperationException>(() => FloorTransitionResolver.Apply(
            state,
            new ChangeFloorCommand(StairDirection.Up),
            first,
            first));
        var inCombat = state with
        {
            Player = state.Player with { Position = first.StairsDown },
            Combat = CombatStateResolver.Begin(new MonsterInstance(
                Guid.Parse("00000000-0000-0000-0000-000000000001"),
                "rat",
                1,
                1,
                state.Player.Position))
        };
        Assert.Throws<InvalidOperationException>(() => FloorTransitionResolver.Apply(
            inCombat, new ChangeFloorCommand(StairDirection.Down), first, second));

        Assert.Throws<InvalidOperationException>(() => FloorTransitionResolver.Apply(
            state with { Player = state.Player with { Alive = false, Position = first.StairsDown } },
            new ChangeFloorCommand(StairDirection.Down),
            first,
            second));
    }

    [Fact]
    public void Transition_validates_direction_position_and_destination_boundaries()
    {
        var generator = new FloorLayoutGenerator();
        var first = generator.Generate(1234, "generator-1", 1);
        var second = generator.Generate(1234, "generator-1", 2);
        var entered = DungeonWalkingResolver.Enter(GameState.Create(1234), new EnterDungeonCommand(), first);
        var state = entered.State with
        {
            Player = entered.State.Player with { Position = first.StairsDown }
        };

        Assert.Throws<ArgumentOutOfRangeException>(() => FloorTransitionResolver.Apply(
            state, new ChangeFloorCommand((StairDirection)999), first, second));
        Assert.Throws<InvalidOperationException>(() => FloorTransitionResolver.Apply(
            state with { Player = new PlayerState { Position = new DungeonPosition(1, 0, 0) } },
            new ChangeFloorCommand(StairDirection.Down), first, second));
        Assert.Throws<InvalidOperationException>(() => FloorTransitionResolver.Apply(
            state with { Player = new PlayerState { Position = new DungeonPosition(2, first.StairsDown.X, first.StairsDown.Y) } },
            new ChangeFloorCommand(StairDirection.Down), first, second));
        Assert.Throws<InvalidOperationException>(() => FloorTransitionResolver.Apply(
            state, new ChangeFloorCommand(StairDirection.Down), first, first));

        var active = state with
        {
            Expedition = new ExpeditionState { Active = true, FloorsVisited = [1] }
        };
        var result = FloorTransitionResolver.Apply(active, new ChangeFloorCommand(StairDirection.Down), first, second);

        Assert.Equal([1, 2], result.State.Expedition.FloorsVisited);
        Assert.Equal(2, result.State.Expedition.DeepestFloorReached);

        var alreadyVisited = active with
        {
            Expedition = active.Expedition with { FloorsVisited = [1, 2] }
        };
        var revisited = FloorTransitionResolver.Apply(
            alreadyVisited, new ChangeFloorCommand(StairDirection.Down), first, second);
        Assert.Equal([1, 2], revisited.State.Expedition.FloorsVisited);

        var fortyNine = generator.Generate(1234, "generator-1", 49);
        var fifty = generator.Generate(1234, "generator-1", 50);
        var boundary = state with { Player = state.Player with { Position = fortyNine.StairsDown } };
        var boundaryResult = FloorTransitionResolver.Apply(
            boundary, new ChangeFloorCommand(StairDirection.Down), fortyNine, fifty);
        Assert.Equal(fifty.StairsUp, boundaryResult.State.Player.Position);
    }

    [Fact]
    public void Transition_rejects_null_arguments_and_invalid_lifecycle_states()
    {
        var generator = new FloorLayoutGenerator();
        var first = generator.Generate(1234, "generator-1", 1);
        var second = generator.Generate(1234, "generator-1", 2);
        var state = GameState.Create(1234) with { Player = new PlayerState { Position = first.StairsDown } };

        Assert.Throws<ArgumentNullException>(() => FloorTransitionResolver.Apply(null!, new ChangeFloorCommand(StairDirection.Down), first, second));
        Assert.Throws<ArgumentNullException>(() => FloorTransitionResolver.Apply(state, null!, first, second));
        Assert.Throws<ArgumentNullException>(() => FloorTransitionResolver.Apply(state, new ChangeFloorCommand(StairDirection.Down), null!, second));
        Assert.Throws<ArgumentNullException>(() => FloorTransitionResolver.Apply(state, new ChangeFloorCommand(StairDirection.Down), first, null!));

        var inactive = state with
        {
            Expedition = new ExpeditionState { StartingFloor = 4, DeepestFloorReached = 4, FloorsVisited = [4] }
        };
        var inactiveBefore = inactive;
        Assert.Throws<InvalidOperationException>(() => FloorTransitionResolver.Apply(
            inactive, new ChangeFloorCommand(StairDirection.Down), first, second));
        Assert.Same(inactiveBefore, inactive);

        var atInn = inactive with { Expedition = new ExpeditionState { Active = true } };
        var atInnBefore = atInn;
        Assert.Throws<InvalidOperationException>(() => FloorTransitionResolver.Apply(
            atInn, new ChangeFloorCommand(StairDirection.Down), first, second));
        Assert.Same(atInnBefore, atInn);
    }
}
