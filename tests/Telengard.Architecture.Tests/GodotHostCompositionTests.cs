using System.Text.Json;
using Telengard.Content;
using Telengard.Core.Combat;
using Telengard.Core.Events;
using Telengard.Core.Items;
using Telengard.Core.Magic;
using Telengard.Core.Simulation;
using Telengard.Core.World.Generation;
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

    private static GodotSession CreateSession(GameState state)
        => CreateSession(state, new FloorLayoutCache(state.WorldSeed, state.Versions.GeneratorVersion));

    private static GodotSession CreateSession(GameState state, FloorLayoutCache layouts)
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
                new ThreatClassificationConfiguration(0, 2, ["cave-rat"])));
    }

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

    private static string RepositoryContentRoot() =>
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "content");
}
