using Telengard.Core.Combat;
using Telengard.Core.Knowledge;
using Telengard.Core.Presentation;
using Telengard.Core.Simulation;
using Telengard.Core.World.Features;
using Xunit;

namespace Telengard.Architecture.Tests;

public sealed class PresentationStateAdapterTests
{
    [Fact]
    public void Create_projects_authoritative_state_without_changing_it()
    {
        var featureId = Guid.Parse("00000000-0000-0000-0000-000000000010");
        var state = CreateState(featureId);

        var presentation = PresentationStateAdapter.Create(state);

        Assert.Equal(state.CurrentMode, presentation.Mode);
        Assert.Equal(state.Versions, presentation.Versions);
        Assert.Equal(state.SimulationTick, presentation.SimulationTick);
        Assert.Equal(state.Inn.IsAtInn, presentation.IsAtInn);
        Assert.Equal(state.SecuredProgress.SecuredGold, presentation.SecuredGold);
        Assert.Equal(state.Player.Position, presentation.Player.Position);
        Assert.Equal(state.Player.Attributes, presentation.Player.Attributes);
        Assert.Equal(state.Player.Experience, presentation.Player.Experience);
        Assert.Equal(state.Player.EquipmentSlots, presentation.Player.EquipmentSlots);
        Assert.Equal(state.Player.Talents, presentation.Player.Talents);
        Assert.Equal(state.Player.Spells, presentation.Player.Spells);
        Assert.Equal(state.Player.Injuries, presentation.Player.Injuries);
        Assert.Equal(state.Player.TemporaryEffects, presentation.Player.TemporaryEffects);
        Assert.Equal(state.Player.Inventory, presentation.Player.Inventory);
        Assert.Equal(state.Expedition.FloorsVisited, presentation.Expedition.FloorsVisited);
        Assert.Equal(state.Expedition.StartSimulationTick, presentation.Expedition.StartSimulationTick);
        Assert.Equal(state.Expedition.SimulationTicks, presentation.Expedition.SimulationTicks);
        Assert.Equal(state.Expedition.MonstersDefeated, presentation.Expedition.MonstersDefeated);
        Assert.Equal(state.Expedition.DiscoveriesMade, presentation.Expedition.DiscoveriesMade);
        Assert.Equal(state.Expedition.RoomsVisited, presentation.Expedition.RoomsVisited);
        Assert.Equal(state.Expedition.Objectives, presentation.Expedition.Objectives);
        Assert.Equal(state.Legacy.PersistentMap.ObservedPositions, presentation.ObservedMap);
        Assert.Equal(state.Knowledge.Entries, presentation.Knowledge);
        Assert.DoesNotContain(state.Dungeon.Features, feature =>
            feature.InstanceId != featureId && feature.Discovered);
    }

    [Fact]
    public void Create_exposes_only_discovered_features()
    {
        var discoveredId = Guid.Parse("00000000-0000-0000-0000-000000000010");
        var hiddenId = Guid.Parse("00000000-0000-0000-0000-000000000011");
        var state = GameState.Create(1234) with
        {
            Dungeon = new DungeonState
            {
                Features =
                [
                    new FeatureInstance(discoveredId, "fountain", new DungeonPosition(1, 2, 3), discovered: true),
                    new FeatureInstance(hiddenId, "altar", new DungeonPosition(1, 3, 3))
                ]
            }
        };

        var presentation = PresentationStateAdapter.Create(state);

        var feature = Assert.Single(presentation.DiscoveredFeatures);
        Assert.Equal(discoveredId, feature.InstanceId);
        Assert.Equal("fountain", feature.DefinitionId);
        Assert.DoesNotContain(presentation.DiscoveredFeatures, candidate => candidate.InstanceId == hiddenId);
    }

    [Fact]
    public void Create_preserves_observed_combat_state_but_redacts_internal_monster_details()
    {
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
            Combat = new CombatState(
                monster,
                CombatPhase.PlayerAction,
                round: 3,
                threatLevel: ThreatLevel.Deadly)
        };

        var combat = Assert.IsType<PresentationCombatState>(PresentationStateAdapter.Create(state).Combat);

        Assert.Equal(monster.InstanceId, combat.EncounterId);
        Assert.Equal(CombatPhase.PlayerAction, combat.Phase);
        Assert.Equal(3, combat.Round);
        Assert.Equal(ThreatLevel.Deadly, combat.ThreatLevel);
        Assert.Equal(combat.SelectedAction, state.Combat!.SelectedAction);
        Assert.Equal(monster.DefinitionId, combat.Monster.DefinitionId);
        Assert.Equal(monster.CurrentHitPoints, combat.Monster.CurrentHitPoints);
        Assert.DoesNotContain(typeof(PresentationMonsterState).GetProperties(), property =>
            property.Name is nameof(MonsterInstance.Level) or
                nameof(MonsterInstance.TemporaryEffects) or
                nameof(MonsterInstance.CurrentBehaviorState));
    }

    [Fact]
    public void Create_returns_read_only_projection_collections()
    {
        var featureId = Guid.Parse("00000000-0000-0000-0000-000000000010");
        var state = CreateState(featureId);
        var presentation = PresentationStateAdapter.Create(state);

        Assert.Throws<NotSupportedException>(() =>
            ((IList<PresentationFeatureState>)presentation.DiscoveredFeatures)[0] = null!);
        Assert.Throws<NotSupportedException>(() =>
            ((IList<DungeonPosition>)presentation.ObservedMap).Clear());
        Assert.Throws<ArgumentNullException>(() => PresentationStateAdapter.Create(null!));
        var player = new PresentationPlayerState(state.Player);
        var expedition = new PresentationExpeditionState(state.Expedition);
        Assert.Throws<ArgumentNullException>(() => new PresentationState(
            GameMode.Classic,
            null!,
            0,
            true,
            0,
            player,
            expedition,
            [],
            [],
            [],
            [],
            null));
        Assert.Throws<ArgumentNullException>(() => new PresentationState(
            GameMode.Classic,
            state.Versions,
            0,
            true,
            0,
            null!,
            expedition,
            [],
            [],
            [],
            [],
            null));
        Assert.Throws<ArgumentNullException>(() => new PresentationState(
            GameMode.Classic,
            state.Versions,
            0,
            true,
            0,
            player,
            null!,
            [],
            [],
            [],
            [],
            null));
        Assert.Throws<ArgumentException>(() => new PresentationState(
            GameMode.Classic,
            state.Versions,
            0,
            true,
            0,
            player,
            expedition,
            new DungeonPosition[] { null! },
            [],
            [],
            [],
            null));
        Assert.Throws<ArgumentException>(() => new PresentationFeatureState(
            new FeatureInstance(Guid.NewGuid(), "hidden", new DungeonPosition(1, 0, 0))));
    }

    private static GameState CreateState(Guid featureId)
    {
        var position = new DungeonPosition(2, 4, 5);
        return GameState.Create(1234, mode: GameMode.Legacy) with
        {
            SimulationTick = 7,
            Inn = new InnState { IsAtInn = false },
            Player = new PlayerState
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                Position = position,
                Inventory = ["potion"],
                CarriedGold = 12
            },
            Expedition = new ExpeditionState
            {
                ExpeditionId = Guid.Parse("00000000-0000-0000-0000-000000000002"),
                FloorsVisited = [1, 2],
                Active = true
            },
            Legacy = new LegacyState
            {
                PersistentMap = new PersistentMapState([position], [position])
            },
            Dungeon = new DungeonState
            {
                Features = [new FeatureInstance(featureId, "fountain", position, discovered: true)]
            },
            Knowledge = new KnowledgeState(
                [new KnowledgeEntry(
                    "feature:fountain",
                    observations: ["restores something"],
                    sampleCount: 1,
                    confidence: 50)])
        };
    }
}
