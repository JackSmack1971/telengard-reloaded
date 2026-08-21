using Telengard.Core.Combat;
using Telengard.Core.Presentation;
using Telengard.Core.Simulation;
using Telengard.Core.World.Features;
using Telengard.Core.World.Generation;
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
    }
}
