using Telengard.Core.Simulation;
using Telengard.Core.World.Generation;
using Telengard.Save;
using Xunit;

namespace Telengard.Architecture.Tests;

public sealed class ExpeditionStateTests
{
    [Fact]
    public void Entering_the_dungeon_starts_a_deterministic_expedition()
    {
        var layout = new FloorLayoutGenerator().Generate(1234, "generator-1", 1);
        var state = GameState.Create(1234, GameVersions.Current, playerId: Guid.Parse("00000000-0000-0000-0000-000000000001"));

        var result = DungeonWalkingResolver.Enter(state, new EnterDungeonCommand(), layout);

        Assert.True(result.State.Expedition.Active);
        Assert.Equal(Guid.Parse("4f9f6060-a6f2-8a44-e9a2-444389125f60"), result.State.Expedition.ExpeditionId);
        Assert.Equal(1, result.State.Expedition.StartingFloor);
        Assert.Equal(1, result.State.Expedition.DeepestFloorReached);
        Assert.Equal([1], result.State.Expedition.FloorsVisited);
        Assert.Contains(result.Events, domainEvent => domainEvent is ExpeditionStartedEvent);
        Assert.Equal(
            result.State.Expedition.ExpeditionId,
            DungeonWalkingResolver.Enter(state, new EnterDungeonCommand(), layout).State.Expedition.ExpeditionId);
    }

    [Fact]
    public void Sequential_expeditions_without_tick_advance_receive_distinct_replayable_ids()
    {
        var layout = new FloorLayoutGenerator().Generate(1234, "generator-1", 1);

        static Guid[] RunTwoExpeditions(FloorLayout layout)
        {
            var initial = GameState.Create(1234, playerId: Guid.Parse("00000000-0000-0000-0000-000000000001"));
            var first = DungeonWalkingResolver.Enter(initial, new EnterDungeonCommand(), layout);
            var returned = DungeonWalkingResolver.Leave(
                first.State with { Player = first.State.Player with { Position = layout.StairsDown } },
                new LeaveDungeonCommand(),
                layout);
            var second = DungeonWalkingResolver.Enter(returned.State, new EnterDungeonCommand(), layout);

            Assert.Equal(0, second.State.SimulationTick);
            Assert.Equal(2, second.State.ExpeditionSequence);
            return [first.State.Expedition.ExpeditionId!.Value, second.State.Expedition.ExpeditionId!.Value];
        }

        var firstRun = RunTwoExpeditions(layout);
        var secondRun = RunTwoExpeditions(layout);

        Assert.NotEqual(firstRun[0], firstRun[1]);
        Assert.Equal(firstRun, secondRun);
    }

    [Fact]
    public void Save_load_between_expeditions_preserves_the_next_deterministic_identity()
    {
        var layout = new FloorLayoutGenerator().Generate(1234, "generator-1", 1);
        var initial = GameState.Create(1234, playerId: Guid.Parse("00000000-0000-0000-0000-000000000001"));
        var first = DungeonWalkingResolver.Enter(initial, new EnterDungeonCommand(), layout);
        var returned = DungeonWalkingResolver.Leave(
            first.State with { Player = first.State.Player with { Position = layout.StairsDown } },
            new LeaveDungeonCommand(),
            layout).State;
        var restored = SaveGameSerializer.Deserialize(SaveGameSerializer.Serialize(returned));

        var direct = DungeonWalkingResolver.Enter(returned, new EnterDungeonCommand(), layout);
        var afterLoad = DungeonWalkingResolver.Enter(restored, new EnterDungeonCommand(), layout);

        Assert.Equal(direct.State.Expedition.ExpeditionId, afterLoad.State.Expedition.ExpeditionId);
        Assert.Equal(2, afterLoad.State.ExpeditionSequence);
    }

    [Fact]
    public void Entry_rejects_an_active_or_dead_expedition()
    {
        var layout = new FloorLayoutGenerator().Generate(1234, "generator-1", 1);
        var active = GameState.Create(1234) with { Expedition = new ExpeditionState { Active = true } };
        var dead = GameState.Create(1234) with { Player = new PlayerState { Alive = false } };

        Assert.Throws<InvalidOperationException>(() => DungeonWalkingResolver.Enter(active, new EnterDungeonCommand(), layout));
        Assert.Throws<InvalidOperationException>(() => DungeonWalkingResolver.Enter(dead, new EnterDungeonCommand(), layout));
    }

    [Fact]
    public void Active_expedition_tracks_new_deepest_and_visited_floors()
    {
        var generator = new FloorLayoutGenerator();
        var floorOne = generator.Generate(1234, "generator-1", 1);
        var floorTwo = generator.Generate(1234, "generator-1", 2);
        var state = GameState.Create(1234);
        var entered = DungeonWalkingResolver.Enter(state, new EnterDungeonCommand(), floorOne);
        var atStairs = entered.State with { Player = entered.State.Player with { Position = floorOne.StairsDown } };
        var changed = FloorTransitionResolver.Apply(atStairs, new ChangeFloorCommand(StairDirection.Down), floorOne, floorTwo);

        Assert.Equal(2, changed.State.Expedition.DeepestFloorReached);
        Assert.Equal([1, 2], changed.State.Expedition.FloorsVisited);
    }

    [Fact]
    public void Completion_is_deterministic_and_emits_success_after_return()
    {
        var layout = new FloorLayoutGenerator().Generate(1234, "generator-1", 1);
        var entered = DungeonWalkingResolver.Enter(GameState.Create(1234), new EnterDungeonCommand(), layout);
        var state = entered.State with { Player = entered.State.Player with { Position = layout.StairsDown } };

        var first = DungeonWalkingResolver.Leave(state, new LeaveDungeonCommand(), layout);
        var second = DungeonWalkingResolver.Leave(state, new LeaveDungeonCommand(), layout);

        Assert.Equal(first.State, second.State);
        Assert.Equal(first.Events, second.Events);
        var succeeded = Assert.IsType<ExpeditionSucceededEvent>(first.Events[^1]);
        Assert.Equal(state.Expedition.ExpeditionId, succeeded.ExpeditionId);
        Assert.False(first.State.Expedition.Active);
    }

    [Fact]
    public void Expedition_state_fields_round_trip_through_the_save_dto()
    {
        var state = GameState.Create(1234) with
        {
            Player = new PlayerState { CarriedGold = 23 },
            Expedition = new ExpeditionState
            {
                ExpeditionId = Guid.Parse("00000000-0000-0000-0000-000000000002"),
                StartingFloor = 2,
                DeepestFloorReached = 7,
                StartSimulationTick = 11,
                SimulationTicks = 19,
                CarriedGold = 23,
                AcquiredItems = ["relic"],
                MonstersDefeated = 5,
                DiscoveriesMade = ["fountain"],
                FloorsVisited = [2, 4, 7],
                RoomsVisited = 8,
                Objectives = ["reach-depth-7"],
                Active = true
            },
            Inn = new InnState { IsAtInn = false }
        };

        var restored = SaveGameSerializer.Deserialize(SaveGameSerializer.Serialize(state));

        Assert.Equal(state.Expedition.ExpeditionId, restored.Expedition.ExpeditionId);
        Assert.Equal(state.Expedition.StartingFloor, restored.Expedition.StartingFloor);
        Assert.Equal(state.Expedition.DeepestFloorReached, restored.Expedition.DeepestFloorReached);
        Assert.Equal(state.Expedition.StartSimulationTick, restored.Expedition.StartSimulationTick);
        Assert.Equal(state.Expedition.SimulationTicks, restored.Expedition.SimulationTicks);
        Assert.Equal(state.ExpeditionSequence, restored.ExpeditionSequence);
        Assert.Equal(state.Expedition.CarriedGold, restored.Expedition.CarriedGold);
        Assert.Equal(state.Expedition.AcquiredItems, restored.Expedition.AcquiredItems);
        Assert.Equal(state.Expedition.MonstersDefeated, restored.Expedition.MonstersDefeated);
        Assert.Equal(state.Expedition.DiscoveriesMade, restored.Expedition.DiscoveriesMade);
        Assert.Equal(state.Expedition.FloorsVisited, restored.Expedition.FloorsVisited);
        Assert.Equal(state.Expedition.RoomsVisited, restored.Expedition.RoomsVisited);
        Assert.Equal(state.Expedition.Objectives, restored.Expedition.Objectives);
        Assert.Equal(state.Expedition.Active, restored.Expedition.Active);
    }
}
