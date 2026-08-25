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
    {
        var events = new List<IDomainEvent>();
        var dispatcher = new CommandDispatcher(state, new DomainEventBus());
        var pack = ContentPackLoader.Load(RepositoryContentRoot());
        return new GodotSession(
            dispatcher,
            new FloorLayoutGenerator().Generate(state.WorldSeed, state.Versions.GeneratorVersion, 1),
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
