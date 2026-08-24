using Telengard.Core.Combat;
using Telengard.Core.Economy;
using Telengard.Core.Items;
using Telengard.Core.Knowledge;
using Telengard.Core.Magic;
using Telengard.Core.Presentation;
using Telengard.Core.Progression;
using Telengard.Core.Simulation;
using Telengard.Core.World.Features;
using Telengard.Core.World.Generation;
using Telengard.Core.Meta;
using Telengard.Terminal;
using Xunit;

namespace Telengard.Architecture.Tests;

public sealed class TerminalRendererTests
{
    [Fact]
    public void Render_projects_stable_dungeon_state_and_orders_map_features_and_knowledge()
    {
        var current = new DungeonPosition(1, 2, 2);
        var feature = new FeatureInstance(
            Guid.Parse("00000000-0000-0000-0000-000000000010"),
            "fountain",
            new DungeonPosition(1, 1, 1),
            discovered: true);
        var state = GameState.Create(1234) with
        {
            Inn = new InnState { IsAtInn = false },
            Player = new PlayerState { Id = Guid.Parse("00000000-0000-0000-0000-000000000001"), Position = current, Level = 3 },
            Expedition = new ExpeditionState { Active = true, StartingFloor = 1, DeepestFloorReached = 2, CarriedGold = 7 },
            SecuredProgress = new SecuredProgressState { SecuredGold = 11 },
            Dungeon = new DungeonState { Features = [feature] },
            Legacy = new LegacyState
            {
                PersistentMap = new PersistentMapState(
                    [current, new DungeonPosition(1, 1, 1)],
                    [new DungeonPosition(1, 0, 0)])
            },
            Knowledge = new KnowledgeState([
                new KnowledgeEntry("zeta", sampleCount: 2, confidence: 50),
                new KnowledgeEntry("alpha", sampleCount: 1, confidence: 25)
            ])
        };

        var presentation = PresentationStateAdapter.Create(state);
        var output = TerminalRenderer.Render(presentation);

        Assert.Contains("SCENE DUNGEON", output);
        Assert.Contains("MAP v 1/0/0", output);
        Assert.Contains("MAP . 1/1/1", output);
        Assert.Contains("MAP @ 1/2/2", output);
        Assert.Contains("FEATURE id=00000000000000000000000000000010 definition=fountain", output);
        Assert.Contains("EXPEDITION active=True id=NONE floor=1..2 carried_gold=7 items=0", output);
        Assert.True(output.IndexOf("KNOWLEDGE subject=alpha", StringComparison.Ordinal) <
            output.IndexOf("KNOWLEDGE subject=zeta", StringComparison.Ordinal));
        Assert.Equal(output, TerminalRenderer.Render(presentation));
    }

    [Fact]
    public void Render_emits_safe_event_cues_without_hidden_encounter_details()
    {
        var monster = new MonsterInstance(
            Guid.Parse("00000000-0000-0000-0000-000000000020"),
            "deep_watcher",
            level: 99,
            currentHitPoints: 17,
            new DungeonPosition(2, 4, 5),
            temporaryEffects: ["hidden_effect"],
            currentBehaviorState: "ambush");
        var events = new IDomainEvent[]
        {
            new EncounterStartedEvent(monster),
            new PlayerMovedEvent(new DungeonPosition(1, 0, 0), new DungeonPosition(1, 1, 0)),
            new MonsterDamagedEvent(monster.InstanceId, Amount: 4, RemainingHitPoints: 13)
        };

        var output = TerminalRenderer.Render(
            PresentationStateAdapter.Create(GameState.Create(1234)),
            events);

        Assert.Contains("EVENT encounter_started", output);
        Assert.Contains("EVENT player_moved 1/1/0", output);
        Assert.Contains("EVENT monster_damaged amount=4", output);
        Assert.True(output.IndexOf("EVENT encounter_started", StringComparison.Ordinal) <
            output.IndexOf("EVENT player_moved", StringComparison.Ordinal));
        Assert.True(output.IndexOf("EVENT player_moved", StringComparison.Ordinal) <
            output.IndexOf("EVENT monster_damaged", StringComparison.Ordinal));
        Assert.DoesNotContain("deep_watcher", output);
        Assert.DoesNotContain("hidden_effect", output);
        Assert.DoesNotContain("99", output);
    }

    [Fact]
    public void Render_projects_inn_state_and_rejects_null_inputs()
    {
        var state = GameState.Create(1234);

        var output = TerminalRenderer.Render(PresentationStateAdapter.Create(state));

        Assert.StartsWith("SCENE INN\n", output, StringComparison.Ordinal);
        Assert.Throws<ArgumentNullException>(() => TerminalRenderer.Render(null!));
        Assert.Throws<ArgumentException>(() => TerminalRenderer.Render(
            PresentationStateAdapter.Create(state),
            new IDomainEvent[] { null! }));
    }

    [Fact]
    public void Render_projects_combat_state_and_every_supported_event_cue()
    {
        var position = new DungeonPosition(2, 3, 4);
        var otherPosition = new DungeonPosition(3, 5, 6);
        var entityId = Guid.Parse("00000000-0000-0000-0000-000000000030");
        var monster = new MonsterInstance(entityId, "monster", 2, 10, position);
        var state = GameState.Create(1234) with
        {
            Inn = new InnState { IsAtInn = false },
            Player = new PlayerState { Position = position },
            Expedition = new ExpeditionState { Active = true, ExpeditionId = entityId },
            Combat = new CombatState(monster, CombatPhase.EnemyAction, round: 3, threatLevel: ThreatLevel.Dangerous)
        };
        var events = new IDomainEvent[]
        {
            new DungeonEnteredEvent(position),
            new PlayerMovedEvent(position, otherPosition),
            new FloorChangedEvent(position, otherPosition, StairDirection.Down),
            new DungeonLeftEvent(otherPosition),
            new FeatureDiscoveredEvent(entityId, position),
            new FeatureActivatedEvent(entityId, position, 2),
            new EncounterStartedEvent(monster),
            new EncounterEndedEvent(entityId),
            new CombatPhaseChangedEvent(entityId, CombatPhase.PlayerAction, CombatPhase.EnemyAction, 3),
            new MonsterDamagedEvent(entityId, 3, 7),
            new MonsterKilledEvent(entityId),
            new SpellCastEvent(entityId, "spark", 2, 3),
            new ItemIdentifiedEvent(entityId),
            new ItemEquippedEvent("weapon", entityId),
            new ItemUnequippedEvent("weapon", entityId),
            new GoldAcquiredEvent(4, 4),
            new GoldSecuredEvent(4, 4),
            new PlayerLeveledUpEvent(1, 2, 10),
            new ExperienceAwardedEvent(5, 10),
            new PlayerDiedEvent(null, position),
            new ExpeditionSucceededEvent(null, 3),
            new ExpeditionFailedEvent(null, 3),
            new KnowledgeObservationAddedEvent("monster", ["observed"]),
            new KnowledgeSampleCountedEvent("monster", 2),
            new KnowledgeConfidenceUpdatedEvent("monster", 2, 50),
            new GameSuspendedEvent(null, position)
        };

        var output = TerminalRenderer.Render(PresentationStateAdapter.Create(state), events);

        Assert.Contains("COMBAT id=", output);
        Assert.Contains("phase=EnemyAction", output);
        Assert.Contains("threat=Dangerous", output);
        var unknownThreat = TerminalRenderer.Render(
            PresentationStateAdapter.Create(state with
            {
                Combat = state.Combat! with { ThreatLevel = null }
            }));
        Assert.Contains("threat=UNKNOWN", unknownThreat);
        Assert.Contains("EVENT dungeon_entered 2/3/4", output);
        Assert.Contains("EVENT player_moved 3/5/6", output);
        Assert.Contains("EVENT floor_changed 3/5/6", output);
        Assert.Contains("EVENT dungeon_left 3/5/6", output);
        Assert.Contains("EVENT feature_discovered 2/3/4", output);
        Assert.Contains("EVENT feature_activated 2/3/4", output);
        Assert.Contains("EVENT encounter_started", output);
        Assert.Contains("EVENT encounter_ended", output);
        Assert.Contains("EVENT combat_phase_changed EnemyAction", output);
        Assert.Contains("EVENT monster_damaged amount=3", output);
        Assert.Contains("EVENT monster_killed", output);
        Assert.Contains("EVENT spell_cast", output);
        Assert.Contains("EVENT item_identified", output);
        Assert.Contains("EVENT item_equipped", output);
        Assert.Contains("EVENT item_unequipped", output);
        Assert.Contains("EVENT gold_acquired amount=4", output);
        Assert.Contains("EVENT gold_secured amount=4", output);
        Assert.Contains("EVENT player_leveled_up level=2", output);
        Assert.Contains("EVENT experience_awarded amount=5", output);
        Assert.Contains("EVENT player_died 2/3/4", output);
        Assert.Contains("EVENT expedition_succeeded", output);
        Assert.Contains("EVENT expedition_failed", output);
        Assert.Equal(25, output.Split('\n').Count(line => line.StartsWith("EVENT ", StringComparison.Ordinal)));
    }
}
