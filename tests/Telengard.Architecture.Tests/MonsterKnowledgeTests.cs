using Telengard.Core.Combat;
using Telengard.Core.Knowledge;
using Telengard.Core.Simulation;
using Telengard.Save;
using Xunit;

namespace Telengard.Architecture.Tests;

public sealed class MonsterKnowledgeTests
{
    [Fact]
    public void Add_records_observed_monster_facts_under_a_namespaced_subject()
    {
        var result = MonsterKnowledgeResolver.Add(
            ActiveState(),
            new AddMonsterKnowledgeCommand("rat", ["saw_claws", "heard_hiss"]));

        var entry = Assert.Single(result.State.Knowledge.Entries);
        Assert.Equal("monster:rat", entry.SubjectId);
        Assert.Equal(["saw_claws", "heard_hiss"], entry.Observations);
        Assert.Equal(1, entry.SampleCount);
        Assert.Collection(
            result.Events,
            domainEvent =>
            {
                var added = Assert.IsType<KnowledgeObservationAddedEvent>(domainEvent);
                Assert.Equal("monster:rat", added.SubjectId);
                Assert.Equal(entry.Observations, added.Observations);
            },
            domainEvent => Assert.IsType<KnowledgeSampleCountedEvent>(domainEvent),
            domainEvent => Assert.IsType<KnowledgeConfidenceUpdatedEvent>(domainEvent));
    }

    [Fact]
    public void Add_reuses_the_observation_pipeline_for_samples_and_confidence()
    {
        var state = ActiveState();
        for (var sample = 1; sample <= 2; sample++)
        {
            state = MonsterKnowledgeResolver.Add(
                state,
                new AddMonsterKnowledgeCommand("rat", [$"sample_{sample}"])).State;
        }

        var entry = Assert.Single(state.Knowledge.Entries);
        Assert.Equal("monster:rat", entry.SubjectId);
        Assert.Equal(["sample_1", "sample_2"], entry.Observations);
        Assert.Equal(2, entry.SampleCount);
        Assert.Equal(50, entry.Confidence);
        Assert.True(MonsterKnowledgeResolver.IsKnown(state.Knowledge, "rat"));
    }

    [Fact]
    public void Add_validates_before_mutating_and_replays_and_round_trips_deterministically()
    {
        var command = new AddMonsterKnowledgeCommand("rat", ["observed"]);
        Assert.Throws<InvalidOperationException>(() => MonsterKnowledgeResolver.Add(
            ActiveState() with { Expedition = new ExpeditionState() },
            command));
        Assert.Throws<InvalidOperationException>(() => MonsterKnowledgeResolver.Add(
            ActiveState() with { Player = new PlayerState { Alive = false } },
            command));
        Assert.Throws<ArgumentException>(() => new AddMonsterKnowledgeCommand("rat", []));
        Assert.Throws<ArgumentException>(() => new AddMonsterKnowledgeCommand("rat", ["observed", "observed"]));
        Assert.Throws<ArgumentException>(() => new AddMonsterKnowledgeCommand(" ", ["observed"]));

        var first = MonsterKnowledgeResolver.Add(ActiveState(), command);
        var second = MonsterKnowledgeResolver.Add(ActiveState(), command);
        Assert.Equal(SaveGameSerializer.Serialize(first.State), SaveGameSerializer.Serialize(second.State));
        Assert.Equal(first.Events.Count, second.Events.Count);
        for (var index = 0; index < first.Events.Count; index++)
        {
            Assert.Equal(first.Events[index].GetType(), second.Events[index].GetType());
        }

        var restored = SaveGameSerializer.Deserialize(SaveGameSerializer.Serialize(first.State));
        Assert.Equal(SaveGameSerializer.Serialize(first.State), SaveGameSerializer.Serialize(restored));
        Assert.False(MonsterKnowledgeResolver.IsKnown(restored.Knowledge, "dragon"));
    }

    [Fact]
    public void Threat_assessment_uses_persistent_monster_knowledge_without_revealing_exact_stats()
    {
        var state = MonsterKnowledgeResolver.Add(
            ActiveState(),
            new AddMonsterKnowledgeCommand("rat", ["observed"])).State with
        {
            Combat = CombatStateResolver.Begin(Monster("rat", 1))
        };
        state = CombatStateResolver.Advance(state, new AdvanceCombatCommand()).State;

        var result = ThreatAssessmentResolver.Resolve(
            state,
            new AssessThreatCommand(),
            new ThreatClassificationConfiguration(0, 2));

        Assert.Equal(ThreatLevel.Trivial, result.State.Combat!.ThreatLevel);
        var assessed = Assert.IsType<ThreatAssessedEvent>(Assert.Single(result.Events));
        Assert.Equal(ThreatLevel.Trivial, assessed.Level);
    }

    private static GameState ActiveState() => GameState.Create(1234) with
    {
        Expedition = new ExpeditionState
        {
            Active = true,
            ExpeditionId = Guid.Parse("00000000-0000-0000-0000-000000000054")
        }
    };

    private static MonsterInstance Monster(string definitionId, int level) => new(
        Guid.Parse("00000000-0000-0000-0000-000000000054"),
        definitionId,
        level,
        3,
        new DungeonPosition(1, 0, 0));
}
