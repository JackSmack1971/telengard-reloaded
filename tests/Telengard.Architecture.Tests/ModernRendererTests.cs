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
using Xunit;

namespace Telengard.Architecture.Tests;

public sealed class ModernRendererTests
{
    [Fact]
    public void Create_projects_only_known_tiles_and_marks_the_current_position()
    {
        var observed = new DungeonPosition(1, 1, 1);
        var visited = new DungeonPosition(1, 0, 0);
        var current = new DungeonPosition(1, 2, 2);
        var state = GameState.Create(1234) with
        {
            Inn = new InnState { IsAtInn = false },
            Player = new PlayerState { Position = current },
            Expedition = new ExpeditionState { Active = true },
            Legacy = new LegacyState
            {
                PersistentMap = new PersistentMapState([observed, current], [visited])
            }
        };

        var frame = ModernRenderer.Create(PresentationStateAdapter.Create(state));

        Assert.Equal(ModernScene.Dungeon, frame.Scene);
        Assert.Equal(current, frame.PlayerPosition);
        Assert.True(frame.Environment.DynamicLighting);
        Assert.True(frame.Environment.AtmosphericEffects);
        Assert.Equal("dungeon", frame.Environment.ThemeId);
        Assert.Equal(
            [
                new ModernTileMarker(visited, ModernTileKnowledge.Visited),
                new ModernTileMarker(observed, ModernTileKnowledge.Observed),
                new ModernTileMarker(current, ModernTileKnowledge.Current)
            ],
            frame.Tiles);
    }

    [Fact]
    public void Create_uses_the_redacted_presentation_state_for_features_and_combat()
    {
        var discoveredId = Guid.Parse("00000000-0000-0000-0000-000000000010");
        var hiddenId = Guid.Parse("00000000-0000-0000-0000-000000000011");
        var monster = new MonsterInstance(
            Guid.Parse("00000000-0000-0000-0000-000000000020"),
            "deep_watcher",
            level: 99,
            currentHitPoints: 17,
            new DungeonPosition(2, 4, 5),
            temporaryEffects: ["hidden_effect"],
            currentBehaviorState: "ambush");
        var state = GameState.Create(1234) with
        {
            Dungeon = new DungeonState
            {
                Features =
                [
                    new FeatureInstance(discoveredId, "fountain", new DungeonPosition(1, 2, 3), discovered: true),
                    new FeatureInstance(hiddenId, "altar", new DungeonPosition(1, 3, 3))
                ]
            },
            Combat = new CombatState(monster, CombatPhase.PlayerAction, round: 3, threatLevel: ThreatLevel.Deadly)
        };

        var frame = ModernRenderer.Create(PresentationStateAdapter.Create(state));

        var feature = Assert.Single(frame.Features);
        Assert.Equal(discoveredId, feature.InstanceId);
        Assert.DoesNotContain(frame.Features, candidate => candidate.InstanceId == hiddenId);
        Assert.Equal(monster.InstanceId, frame.Combat!.Monster.InstanceId);
        Assert.DoesNotContain(typeof(ModernMonsterMarker).GetProperties(), property =>
            property.Name is nameof(MonsterInstance.Level) or
                nameof(MonsterInstance.TemporaryEffects) or
                nameof(MonsterInstance.CurrentBehaviorState));
    }

    [Fact]
    public void Create_translates_committed_events_into_safe_visual_cues_in_order()
    {
        var from = new DungeonPosition(1, 0, 0);
        var to = new DungeonPosition(1, 1, 0);
        var monsterId = Guid.Parse("00000000-0000-0000-0000-000000000020");
        var state = GameState.Create(1234) with
        {
            Player = new PlayerState { Position = to }
        };
        var events = new IDomainEvent[]
        {
            new PlayerMovedEvent(from, to),
            new MonsterDamagedEvent(monsterId, Amount: 4, RemainingHitPoints: 6),
            new EncounterEndedEvent(monsterId)
        };

        var frame = ModernRenderer.Create(PresentationStateAdapter.Create(state), events);

        Assert.Equal(
            [ModernCueKind.PlayerMoved, ModernCueKind.MonsterDamaged, ModernCueKind.CombatEnded],
            frame.Cues.Select(cue => cue.Kind));
        Assert.Equal(to, frame.Cues[0].Position);
        Assert.Equal(monsterId, frame.Cues[1].EntityId);
        Assert.Equal(4, frame.Cues[1].Value);
        Assert.Equal(monsterId, frame.Cues[2].EntityId);
    }

    [Fact]
    public void Create_returns_read_only_frame_collections_and_rejects_null_inputs()
    {
        var state = GameState.Create(1234);
        var frame = ModernRenderer.Create(PresentationStateAdapter.Create(state));

        Assert.Throws<NotSupportedException>(() =>
            ((IList<ModernTileMarker>)frame.Tiles).Clear());
        Assert.Throws<ArgumentNullException>(() => ModernRenderer.Create(null!));
        Assert.Throws<ArgumentException>(() => ModernRenderer.Create(
            PresentationStateAdapter.Create(state),
            new IDomainEvent[] { null! }));
        var environment = new ModernEnvironment(false, false);
        var hud = new ModernHud(Guid.Empty, 1, 1, 1, 1, 1, 0, 0, true);
        Assert.Throws<ArgumentNullException>(() => new ModernRenderFrame(
            ModernScene.Inn,
            null!,
            environment,
            [],
            [],
            hud,
            null,
            []));
        Assert.Throws<ArgumentException>(() => new ModernRenderFrame(
            ModernScene.Inn,
            new DungeonPosition(1, 0, 0),
            environment,
            new ModernTileMarker[] { null! },
            [],
            hud,
            null,
            []));
    }

    [Fact]
    public void Create_projects_every_supported_event_into_a_safe_cue_and_ignores_unknown_events()
    {
        var position = new DungeonPosition(2, 3, 4);
        var otherPosition = new DungeonPosition(3, 5, 6);
        var entityId = Guid.Parse("00000000-0000-0000-0000-000000000030");
        var monster = new MonsterInstance(
            entityId,
            "monster",
            level: 2,
            currentHitPoints: 10,
            position);
        var events = new IDomainEvent[]
        {
            new DungeonEnteredEvent(position),
            new PlayerMovedEvent(position, otherPosition),
            new FloorChangedEvent(position, otherPosition, StairDirection.Down),
            new DungeonLeftEvent(otherPosition),
            new FeatureDiscoveredEvent(entityId, position),
            new FeatureActivatedEvent(entityId, position, ActivationCount: 2),
            new EncounterStartedEvent(monster),
            new EncounterEndedEvent(entityId),
            new CombatPhaseChangedEvent(entityId, CombatPhase.PlayerAction, CombatPhase.EnemyAction, Round: 3),
            new MonsterDamagedEvent(entityId, Amount: 3, RemainingHitPoints: 7),
            new MonsterKilledEvent(entityId),
            new SpellCastEvent(entityId, "spark", Cost: 2, RemainingSpellPower: 3),
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

        var frame = ModernRenderer.Create(
            PresentationStateAdapter.Create(GameState.Create(1234) with
            {
                Player = new PlayerState { Position = position }
            }),
            events);

        Assert.Equal(25, frame.Cues.Count);
        Assert.Equal(
            [
                ModernCueKind.DungeonEntered,
                ModernCueKind.PlayerMoved,
                ModernCueKind.FloorChanged,
                ModernCueKind.DungeonLeft,
                ModernCueKind.FeatureDiscovered,
                ModernCueKind.FeatureActivated,
                ModernCueKind.CombatStarted,
                ModernCueKind.CombatEnded,
                ModernCueKind.CombatPhaseChanged,
                ModernCueKind.MonsterDamaged,
                ModernCueKind.MonsterKilled,
                ModernCueKind.SpellCast,
                ModernCueKind.ItemIdentified,
                ModernCueKind.ItemEquipped,
                ModernCueKind.ItemUnequipped,
                ModernCueKind.GoldAcquired,
                ModernCueKind.GoldSecured,
                ModernCueKind.PlayerLeveledUp,
                ModernCueKind.ExperienceAwarded,
                ModernCueKind.PlayerDied,
                ModernCueKind.ExpeditionSucceeded,
                ModernCueKind.ExpeditionFailed,
                ModernCueKind.KnowledgeUpdated,
                ModernCueKind.KnowledgeUpdated,
                ModernCueKind.KnowledgeUpdated
            ],
            frame.Cues.Select(cue => cue.Kind));
        Assert.Equal(otherPosition, frame.Cues[1].Position);
        Assert.Equal(entityId, frame.Cues[5].EntityId);
        Assert.Equal(2, frame.Cues[5].Value);
        Assert.Equal(CombatPhase.EnemyAction, (CombatPhase)frame.Cues[8].Value!);
        Assert.Equal(3, frame.Cues[9].Value);
        Assert.Equal(4, frame.Cues[15].Value);
        Assert.Equal(2, frame.Cues[17].Value);
    }

    [Fact]
    public void Create_exposes_all_projected_frame_properties_and_combat_details()
    {
        var position = new DungeonPosition(1, 2, 3);
        var featureId = Guid.Parse("00000000-0000-0000-0000-000000000040");
        var monsterId = Guid.Parse("00000000-0000-0000-0000-000000000041");
        var state = GameState.Create(1234) with
        {
            Inn = new InnState { IsAtInn = false },
            Player = new PlayerState
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000042"),
                Position = position,
                Level = 4,
                HitPoints = 8,
                MaxHitPoints = 10,
                SpellPower = 3,
                MaxSpellPower = 5,
                CarriedGold = 6,
                Alive = true
            },
            SecuredProgress = new SecuredProgressState { SecuredGold = 7 },
            Expedition = new ExpeditionState { Active = true },
            Dungeon = new DungeonState
            {
                Features = [new FeatureInstance(featureId, "fountain", position, discovered: true, activationCount: 2)]
            },
            Combat = new CombatState(
                new MonsterInstance(monsterId, "rat", 1, 9, position),
                CombatPhase.EnemyAction,
                round: 3,
                threatLevel: ThreatLevel.Dangerous)
        };

        var frame = ModernRenderer.Create(PresentationStateAdapter.Create(state));

        Assert.Equal(ModernScene.Dungeon, frame.Scene);
        Assert.Equal(position, frame.PlayerPosition);
        Assert.True(frame.Environment.DynamicLighting);
        Assert.True(frame.Environment.AtmosphericEffects);
        var marker = Assert.Single(frame.Features);
        Assert.Equal(featureId, marker.InstanceId);
        Assert.Equal("fountain", marker.DefinitionId);
        Assert.Equal("fountain", marker.PresentationKey);
        Assert.Equal(position, marker.Position);
        Assert.Equal(2, marker.ActivationCount);
        Assert.Equal(state.Player.Id, frame.Hud.PlayerId);
        Assert.Equal(4, frame.Hud.Level);
        Assert.Equal(8, frame.Hud.HitPoints);
        Assert.Equal(10, frame.Hud.MaxHitPoints);
        Assert.Equal(3, frame.Hud.SpellPower);
        Assert.Equal(5, frame.Hud.MaxSpellPower);
        Assert.Equal(6, frame.Hud.CarriedGold);
        Assert.Equal(7, frame.Hud.SecuredGold);
        Assert.True(frame.Hud.Alive);
        Assert.Equal(state.Combat.EncounterId, frame.Combat!.EncounterId);
        Assert.Equal(CombatPhase.EnemyAction, frame.Combat.Phase);
        Assert.Equal(3, frame.Combat.Round);
        Assert.Equal(ThreatLevel.Dangerous, frame.Combat.ThreatLevel);
        Assert.Equal(monsterId, frame.Combat.Monster.InstanceId);
        Assert.Equal("rat", frame.Combat.Monster.DefinitionId);
        Assert.Equal("rat", frame.Combat.Monster.PresentationKey);
        Assert.Equal(9, frame.Combat.Monster.CurrentHitPoints);
        Assert.Equal(position, frame.Combat.Monster.Position);
    }

    [Fact]
    public void Create_projects_known_inventory_spells_and_journal_subjects()
    {
        var state = GameState.Create(1234) with
        {
            Player = new PlayerState
            {
                Inventory = ["tarnished_sword"],
                Spells = ["ember_bolt"]
            },
            Knowledge = new KnowledgeState(
                [new KnowledgeEntry("crypt_stalker", ["observed"], sampleCount: 1)])
        };

        var frame = ModernRenderer.Create(PresentationStateAdapter.Create(state));

        Assert.Equal(["tarnished_sword"], frame.Inventory);
        Assert.Equal(["ember_bolt"], frame.Spells);
        Assert.Equal(["crypt_stalker"], frame.Journal);
    }
}
