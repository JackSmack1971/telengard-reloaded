using Telengard.Core.Combat;
using Telengard.Core.Knowledge;
using Telengard.Core.Presentation;
using Telengard.Core.Simulation;
using Telengard.Core.World.Features;
using Telengard.Core.World.Generation;
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
}
