using Telengard.Core.Combat;
using Telengard.Core.Knowledge;
using Telengard.Core.Presentation;
using Telengard.Core.Simulation;
using Telengard.Core.World.Features;
using Telengard.Core.World.Generation;
using Telengard.Save;
using Telengard.Terminal;
using Xunit;

namespace Telengard.Architecture.Tests;

public sealed class RendererSaveCompatibilityTests
{
    [Fact]
    public void Save_reload_preserves_modern_and_terminal_projections_without_exposing_hidden_state()
    {
        var layout = new FloorLayoutGenerator().Generate(1234, "generator-1", 1);
        var entered = DungeonWalkingResolver.Enter(
            GameState.Create(1234, mode: GameMode.Legacy),
            new EnterDungeonCommand(),
            layout);
        var position = entered.State.Player.Position;
        var observed = new DungeonPosition(1, 0, 0);
        var visited = new DungeonPosition(1, 0, 0);
        var discoveredFeatureId = Guid.Parse("00000000-0000-0000-0000-000000000010");
        var hiddenFeatureId = Guid.Parse("00000000-0000-0000-0000-000000000011");
        var monsterId = Guid.Parse("00000000-0000-0000-0000-000000000020");
        var state = entered.State with
        {
            SimulationTick = 42,
            Inn = new InnState { IsAtInn = false },
            Player = new PlayerState
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                Attributes = new(1, 2, 3, 4, 5, 6),
                Position = position,
                Level = 3,
                HitPoints = 8,
                MaxHitPoints = 10,
                SpellPower = 4,
                MaxSpellPower = 6,
                Inventory = ["potion"],
                CarriedGold = 17
            },
            Expedition = entered.State.Expedition with
            {
                DeepestFloorReached = 1,
                CarriedGold = 17,
                FloorsVisited = [1]
            },
            SecuredProgress = new SecuredProgressState { SecuredGold = 23 },
            Legacy = new LegacyState
            {
                PersistentMap = new PersistentMapState([observed, position], [visited])
            },
            Dungeon = new DungeonState
            {
                Features =
                [
                    new FeatureInstance(discoveredFeatureId, "fountain", observed, discovered: true),
                    new FeatureInstance(hiddenFeatureId, "secret-altar", position)
                ]
            },
            Knowledge = new KnowledgeState(
                [new KnowledgeEntry(
                    "feature:fountain",
                    observations: ["restores something"],
                    sampleCount: 1,
                    confidence: 50)]),
            Combat = new CombatState(
                new MonsterInstance(
                    monsterId,
                    "deep-watcher",
                    level: 4,
                    currentHitPoints: 7,
                    position,
                    temporaryEffects: ["hidden-effect"],
                    currentBehaviorState: "ambush"),
                CombatPhase.EnemyAction,
                round: 2,
                selectedAction: CombatAction.Defend,
                threatLevel: ThreatLevel.Deadly)
        };
        var events = entered.Events
            .Append<IDomainEvent>(new EncounterStartedEvent(state.Combat!.Monster))
            .ToArray();
        var originalSave = SaveGameSerializer.Serialize(state);

        var expectedPresentation = PresentationStateAdapter.Create(state);
        var expectedModern = ModernRenderer.Create(expectedPresentation, events);
        var expectedTerminal = TerminalRenderer.Render(expectedPresentation, events);
        var reloaded = SaveGameSerializer.Deserialize(originalSave);
        var actualPresentation = PresentationStateAdapter.Create(reloaded);
        var actualModern = ModernRenderer.Create(actualPresentation, events);
        var actualTerminal = TerminalRenderer.Render(actualPresentation, events);
        var originalContinuation = CombatStateResolver.Advance(state, new AdvanceCombatCommand());
        var reloadedContinuation = CombatStateResolver.Advance(reloaded, new AdvanceCombatCommand());

        Assert.Collection(
            events,
            domainEvent => Assert.IsType<DungeonEnteredEvent>(domainEvent),
            domainEvent => Assert.IsType<ExpeditionStartedEvent>(domainEvent),
            domainEvent => Assert.IsType<EncounterStartedEvent>(domainEvent));
        Assert.Equal(originalSave, SaveGameSerializer.Serialize(reloaded));
        Assert.Equal(
            SaveGameSerializer.Serialize(originalContinuation.State),
            SaveGameSerializer.Serialize(reloadedContinuation.State));
        Assert.Equal(originalContinuation.Events, reloadedContinuation.Events);
        Assert.Equal(expectedTerminal, actualTerminal);
        Assert.Equal(expectedModern.Scene, actualModern.Scene);
        Assert.Equal(expectedModern.PlayerPosition, actualModern.PlayerPosition);
        Assert.Equal(expectedModern.Environment, actualModern.Environment);
        Assert.Equal(expectedModern.Tiles, actualModern.Tiles);
        Assert.Equal(expectedModern.Features, actualModern.Features);
        Assert.Equal(expectedModern.Hud, actualModern.Hud);
        Assert.Equal(expectedModern.Combat, actualModern.Combat);
        Assert.Equal(expectedModern.Cues, actualModern.Cues);
        Assert.Equal(
            [ModernCueKind.DungeonEntered, ModernCueKind.CombatStarted],
            actualModern.Cues.Select(cue => cue.Kind));
        Assert.Contains("EVENT dungeon_entered", actualTerminal);
        Assert.Contains("EVENT encounter_started", actualTerminal);
        Assert.True(actualTerminal.IndexOf("EVENT dungeon_entered", StringComparison.Ordinal) <
            actualTerminal.IndexOf("EVENT encounter_started", StringComparison.Ordinal));
        Assert.DoesNotContain(actualPresentation.DiscoveredFeatures, feature => feature.InstanceId == hiddenFeatureId);
        Assert.DoesNotContain(hiddenFeatureId.ToString("N"), actualTerminal, StringComparison.Ordinal);
        Assert.DoesNotContain("hidden-effect", actualTerminal, StringComparison.Ordinal);
        Assert.DoesNotContain("ambush", actualTerminal, StringComparison.Ordinal);
        Assert.Equal(originalSave, SaveGameSerializer.Serialize(state));
    }
}
