using Telengard.Core.Knowledge;
using Telengard.Core.Simulation;
using Telengard.Save;
using Xunit;

namespace Telengard.Architecture.Tests;

public sealed class KnowledgeConfidenceTests
{
    [Fact]
    public void Add_progresses_confidence_at_configured_sample_thresholds()
    {
        var state = ActiveState();
        var expected = new[] { 25, 50, 50, 75, 75, 75, 100 };

        for (var index = 0; index < expected.Length; index++)
        {
            var result = KnowledgeObservationResolver.Add(
                state,
                new AddKnowledgeObservationCommand("fountain", [$"sample_{index}"]));

            var entry = Assert.Single(result.State.Knowledge.Entries);
            Assert.Equal(index + 1, entry.SampleCount);
            Assert.Equal(expected[index], entry.Confidence);
            var confidenceEvent = result.Events
                .OfType<KnowledgeConfidenceUpdatedEvent>()
                .SingleOrDefault();
            if (index is 0 or 1 or 3 or 6)
            {
                Assert.NotNull(confidenceEvent);
                Assert.Equal(entry.SampleCount, confidenceEvent.SampleCount);
                Assert.Equal(entry.Confidence, confidenceEvent.Confidence);
            }
            else
            {
                Assert.Null(confidenceEvent);
            }
            state = result.State;
        }
    }

    [Fact]
    public void Add_uses_supplied_confidence_configuration_without_exposing_hidden_data()
    {
        var configuration = new KnowledgeConfidenceConfiguration(
            rumorSampleCount: 1,
            suspectedSampleCount: 3,
            probableSampleCount: 5,
            highConfidenceSampleCount: 8,
            rumorConfidence: 10,
            suspectedConfidence: 30,
            probableConfidence: 60,
            highConfidence: 90);
        var state = ActiveState();

        for (var sample = 1; sample <= 5; sample++)
        {
            state = KnowledgeObservationResolver.Add(
                state,
                new AddKnowledgeObservationCommand("fountain", [$"sample_{sample}"]),
                configuration).State;
        }

        var entry = Assert.Single(state.Knowledge.Entries);
        Assert.Equal(60, entry.Confidence);
        Assert.Equal(["sample_1", "sample_2", "sample_3", "sample_4", "sample_5"], entry.Observations);
    }

    [Fact]
    public void Add_does_not_emit_a_confidence_event_when_the_value_is_unchanged()
    {
        var state = ActiveState() with
        {
            Knowledge = new KnowledgeState([
                new KnowledgeEntry("fountain", observations: ["known"], sampleCount: 7, confidence: 100)
            ])
        };

        var result = KnowledgeObservationResolver.Add(
            state,
            new AddKnowledgeObservationCommand("fountain", ["known"]));

        Assert.Single(result.Events);
        Assert.IsType<KnowledgeSampleCountedEvent>(result.Events[0]);
        Assert.Equal(100, Assert.Single(result.State.Knowledge.Entries).Confidence);
    }

    [Fact]
    public void Configuration_rejects_invalid_thresholds_and_confidence_values()
    {
        Assert.Throws<ArgumentException>(() => new KnowledgeConfidenceConfiguration(
            rumorSampleCount: 2));
        Assert.Throws<ArgumentException>(() => new KnowledgeConfidenceConfiguration(
            suspectedSampleCount: 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new KnowledgeConfidenceConfiguration(
            rumorConfidence: 101));
        Assert.Throws<ArgumentException>(() => new KnowledgeConfidenceConfiguration(
            suspectedConfidence: 10,
            rumorConfidence: 20));
        Assert.Throws<ArgumentOutOfRangeException>(() => new KnowledgeConfidenceConfiguration()
            .Resolve(-1));
    }

    [Fact]
    public void Confidence_progression_replays_and_round_trips_deterministically()
    {
        var command = new AddKnowledgeObservationCommand("fountain", ["known"]);
        var first = KnowledgeObservationResolver.Add(ActiveState(), command);
        var second = KnowledgeObservationResolver.Add(ActiveState(), command);

        Assert.Equal(SaveGameSerializer.Serialize(first.State), SaveGameSerializer.Serialize(second.State));
        Assert.Equal(first.Events.Count, second.Events.Count);
        for (var index = 0; index < first.Events.Count; index++)
        {
            Assert.Equal(first.Events[index].GetType(), second.Events[index].GetType());
        }
        var firstConfidence = Assert.IsType<KnowledgeConfidenceUpdatedEvent>(first.Events[^1]);
        var secondConfidence = Assert.IsType<KnowledgeConfidenceUpdatedEvent>(second.Events[^1]);
        Assert.Equal(firstConfidence.SubjectId, secondConfidence.SubjectId);
        Assert.Equal(firstConfidence.SampleCount, secondConfidence.SampleCount);
        Assert.Equal(firstConfidence.Confidence, secondConfidence.Confidence);

        var restored = SaveGameSerializer.Deserialize(SaveGameSerializer.Serialize(first.State));
        var expectedEntry = Assert.Single(first.State.Knowledge.Entries);
        var actualEntry = Assert.Single(restored.Knowledge.Entries);
        Assert.Equal(expectedEntry.SubjectId, actualEntry.SubjectId);
        Assert.Equal(expectedEntry.Observations, actualEntry.Observations);
        Assert.Equal(expectedEntry.SampleCount, actualEntry.SampleCount);
        Assert.Equal(expectedEntry.Confidence, actualEntry.Confidence);
        Assert.Equal(GameState.CurrentSaveVersion, restored.SaveVersion);
    }

    private static GameState ActiveState() => GameState.Create(1234) with
    {
        Expedition = new ExpeditionState
        {
            Active = true,
            ExpeditionId = Guid.Parse("00000000-0000-0000-0000-000000000053")
        }
    };
}
