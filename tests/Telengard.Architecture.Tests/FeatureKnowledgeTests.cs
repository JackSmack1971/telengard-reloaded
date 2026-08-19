using Telengard.Core.Knowledge;
using Telengard.Core.Simulation;
using Telengard.Save;
using Xunit;

namespace Telengard.Architecture.Tests;

public sealed class FeatureKnowledgeTests
{
    [Fact]
    public void Add_records_observed_feature_facts_under_a_namespaced_subject()
    {
        var result = FeatureKnowledgeResolver.Add(
            ActiveState(),
            new AddFeatureKnowledgeCommand("azure-fountain", ["cold_water", "restored_spell_power"]));

        var entry = Assert.Single(result.State.Knowledge.Entries);
        Assert.Equal("feature:azure-fountain", entry.SubjectId);
        Assert.Equal(["cold_water", "restored_spell_power"], entry.Observations);
        Assert.Equal(1, entry.SampleCount);
        Assert.Collection(
            result.Events,
            domainEvent =>
            {
                var added = Assert.IsType<KnowledgeObservationAddedEvent>(domainEvent);
                Assert.Equal("feature:azure-fountain", added.SubjectId);
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
            state = FeatureKnowledgeResolver.Add(
                state,
                new AddFeatureKnowledgeCommand("stone-altar", [$"sample_{sample}"])).State;
        }

        var entry = Assert.Single(state.Knowledge.Entries);
        Assert.Equal("feature:stone-altar", entry.SubjectId);
        Assert.Equal(["sample_1", "sample_2"], entry.Observations);
        Assert.Equal(2, entry.SampleCount);
        Assert.Equal(50, entry.Confidence);
        Assert.True(FeatureKnowledgeResolver.IsKnown(state.Knowledge, "stone-altar"));
        Assert.False(FeatureKnowledgeResolver.IsKnown(state.Knowledge, "azure-fountain"));
    }

    [Fact]
    public void Add_validates_before_mutating_and_replays_and_round_trips_deterministically()
    {
        var command = new AddFeatureKnowledgeCommand("azure-fountain", ["observed"]);
        Assert.Throws<InvalidOperationException>(() => FeatureKnowledgeResolver.Add(
            ActiveState() with { Expedition = new ExpeditionState() },
            command));
        Assert.Throws<InvalidOperationException>(() => FeatureKnowledgeResolver.Add(
            ActiveState() with { Player = new PlayerState { Alive = false } },
            command));
        Assert.Throws<ArgumentException>(() => new AddFeatureKnowledgeCommand("azure-fountain", []));
        Assert.Throws<ArgumentException>(() => new AddFeatureKnowledgeCommand("azure-fountain", ["observed", "observed"]));
        Assert.Throws<ArgumentException>(() => new AddFeatureKnowledgeCommand(" ", ["observed"]));

        var first = FeatureKnowledgeResolver.Add(ActiveState(), command);
        var second = FeatureKnowledgeResolver.Add(ActiveState(), command);
        Assert.Equal(SaveGameSerializer.Serialize(first.State), SaveGameSerializer.Serialize(second.State));
        Assert.Equal(first.Events.Count, second.Events.Count);
        for (var index = 0; index < first.Events.Count; index++)
        {
            Assert.Equal(first.Events[index].GetType(), second.Events[index].GetType());
        }

        var restored = SaveGameSerializer.Deserialize(SaveGameSerializer.Serialize(first.State));
        Assert.Equal(SaveGameSerializer.Serialize(first.State), SaveGameSerializer.Serialize(restored));
        Assert.False(FeatureKnowledgeResolver.IsKnown(restored.Knowledge, "stone-altar"));
    }

    private static GameState ActiveState() => GameState.Create(1234) with
    {
        Expedition = new ExpeditionState
        {
            Active = true,
            FloorsVisited = [1],
            ExpeditionId = Guid.Parse("00000000-0000-0000-0000-000000000055")
        },
        Inn = new InnState { IsAtInn = false }
    };
}
