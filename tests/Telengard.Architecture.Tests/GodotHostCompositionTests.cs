using System.Text.Json;
using Telengard.Content;
using Telengard.Core.Combat;
using Telengard.Core.Events;
using Telengard.Core.Items;
using Telengard.Core.Magic;
using Telengard.Core.Simulation;
using Telengard.Core.World.Generation;
using Telengard.Core.World.Features;
using Telengard.GodotHost;
using Xunit;

namespace Telengard.Architecture.Tests;

public sealed class GodotHostCompositionTests
{
    [Fact]
    public void Floor_layout_cache_is_deterministic_and_bounded_to_the_mvp()
    {
        var first = new FloorLayoutCache(1234, "generator-1");
        var second = new FloorLayoutCache(1234, "generator-1");

        Assert.Same(first.Get(3), first.Get(3));
        Assert.Equal(first.Get(3).StairsUp, second.Get(3).StairsUp);
        Assert.Equal(first.Get(3).StairsDown, second.Get(3).StairsDown);
        Assert.Equal(first.Get(3).Rooms, second.Get(3).Rooms);
        for (var x = 0; x < first.Get(3).Width; x++)
            for (var y = 0; y < first.Get(3).Height; y++)
                Assert.Equal(first.Get(3).GetTile(new DungeonPosition(3, x, y)), second.Get(3).GetTile(new DungeonPosition(3, x, y)));
        Assert.Throws<InvalidOperationException>(() => first.Get(6));
    }

    [Fact]
    public void Host_routes_floor_changes_against_current_and_destination_layouts()
    {
        var layouts = new FloorLayoutCache(1234, "generator-1");
        var floorOne = layouts.Get(1);
        var state = GameState.Create(1234) with
        {
            Inn = new InnState { IsAtInn = false },
            Expedition = new ExpeditionState { Active = true, FloorsVisited = [1] },
            Player = new PlayerState { Alive = true, Position = floorOne.StairsDown }
        };
        var session = CreateSession(state, layouts);

        session.Dispatch(Request("{\"type\":\"change_floor\",\"direction\":\"Down\"}"));

        Assert.Equal(2, session.CurrentState.Player.Position.Floor);
        Assert.Equal(layouts.Get(2).StairsUp, session.CurrentState.Player.Position);
    }

    [Fact]
    public void Host_traverses_and_revisits_all_mvp_floors_using_each_cached_layout()
    {
        var layouts = new FloorLayoutCache(1234, "generator-1");
        var floorOne = layouts.Get(1);
        var state = GameState.Create(1234) with
        {
            Inn = new InnState { IsAtInn = false },
            Expedition = new ExpeditionState { Active = true, FloorsVisited = [1] },
            Player = new PlayerState { Alive = true, Position = floorOne.StairsDown }
        };
        var session = CreateSession(state, layouts);

        for (var floor = 1; floor < 5; floor++)
        {
            session.Dispatch(Request("{\"type\":\"change_floor\",\"direction\":\"Down\"}"));
            Assert.Equal(layouts.Get(floor + 1).StairsUp, session.CurrentState.Player.Position);
            MoveTo(session, layouts.Get(floor + 1), layouts.Get(floor + 1).StairsDown);
        }

        for (var floor = 5; floor > 1; floor--)
        {
            MoveTo(session, layouts.Get(floor), layouts.Get(floor).StairsUp);
            session.Dispatch(Request("{\"type\":\"change_floor\",\"direction\":\"Up\"}"));
            Assert.Equal(layouts.Get(floor - 1).StairsDown, session.CurrentState.Player.Position);
        }

        Assert.Contains(layouts.Get(2).StairsDown, session.CurrentState.Legacy.PersistentMap.VisitedPositions);
        var beforeInvalidTransition = session.CurrentState;
        Assert.Throws<InvalidOperationException>(() => session.Dispatch(Request("{\"type\":\"change_floor\",\"direction\":\"Up\"}")));
        Assert.Equal(beforeInvalidTransition, session.CurrentState);
    }

    [Fact]
    public void Host_rejects_mvp_boundary_transition_before_loading_an_out_of_range_layout()
    {
        var layouts = new FloorLayoutCache(1234, "generator-1");
        var floorFive = layouts.Get(5);
        var initial = GameState.Create(1234) with
        {
            Inn = new InnState { IsAtInn = false },
            Expedition = new ExpeditionState { Active = true, FloorsVisited = [1, 5] },
            Player = new PlayerState { Alive = true, Position = floorFive.StairsDown }
        };
        var session = CreateSession(initial, layouts);

        Assert.Throws<InvalidOperationException>(() => session.Dispatch(Request("{\"type\":\"change_floor\",\"direction\":\"Down\"}")));
        Assert.Equal(initial, session.CurrentState);
    }

    [Fact]
    public void Host_composes_floor_one_leave_boundary()
    {
        var layouts = new FloorLayoutCache(1234, "generator-1");
        var floorOne = layouts.Get(1);
        var initial = GameState.Create(1234) with
        {
            Inn = new InnState { IsAtInn = false },
            Expedition = new ExpeditionState { Active = true, FloorsVisited = [1], CarriedGold = 7 },
            Player = new PlayerState { Alive = true, Position = floorOne.StairsDown, CarriedGold = 7 }
        };
        var session = CreateSession(initial, layouts);

        session.Dispatch(Request("{\"type\":\"leave_dungeon\"}"));

        Assert.True(session.CurrentState.Inn.IsAtInn);
        Assert.False(session.CurrentState.Expedition.Active);
        Assert.Equal(7, session.CurrentState.SecuredProgress.SecuredGold);
    }

    [Fact]
    public void Host_composition_delegates_attack_spell_and_equipment_to_core()
    {
        var attackState = ActiveCombat(CombatAction.Attack, hitPoints: 5);
        var attackSession = CreateSession(attackState);
        attackSession.Dispatch(Request("{\"type\":\"resolve_combat_action\"}"));
        Assert.Equal(2, attackSession.CurrentState.Combat!.Monster.CurrentHitPoints);
        Assert.Equal(CombatPhase.EnemyAction, attackSession.CurrentState.Combat.Phase);

        var spellState = ActiveCombat(CombatAction.CastSpell, hitPoints: 5) with
        {
            Player = ActiveCombat(CombatAction.CastSpell, 5).Player with
            {
                SpellPower = 5,
                MaxSpellPower = 5,
                Spells = ["ember-bolt"]
            }
        };
        var spellSession = CreateSession(spellState);
        spellSession.Dispatch(Request("{\"type\":\"cast_spell\",\"spell_id\":\"ember-bolt\"}"));
        Assert.Equal(2, spellSession.CurrentState.Player.SpellPower);
        Assert.Equal(CombatPhase.EnemyAction, spellSession.CurrentState.Combat!.Phase);

        var itemInstanceId = Guid.Parse("00000000-0000-0000-0000-000000000201");
        var equipmentState = GameState.Create(1234) with
        {
            Player = new PlayerState
            {
                Alive = true,
                EquipmentSlots = [new EquipmentSlotState("weapon")]
            }
        };
        var equipmentSession = CreateSession(equipmentState);
        equipmentSession.Dispatch(Request($"{{\"type\":\"equip_item\",\"slot_id\":\"weapon\",\"item_instance_id\":\"{itemInstanceId}\"}}"));
        Assert.Equal(itemInstanceId, equipmentSession.CurrentState.Player.EquipmentSlots[0].ItemInstanceId);
    }

    [Fact]
    public void Host_rejects_unknown_spell_and_invalid_combat_state_without_mutation()
    {
        var initial = ActiveCombat(CombatAction.CastSpell, hitPoints: 5) with
        {
            Player = ActiveCombat(CombatAction.CastSpell, 5).Player with
            {
                SpellPower = 5,
                MaxSpellPower = 5,
                Spells = ["ember-bolt"]
            }
        };
        var session = CreateSession(initial);

        Assert.Throws<KeyNotFoundException>(() => session.Dispatch(Request("{\"type\":\"cast_spell\",\"spell_id\":\"missing-spell\"}")));
        Assert.Equal(initial, session.CurrentState);

        var invalid = initial with
        {
            Combat = initial.Combat! with { Phase = CombatPhase.PlayerAction, SelectedAction = null }
        };
        var invalidSession = CreateSession(invalid);
        Assert.Throws<InvalidOperationException>(() => invalidSession.Dispatch(Request("{\"type\":\"cast_spell\",\"spell_id\":\"ember-bolt\"}")));
        Assert.Equal(invalid, invalidSession.CurrentState);
    }

    [Fact]
    public void Host_movement_uses_authored_encounter_table_and_feature_interaction_discovers_content()
    {
        var layouts = new FloorLayoutCache(1234, "generator-1");
        var start = layouts.Get(1).StairsUp;
        var next = FindWalkableNeighbor(layouts.Get(1), start);
        var feature = new FeatureInstance(Guid.Parse("00000000-0000-0000-0000-000000000301"), "azure-fountain", next);
        var state = ActiveState(start) with { Dungeon = new DungeonState { Features = [feature] } };
        var encounterSession = CreateSession(ActiveState(start), layouts, encounterChance: 1);

        encounterSession.Dispatch(Request($"{{\"type\":\"move\",\"direction\":\"{DirectionBetween(start, next)}\"}}"));

        Assert.NotNull(encounterSession.CurrentState.Combat);
        Assert.Contains(encounterSession.ContentPack.EncounterTables.GetRequired("upper-ruins-encounters").Entries,
            entry => entry.MonsterId == encounterSession.CurrentState.Combat!.Monster.DefinitionId);

        var featureSession = CreateSession(state, layouts);
        featureSession.Dispatch(Request($"{{\"type\":\"move\",\"direction\":\"{DirectionBetween(start, next)}\"}}"));
        featureSession.Dispatch(Request("{\"type\":\"interact\"}"));
        Assert.True(featureSession.CurrentState.Dungeon.Features.Single().Discovered);
    }

    [Fact]
    public void Host_collect_treasure_uses_production_loot_and_keeps_it_unsecured()
    {
        var session = CreateSession(ActiveState(new DungeonPosition(1, 0, 0)));

        session.Dispatch(Request("{\"type\":\"collect_treasure\"}"));

        Assert.Single(session.CurrentState.Expedition.AcquiredItems);
        Assert.Equal(0, session.CurrentState.SecuredProgress.SecuredGold);
    }

    private static GodotSession CreateSession(GameState state)
        => CreateSession(state, new FloorLayoutCache(state.WorldSeed, state.Versions.GeneratorVersion));

    private static GodotSession CreateSession(GameState state, FloorLayoutCache layouts, double encounterChance = 0)
    {
        var events = new List<IDomainEvent>();
        var dispatcher = new CommandDispatcher(state, new DomainEventBus());
        var pack = ContentPackLoader.Load(RepositoryContentRoot());
        return new GodotSession(
            dispatcher,
            layouts,
            events,
            pack,
            new GodotGameplayConfiguration(
                new AttackConfiguration(3),
                new FleeConfiguration(1),
                new ThreatClassificationConfiguration(0, 2, ["cave-rat"]),
                encounterChance));
    }

    private static GameState ActiveState(DungeonPosition position) => GameState.Create(1234) with
    {
        Inn = new InnState { IsAtInn = false },
        Expedition = new ExpeditionState { Active = true, FloorsVisited = [1] },
        Player = new PlayerState { Alive = true, Position = position }
    };

    private static DungeonPosition FindWalkableNeighbor(FloorLayout layout, DungeonPosition origin)
    {
        foreach (var direction in Enum.GetValues<MovementDirection>())
        {
            var candidate = direction switch
            {
                MovementDirection.North => new DungeonPosition(origin.Floor, origin.X, origin.Y - 1),
                MovementDirection.South => new DungeonPosition(origin.Floor, origin.X, origin.Y + 1),
                MovementDirection.East => new DungeonPosition(origin.Floor, origin.X + 1, origin.Y),
                _ => new DungeonPosition(origin.Floor, origin.X - 1, origin.Y)
            };
            if (candidate.X >= 0 && candidate.Y >= 0 && candidate.X < layout.Width && candidate.Y < layout.Height && layout.IsWalkable(candidate)) return candidate;
        }

        throw new InvalidOperationException("No walkable neighbor found.");
    }

    private static MovementDirection DirectionBetween(DungeonPosition from, DungeonPosition to) =>
        (to.X - from.X, to.Y - from.Y) switch
        {
            (0, -1) => MovementDirection.North,
            (0, 1) => MovementDirection.South,
            (1, 0) => MovementDirection.East,
            (-1, 0) => MovementDirection.West,
            _ => throw new InvalidOperationException("Positions are not adjacent.")
        };

    private static GameState ActiveCombat(CombatAction action, int hitPoints)
    {
        var position = new DungeonPosition(1, 0, 0);
        return GameState.Create(1234) with
        {
            Inn = new InnState { IsAtInn = false },
            Expedition = new ExpeditionState { Active = true },
            Player = new PlayerState
            {
                Alive = true,
                HitPoints = 10,
                MaxHitPoints = 10,
                Position = position
            },
            Combat = new CombatState(
                new MonsterInstance(
                    Guid.Parse("00000000-0000-0000-0000-000000000202"),
                    "cave-rat",
                    1,
                    hitPoints,
                    position),
                CombatPhase.Resolution,
                selectedAction: action)
        };
    }

    private static JsonElement Request(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }

    private static void MoveTo(GodotSession session, FloorLayout layout, DungeonPosition destination)
    {
        var start = session.CurrentState.Player.Position;
        var queue = new Queue<(DungeonPosition Position, IReadOnlyList<MovementDirection> Path)>();
        var visited = new HashSet<DungeonPosition> { start };
        queue.Enqueue((start, []));
        while (queue.Count > 0)
        {
            var (position, path) = queue.Dequeue();
            if (position == destination)
            {
                foreach (var direction in path)
                    session.Dispatch(Request($"{{\"type\":\"move\",\"direction\":\"{direction}\"}}"));
                Assert.Equal(destination, session.CurrentState.Player.Position);
                return;
            }

            foreach (var (direction, next) in Neighbors(position))
            {
                if (next.X < 0 || next.X >= layout.Width || next.Y < 0 || next.Y >= layout.Height || !layout.IsWalkable(next) || !visited.Add(next))
                    continue;
                queue.Enqueue((next, [.. path, direction]));
            }
        }

        throw new InvalidOperationException($"No walkable path to {destination}.");
    }

    private static IEnumerable<(MovementDirection Direction, DungeonPosition Position)> Neighbors(DungeonPosition position)
    {
        yield return (MovementDirection.North, new DungeonPosition(position.Floor, position.X, position.Y - 1));
        yield return (MovementDirection.South, new DungeonPosition(position.Floor, position.X, position.Y + 1));
        yield return (MovementDirection.East, new DungeonPosition(position.Floor, position.X + 1, position.Y));
        yield return (MovementDirection.West, new DungeonPosition(position.Floor, position.X - 1, position.Y));
    }

    private static string RepositoryContentRoot() =>
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "content");
}
