using Telengard.Core.Combat;
using Telengard.Core.Rng;
using Telengard.Core.Simulation;
using Telengard.Core.World.Generation;
using Xunit;

namespace Telengard.Architecture.Tests;

public sealed class EncounterTriggerTests
{
    [Fact]
    public void Configured_movement_can_start_a_deterministic_encounter()
    {
        var layout = new FloorLayoutGenerator().Generate(1234, "generator-1", 1);
        var entered = DungeonWalkingResolver.Enter(GameState.Create(1234), new EnterDungeonCommand(), layout);
        var start = FindWalkableNeighbor(layout, entered.State.Player.Position);
        var configuration = new EncounterTriggerConfiguration(
            1,
            [new EncounterSpawnOption("crypt-stalker", 2, 12)]);

        var first = DungeonWalkingResolver.Move(
            entered.State with { Player = entered.State.Player with { Position = start } },
            new MoveCommand(DirectionBetween(start, entered.State.Player.Position)),
            layout,
            configuration);
        var second = DungeonWalkingResolver.Move(
            entered.State with { Player = entered.State.Player with { Position = start } },
            new MoveCommand(DirectionBetween(start, entered.State.Player.Position)),
            layout,
            configuration);

        var encounter = Assert.Single(first.Events.OfType<EncounterStartedEvent>());
        Assert.Equal(first.State, second.State);
        Assert.Equal(first.Events, second.Events);
        Assert.Equal("crypt-stalker", encounter.Monster.DefinitionId);
        Assert.Equal(2, encounter.Monster.Level);
        Assert.Equal(12, encounter.Monster.CurrentHitPoints);
        Assert.Equal(first.State.Player.Position, encounter.Monster.Position);
    }

    [Fact]
    public void Zero_chance_or_empty_options_do_not_start_an_encounter()
    {
        var layout = new FloorLayoutGenerator().Generate(1234, "generator-1", 1);
        var entered = DungeonWalkingResolver.Enter(GameState.Create(1234), new EnterDungeonCommand(), layout);

        var zeroChance = EncounterTriggerResolver.Evaluate(
            entered.State,
            entered.State.Player.Position,
            new EncounterTriggerConfiguration(0, [new EncounterSpawnOption("rat", 1, 1)]));
        var noOptions = EncounterTriggerResolver.Evaluate(
            entered.State,
            entered.State.Player.Position,
            new EncounterTriggerConfiguration(1));

        Assert.Empty(zeroChance.Events);
        Assert.Empty(noOptions.Events);
        Assert.Equal(entered.State, zeroChance.State);
        Assert.Equal(entered.State, noOptions.State);
    }

    [Fact]
    public void Trigger_validation_rejects_non_expedition_and_wrong_position_requests()
    {
        var layout = new FloorLayoutGenerator().Generate(1234, "generator-1", 1);
        var initial = GameState.Create(1234);
        var configuration = new EncounterTriggerConfiguration(1, [new EncounterSpawnOption("rat", 1, 1)]);

        Assert.Throws<ArgumentNullException>(() => EncounterTriggerResolver.Evaluate(null!, initial.Player.Position, configuration));
        Assert.Throws<ArgumentNullException>(() => EncounterTriggerResolver.Evaluate(initial, null!, configuration));
        Assert.Throws<ArgumentNullException>(() => EncounterTriggerResolver.Evaluate(initial, initial.Player.Position, null!));
        Assert.Throws<InvalidOperationException>(() => EncounterTriggerResolver.Evaluate(
            initial with { Expedition = new ExpeditionState { Active = false } },
            initial.Player.Position,
            configuration));

        var entered = DungeonWalkingResolver.Enter(initial, new EnterDungeonCommand(), layout);
        var otherPosition = new DungeonPosition(1, entered.State.Player.Position.X + 1, entered.State.Player.Position.Y);
        Assert.Throws<InvalidOperationException>(() => EncounterTriggerResolver.Evaluate(
            entered.State,
            otherPosition,
            configuration));

        var inCombat = entered.State with
        {
            Combat = CombatStateResolver.Begin(new MonsterInstance(
                Guid.Parse("00000000-0000-0000-0000-000000000001"),
                "rat",
                1,
                1,
                entered.State.Player.Position))
        };
        Assert.Throws<InvalidOperationException>(() => EncounterTriggerResolver.Evaluate(
            entered.State with { Player = entered.State.Player with { Alive = false } },
            entered.State.Player.Position,
            configuration));
        Assert.Throws<InvalidOperationException>(() => EncounterTriggerResolver.Evaluate(
            inCombat,
            entered.State.Player.Position,
            configuration));
    }

    [Fact]
    public void Deterministic_non_triggering_roll_preserves_state_without_an_event()
    {
        var position = new DungeonPosition(1, 0, 0);
        var state = GameState.Create(1234) with
        {
            Player = new PlayerState { Position = position },
            Expedition = new ExpeditionState { Active = true }
        };
        var configuration = new EncounterTriggerConfiguration(0.5, [new EncounterSpawnOption("rat", 1, 1)]);

        var result = Enumerable.Range(0, 100)
            .Select(tick => EncounterTriggerResolver.Evaluate(
                state with { SimulationTick = tick }, position, configuration))
            .FirstOrDefault(candidate => candidate.Events.Count == 0);

        Assert.NotNull(result);
        Assert.Equal(state with { SimulationTick = result!.State.SimulationTick }, result.State);
    }

    [Fact]
    public void Configuration_validates_trigger_and_spawn_boundaries()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new EncounterTriggerConfiguration(-0.1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EncounterTriggerConfiguration(double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EncounterTriggerConfiguration(1.1));
        Assert.Throws<ArgumentException>(() => new EncounterSpawnOption("", 1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EncounterSpawnOption("rat", 0, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EncounterSpawnOption("rat", 1, -1));
        Assert.Throws<ArgumentNullException>(() => new EncounterTriggerConfiguration(1, [null!]));
    }

    [Fact]
    public void Trigger_probability_equal_to_the_roll_does_not_start_an_encounter()
    {
        var state = GameState.Create(1234) with
        {
            Player = new PlayerState { Position = new DungeonPosition(1, 0, 0) },
            Expedition = new ExpeditionState { Active = true, ExpeditionId = null }
        };
        var position = state.Player.Position;
        var roll = new DeterministicRng(state.WorldSeed, state.Versions.GeneratorVersion)
            .CreateStream(
                "encounter",
                "expedition:none",
                $"tick:{state.SimulationTick}",
                $"floor:{position.Floor}",
                $"x:{position.X}",
                $"y:{position.Y}")
            .NextDouble();

        Assert.True(roll > 0);
        var result = EncounterTriggerResolver.Evaluate(
            state,
            position,
            new EncounterTriggerConfiguration(roll, [new EncounterSpawnOption("rat", 1, 1)]));

        Assert.Equal(0.070197857916355133d, roll);
        Assert.Equal(state, result.State);
        Assert.Empty(result.Events);
    }

    [Fact]
    public void Encounter_instance_ids_are_stable_for_missing_expedition_ids()
    {
        var state = GameState.Create(1234) with
        {
            Player = new PlayerState { Position = new DungeonPosition(1, 0, 0) },
            Expedition = new ExpeditionState { Active = true, ExpeditionId = null }
        };

        var result = EncounterTriggerResolver.Evaluate(
            state,
            state.Player.Position,
            new EncounterTriggerConfiguration(1, [new EncounterSpawnOption("rat", 1, 1)]));

        var encounter = Assert.IsType<EncounterStartedEvent>(Assert.Single(result.Events));
        Assert.Equal(Guid.Parse("edbf8e93-0c3a-c484-da6f-8f04d3f030db"), encounter.Monster.InstanceId);

        var triggered = EncounterTriggerResolver.Evaluate(
            state,
            state.Player.Position,
            new EncounterTriggerConfiguration(0.8, [new EncounterSpawnOption("rat", 1, 1)]));
        Assert.Single(triggered.Events);
    }

    [Fact]
    public void Trigger_stream_scope_includes_an_expedition_id()
    {
        var id = Guid.Parse("00000000-0000-0000-0000-000000000002");
        var state = GameState.Create(1234) with
        {
            Player = new PlayerState { Position = new DungeonPosition(1, 0, 0) },
            Expedition = new ExpeditionState { Active = true, ExpeditionId = id }
        };
        var roll = new DeterministicRng(state.WorldSeed, state.Versions.GeneratorVersion)
            .CreateStream("encounter", $"expedition:{id}", "tick:0", "floor:1", "x:0", "y:0")
            .NextDouble();

        Assert.Equal(0.48504265397787094d, roll);
        var result = EncounterTriggerResolver.Evaluate(
            state,
            state.Player.Position,
            new EncounterTriggerConfiguration(roll, [new EncounterSpawnOption("rat", 1, 1)]));

        Assert.Equal(state, result.State);
        Assert.Empty(result.Events);
    }

    private static DungeonPosition FindWalkableNeighbor(FloorLayout layout, DungeonPosition origin)
    {
        foreach (var direction in Enum.GetValues<MovementDirection>())
        {
            var candidate = Offset(origin, direction);
            if (candidate.X >= 0 && candidate.X < layout.Width && candidate.Y >= 0 && candidate.Y < layout.Height && layout.IsWalkable(candidate))
            {
                return candidate;
            }
        }

        throw new InvalidOperationException("The generated layout has no walkable neighbor.");
    }

    private static DungeonPosition Offset(DungeonPosition position, MovementDirection direction) => direction switch
    {
        MovementDirection.North => new(position.Floor, position.X, position.Y - 1),
        MovementDirection.South => new(position.Floor, position.X, position.Y + 1),
        MovementDirection.East => new(position.Floor, position.X + 1, position.Y),
        MovementDirection.West => new(position.Floor, position.X - 1, position.Y),
        _ => throw new ArgumentOutOfRangeException(nameof(direction))
    };

    private static MovementDirection DirectionBetween(DungeonPosition from, DungeonPosition to) =>
        (to.X - from.X, to.Y - from.Y) switch
        {
            (0, -1) => MovementDirection.North,
            (0, 1) => MovementDirection.South,
            (1, 0) => MovementDirection.East,
            (-1, 0) => MovementDirection.West,
            _ => throw new ArgumentOutOfRangeException(nameof(to))
        };
}
