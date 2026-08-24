using Telengard.Core.Combat;
using Telengard.Core.Simulation;
using Telengard.Core.World.Generation;
using Xunit;

namespace Telengard.Architecture.Tests;

public sealed class DungeonWalkingTests
{
    [Fact]
    public void Seeded_dungeon_can_be_entered_walked_changed_and_left()
    {
        var generator = new FloorLayoutGenerator();
        var floorOne = generator.Generate(1234, "generator-1", 1);
        var floorTwo = generator.Generate(1234, "generator-1", 2);
        var state = GameState.Create(1234, new GameVersions("0.1", "generator-1", "0.1"));
        var entered = DungeonWalkingResolver.Enter(state, new EnterDungeonCommand(), floorOne);
        var position = entered.State.Player.Position;
        var path = PathTo(floorOne, position, floorOne.StairsDown);

        foreach (var step in path)
        {
            var moved = DungeonWalkingResolver.Move(entered.State, new MoveCommand(step), floorOne);
            entered = moved;
        }

        var changed = FloorTransitionResolver.Apply(entered.State, new ChangeFloorCommand(StairDirection.Down), floorOne, floorTwo);
        var returned = FloorTransitionResolver.Apply(changed.State, new ChangeFloorCommand(StairDirection.Up), floorTwo, floorOne);
        var left = DungeonWalkingResolver.Leave(returned.State, new LeaveDungeonCommand(), floorOne);

        Assert.Equal(floorOne.StairsDown, left.State.Player.Position);
        Assert.Contains(floorOne.StairsUp, left.State.Legacy.PersistentMap.VisitedPositions);
        Assert.NotEmpty(path);
        Assert.Single(changed.Events);
        Assert.IsType<DungeonLeftEvent>(left.Events[0]);
        Assert.IsType<ExpeditionSucceededEvent>(left.Events[1]);
        Assert.False(left.State.Expedition.Active);
    }

    [Fact]
    public void Walls_reject_movement_while_corridors_and_doors_are_traversable()
    {
        var layout = new FloorLayoutGenerator().Generate(1234, "generator-1", 1);
        var (position, wall, walkable) = FindMixedNeighborhood(layout);
        var entered = DungeonWalkingResolver.Enter(GameState.Create(1234), new EnterDungeonCommand(), layout);
        var state = entered.State with { Player = entered.State.Player with { Position = position } };

        Assert.Throws<InvalidOperationException>(() => DungeonWalkingResolver.Move(state, new MoveCommand(wall), layout));
        var moved = DungeonWalkingResolver.Move(state, new MoveCommand(walkable), layout);
        Assert.NotEqual(state.Player.Position, moved.State.Player.Position);
        var movement = Assert.IsType<PlayerMovedEvent>(Assert.Single(moved.Events));
        Assert.Equal(state.Player.Position, movement.From);
        Assert.Equal(moved.State.Player.Position, movement.To);
    }

    [Fact]
    public void Movement_rejects_inactive_and_at_inn_states_before_discovery()
    {
        var layout = new FloorLayoutGenerator().Generate(1234, "generator-1", 1);
        var (position, _, walkable) = FindMixedNeighborhood(layout);
        var inactive = GameState.Create(1234) with
        {
            Player = new PlayerState { Position = position }
        };

        Assert.Throws<InvalidOperationException>(() => DungeonWalkingResolver.Move(
            inactive, new MoveCommand(walkable), layout));
        Assert.Equal(new PersistentMapState(), inactive.Legacy.PersistentMap);

        var atInn = inactive with { Expedition = new ExpeditionState { Active = true } };
        Assert.Throws<InvalidOperationException>(() => DungeonWalkingResolver.Move(
            atInn, new MoveCommand(walkable), layout));
        Assert.Equal(new PersistentMapState(), atInn.Legacy.PersistentMap);
        Assert.Throws<InvalidOperationException>(() => DungeonWalkingResolver.Enter(
            GameState.Create(1234) with { ExpeditionSequence = -1 },
            new EnterDungeonCommand(),
            layout));
    }

    [Fact]
    public void Returning_to_the_inn_secures_carried_gold()
    {
        var layout = new FloorLayoutGenerator().Generate(1234, "generator-1", 1);
        var entered = DungeonWalkingResolver.Enter(GameState.Create(1234), new EnterDungeonCommand(), layout);
        var state = entered.State with
        {
            Player = entered.State.Player with { Position = layout.StairsDown, CarriedGold = 23 },
            Expedition = entered.State.Expedition with { CarriedGold = 23 }
        };

        var returned = DungeonWalkingResolver.Leave(state, new LeaveDungeonCommand(), layout);

        Assert.True(returned.State.Inn.IsAtInn);
        Assert.Equal(0, returned.State.Player.CarriedGold);
        Assert.Equal(0, returned.State.Expedition.CarriedGold);
        Assert.False(returned.State.Expedition.Active);
        Assert.Equal(23, returned.State.SecuredProgress.SecuredGold);
        var secured = Assert.IsType<GoldSecuredEvent>(returned.Events[1]);
        Assert.Equal(23, secured.Amount);
        Assert.Equal(23, secured.SecuredGold);
        var succeeded = Assert.IsType<ExpeditionSucceededEvent>(returned.Events[2]);
        Assert.Equal(entered.State.Expedition.ExpeditionId, succeeded.ExpeditionId);
        Assert.Equal(entered.State.Expedition.DeepestFloorReached, succeeded.DeepestFloorReached);
    }

    [Fact]
    public void Returning_rejects_inconsistent_carried_gold_before_mutation()
    {
        var layout = new FloorLayoutGenerator().Generate(1234, "generator-1", 1);
        var entered = DungeonWalkingResolver.Enter(GameState.Create(1234), new EnterDungeonCommand(), layout);
        var state = entered.State with
        {
            Player = entered.State.Player with { Position = layout.StairsDown, CarriedGold = 1 }
        };

        Assert.Throws<InvalidOperationException>(() => DungeonWalkingResolver.Leave(state, new LeaveDungeonCommand(), layout));
        Assert.False(state.Inn.IsAtInn);
    }

    [Fact]
    public void Returning_rejects_negative_secured_gold_before_mutation()
    {
        var layout = new FloorLayoutGenerator().Generate(1234, "generator-1", 1);
        var entered = DungeonWalkingResolver.Enter(GameState.Create(1234), new EnterDungeonCommand(), layout);
        var state = entered.State with
        {
            Player = entered.State.Player with { Position = layout.StairsDown },
            SecuredProgress = new SecuredProgressState { SecuredGold = -1 }
        };

        Assert.Throws<InvalidOperationException>(() => DungeonWalkingResolver.Leave(state, new LeaveDungeonCommand(), layout));
        Assert.False(state.Inn.IsAtInn);
    }

    [Fact]
    public void Returning_rejects_secured_gold_overflow_before_mutation()
    {
        var layout = new FloorLayoutGenerator().Generate(1234, "generator-1", 1);
        var entered = DungeonWalkingResolver.Enter(GameState.Create(1234), new EnterDungeonCommand(), layout);
        var state = entered.State with
        {
            Player = entered.State.Player with { Position = layout.StairsDown, CarriedGold = 1 },
            Expedition = entered.State.Expedition with { CarriedGold = 1 },
            SecuredProgress = new SecuredProgressState { SecuredGold = int.MaxValue }
        };

        Assert.Throws<OverflowException>(() => DungeonWalkingResolver.Leave(state, new LeaveDungeonCommand(), layout));
        Assert.False(state.Inn.IsAtInn);
    }

    [Fact]
    public void Returning_accepts_the_largest_secured_total()
    {
        var layout = new FloorLayoutGenerator().Generate(1234, "generator-1", 1);
        var entered = DungeonWalkingResolver.Enter(GameState.Create(1234), new EnterDungeonCommand(), layout);
        var state = entered.State with
        {
            Player = entered.State.Player with { Position = layout.StairsDown, CarriedGold = 1 },
            Expedition = entered.State.Expedition with { CarriedGold = 1 },
            SecuredProgress = new SecuredProgressState { SecuredGold = int.MaxValue - 1 }
        };

        var returned = DungeonWalkingResolver.Leave(state, new LeaveDungeonCommand(), layout);

        Assert.Equal(int.MaxValue, returned.State.SecuredProgress.SecuredGold);
    }

    [Fact]
    public void Entering_preserves_persistent_knowledge_from_other_floors()
    {
        var layout = new FloorLayoutGenerator().Generate(1234, "generator-1", 1);
        var otherFloor = new DungeonPosition(2, 3, 4);
        var state = GameState.Create(1234) with
        {
            Legacy = new LegacyState
            {
                PersistentMap = new PersistentMapState([otherFloor], [otherFloor])
            }
        };

        var entered = DungeonWalkingResolver.Enter(state, new EnterDungeonCommand(), layout);

        Assert.Contains(otherFloor, entered.State.Legacy.PersistentMap.ObservedPositions);
        Assert.Contains(otherFloor, entered.State.Legacy.PersistentMap.VisitedPositions);
    }

    [Fact]
    public void Entry_exit_and_movement_validate_all_boundary_paths()
    {
        var generator = new FloorLayoutGenerator();
        var first = generator.Generate(1234, "generator-1", 1);
        var second = generator.Generate(1234, "generator-1", 2);
        var initial = GameState.Create(1234);

        Assert.Throws<InvalidOperationException>(() => DungeonWalkingResolver.Enter(initial, new EnterDungeonCommand(), second));
        Assert.Throws<InvalidOperationException>(() => DungeonWalkingResolver.Enter(
            initial with { Player = new PlayerState { Alive = false } }, new EnterDungeonCommand(), first));
        Assert.Throws<InvalidOperationException>(() => DungeonWalkingResolver.Enter(
            initial with { Inn = new InnState { IsAtInn = false } }, new EnterDungeonCommand(), first));
        var entered = DungeonWalkingResolver.Enter(initial, new EnterDungeonCommand(), first);
        Assert.Throws<InvalidOperationException>(() => DungeonWalkingResolver.Enter(entered.State, new EnterDungeonCommand(), first));

        var deadStart = FindPositionWithDirection(first, MovementDirection.East);
        Assert.Throws<InvalidOperationException>(() => DungeonWalkingResolver.Move(
            entered.State with { Player = entered.State.Player with { Alive = false, Position = deadStart } },
            new MoveCommand(MovementDirection.East),
            first));

        Assert.Throws<InvalidOperationException>(() => DungeonWalkingResolver.Move(
            entered.State with { Player = entered.State.Player with { Position = second.StairsUp } },
            new MoveCommand(MovementDirection.North), first));
        var combatState = entered.State with
        {
            Combat = CombatStateResolver.Begin(new MonsterInstance(
                Guid.Parse("00000000-0000-0000-0000-000000000001"),
                "rat",
                1,
                1,
                entered.State.Player.Position))
        };
        Assert.Throws<InvalidOperationException>(() => DungeonWalkingResolver.Move(
            combatState, new MoveCommand(MovementDirection.North), first));
        Assert.Throws<ArgumentOutOfRangeException>(() => DungeonWalkingResolver.Move(
            entered.State, new MoveCommand((MovementDirection)999), first));

        foreach (var direction in Enum.GetValues<MovementDirection>())
        {
            var candidate = FindPositionWithDirection(first, direction);
            var moved = DungeonWalkingResolver.Move(
                entered.State with { Player = entered.State.Player with { Position = candidate } },
                new MoveCommand(direction), first);
            Assert.Equal(direction, DirectionBetween(candidate, moved.State.Player.Position));
        }

        Assert.Throws<InvalidOperationException>(() => DungeonWalkingResolver.Leave(
            entered.State, new LeaveDungeonCommand(), first));
        Assert.Throws<InvalidOperationException>(() => DungeonWalkingResolver.Leave(
            entered.State with { Player = entered.State.Player with { Position = second.StairsDown } },
            new LeaveDungeonCommand(), second));
        Assert.Throws<InvalidOperationException>(() => DungeonWalkingResolver.Leave(
            combatState with { Player = combatState.Player with { Position = first.StairsDown } },
            new LeaveDungeonCommand(), first));

        Assert.Throws<ArgumentNullException>(() => DungeonWalkingResolver.Enter(null!, new EnterDungeonCommand(), first));
        Assert.Throws<ArgumentNullException>(() => DungeonWalkingResolver.Enter(initial, null!, first));
        Assert.Throws<ArgumentNullException>(() => DungeonWalkingResolver.Enter(initial, new EnterDungeonCommand(), null!));
        Assert.Throws<ArgumentNullException>(() => DungeonWalkingResolver.Move(null!, new MoveCommand(MovementDirection.North), first));
        Assert.Throws<ArgumentNullException>(() => DungeonWalkingResolver.Move(entered.State, null!, first));
        Assert.Throws<ArgumentNullException>(() => DungeonWalkingResolver.Move(entered.State, new MoveCommand(MovementDirection.North), null!));
        Assert.Throws<ArgumentNullException>(() => DungeonWalkingResolver.Leave(null!, new LeaveDungeonCommand(), first));
        Assert.Throws<ArgumentNullException>(() => DungeonWalkingResolver.Leave(entered.State, null!, first));
        Assert.Throws<ArgumentNullException>(() => DungeonWalkingResolver.Leave(entered.State, new LeaveDungeonCommand(), null!));

        foreach (var (position, direction) in new[]
        {
            (new DungeonPosition(1, 0, 0), MovementDirection.West),
            (new DungeonPosition(1, 0, 0), MovementDirection.North),
            (new DungeonPosition(1, first.Width - 1, first.Height - 1), MovementDirection.East),
            (new DungeonPosition(1, first.Width - 1, first.Height - 1), MovementDirection.South)
        })
        {
            Assert.Throws<InvalidOperationException>(() => DungeonWalkingResolver.Move(
                entered.State with { Player = entered.State.Player with { Position = position } },
                new MoveCommand(direction), first));
        }
    }

    [Fact]
    public void Discovering_a_floor_preserves_known_positions_from_other_floors()
    {
        var generator = new FloorLayoutGenerator();
        var first = generator.Generate(1234, "generator-1", 1);
        var floor = generator.Generate(1234, "generator-1", 2);
        var start = FindPositionWithDirection(floor, MovementDirection.East);
        var retained = new DungeonPosition(1, 2, 2);
        var entered = DungeonWalkingResolver.Enter(GameState.Create(1234), new EnterDungeonCommand(), first);
        var descended = FloorTransitionResolver.Apply(
            entered.State with { Player = entered.State.Player with { Position = first.StairsDown } },
            new ChangeFloorCommand(StairDirection.Down),
            first,
            floor);
        var state = descended.State with
        {
            Player = descended.State.Player with { Position = start },
            Legacy = new LegacyState
            {
                PersistentMap = new PersistentMapState([retained], [retained])
            }
        };

        var result = DungeonWalkingResolver.Move(state, new MoveCommand(MovementDirection.East), floor);

        Assert.Contains(retained, result.State.Legacy.PersistentMap.ObservedPositions);
        Assert.Contains(retained, result.State.Legacy.PersistentMap.VisitedPositions);
    }

    [Fact]
    public void Geography_and_discovered_map_revisit_identically()
    {
        var generator = new FloorLayoutGenerator();
        var first = generator.Generate(99, "generator-1", 3);
        var second = generator.Generate(99, "generator-1", 3);
        Assert.Equal(first.Rooms, second.Rooms);
        for (var x = 0; x < first.Width; x++)
            for (var y = 0; y < first.Height; y++)
                Assert.Equal(first.GetTile(new DungeonPosition(3, x, y)), second.GetTile(new DungeonPosition(3, x, y)));

        var observed = new DungeonPosition(3, first.StairsUp.X + 1, first.StairsUp.Y);
        var map = Telengard.Core.World.Visibility.FogOfWarMap.Create(first).Observe([observed]).Visit(first.StairsUp);
        var restored = Telengard.Core.World.Visibility.FogOfWarMap.Create(second, map.ToPersistentState());
        Assert.Equal(map.ObservedPositions, restored.ObservedPositions);
        Assert.Equal(map.VisitedPositions, restored.VisitedPositions);
        Assert.DoesNotContain(new DungeonPosition(3, first.Width - 1, first.Height - 1), restored.ObservedPositions);
    }

    private static (DungeonPosition Position, MovementDirection Wall, MovementDirection Walkable) FindMixedNeighborhood(FloorLayout layout)
    {
        for (var x = 0; x < layout.Width; x++)
            for (var y = 0; y < layout.Height; y++)
            {
                var position = new DungeonPosition(layout.Floor, x, y);
                if (!layout.IsWalkable(position)) continue;
                var directions = Enum.GetValues<MovementDirection>().Where(direction =>
                {
                    var candidate = Offset(position, direction);
                    return candidate.X >= 0 && candidate.X < layout.Width && candidate.Y >= 0 && candidate.Y < layout.Height;
                }).ToArray();
                var wall = directions.FirstOrDefault(direction => !layout.IsWalkable(Offset(position, direction)));
                var walkable = directions.FirstOrDefault(direction => layout.IsWalkable(Offset(position, direction)));
                if (directions.Contains(wall) && directions.Contains(walkable)) return (position, wall, walkable);
            }

        throw new InvalidOperationException("The test layout has no mixed wall/walkable neighborhood.");
    }

    private static DungeonPosition Offset(DungeonPosition position, MovementDirection direction) => direction switch
    {
        MovementDirection.North => new(position.Floor, position.X, position.Y - 1),
        MovementDirection.South => new(position.Floor, position.X, position.Y + 1),
        MovementDirection.East => new(position.Floor, position.X + 1, position.Y),
        MovementDirection.West => new(position.Floor, position.X - 1, position.Y),
        _ => throw new ArgumentOutOfRangeException(nameof(direction))
    };

    private static DungeonPosition FindPositionWithDirection(FloorLayout layout, MovementDirection direction)
    {
        for (var x = 0; x < layout.Width; x++)
            for (var y = 0; y < layout.Height; y++)
            {
                var position = new DungeonPosition(layout.Floor, x, y);
                var next = Offset(position, direction);
                if (layout.IsWalkable(position) && next.X >= 0 && next.X < layout.Width &&
                    next.Y >= 0 && next.Y < layout.Height && layout.IsWalkable(next)) return position;
            }

        throw new InvalidOperationException($"The test layout has no path for {direction}.");
    }

    private static MovementDirection DirectionBetween(DungeonPosition from, DungeonPosition to) =>
        (to.X - from.X, to.Y - from.Y) switch
        {
            (0, -1) => MovementDirection.North,
            (0, 1) => MovementDirection.South,
            (1, 0) => MovementDirection.East,
            (-1, 0) => MovementDirection.West,
            _ => throw new ArgumentOutOfRangeException(nameof(to))
        };

    private static IReadOnlyList<MovementDirection> PathTo(FloorLayout layout, DungeonPosition start, DungeonPosition target)
    {
        var pending = new Queue<(DungeonPosition Position, IReadOnlyList<MovementDirection> Path)>();
        var visited = new HashSet<DungeonPosition> { start };
        pending.Enqueue((start, []));
        while (pending.TryDequeue(out var current))
        {
            if (current.Position == target) return current.Path;
            foreach (var (direction, next) in Neighbors(layout, current.Position))
                if (visited.Add(next)) pending.Enqueue((next, [.. current.Path, direction]));
        }

        throw new InvalidOperationException("The test layout has no path to the stairs.");
    }

    private static IEnumerable<(MovementDirection Direction, DungeonPosition Position)> Neighbors(FloorLayout layout, DungeonPosition position)
    {
        var candidates = new[]
        {
            (MovementDirection.North, new DungeonPosition(position.Floor, position.X, position.Y - 1)),
            (MovementDirection.South, new DungeonPosition(position.Floor, position.X, position.Y + 1)),
            (MovementDirection.East, new DungeonPosition(position.Floor, position.X + 1, position.Y)),
            (MovementDirection.West, new DungeonPosition(position.Floor, position.X - 1, position.Y))
        };
        return candidates.Where(candidate => candidate.Item2.X >= 0 && candidate.Item2.X < layout.Width && candidate.Item2.Y >= 0 && candidate.Item2.Y < layout.Height && layout.IsWalkable(candidate.Item2));
    }
}
