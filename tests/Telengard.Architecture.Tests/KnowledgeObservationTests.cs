using Telengard.Core.Knowledge;
using Telengard.Core.Simulation;
using Telengard.Save;
using Xunit;

namespace Telengard.Architecture.Tests;

public sealed class KnowledgeObservationTests
{
    [Fact]
    public void Add_creates_observed_entry_and_emits_event_after_commit()
    {
        var state = ActiveState();
        var command = new AddKnowledgeObservationCommand(
            "azure-fountain",
            ["restored_spell_power", "cold_water"]);

        var result = KnowledgeObservationResolver.Add(state, command);

        var entry = Assert.Single(result.State.Knowledge.Entries);
        Assert.Equal("azure-fountain", entry.SubjectId);
        Assert.Equal(["restored_spell_power", "cold_water"], entry.Observations);
        Assert.Equal(1, entry.SampleCount);
        Assert.Collection(
            result.Events,
            domainEvent =>
            {
                var added = Assert.IsType<KnowledgeObservationAddedEvent>(domainEvent);
                Assert.Equal("azure-fountain", added.SubjectId);
                Assert.Equal(["restored_spell_power", "cold_water"], added.Observations);
            },
            domainEvent =>
            {
                var counted = Assert.IsType<KnowledgeSampleCountedEvent>(domainEvent);
                Assert.Equal("azure-fountain", counted.SubjectId);
                Assert.Equal(1, counted.SampleCount);
            },
            domainEvent =>
            {
                var updated = Assert.IsType<KnowledgeConfidenceUpdatedEvent>(domainEvent);
                Assert.Equal("azure-fountain", updated.SubjectId);
                Assert.Equal(1, updated.SampleCount);
                Assert.Equal(25, updated.Confidence);
            });
    }

    [Fact]
    public void Add_merges_new_observations_and_preserves_other_journal_fields()
    {
        var existing = new KnowledgeEntry(
            "azure-fountain",
            observations: ["restored_spell_power"],
            sampleCount: 7,
            hypotheses: ["outcomes_depend_on_color"],
            confidence: 42,
            confirmedFacts: ["restored_spell_power"]);
        var state = ActiveState() with { Knowledge = new KnowledgeState([existing]) };

        var result = KnowledgeObservationResolver.Add(
            state,
            new AddKnowledgeObservationCommand(
                "azure-fountain",
                ["restored_spell_power", "caused_blindness"]));

        var entry = Assert.Single(result.State.Knowledge.Entries);
        Assert.Equal(["restored_spell_power", "caused_blindness"], entry.Observations);
        Assert.Equal(existing.SampleCount + 1, entry.SampleCount);
        Assert.Equal(existing.Hypotheses, entry.Hypotheses);
        Assert.Equal(100, entry.Confidence);
        Assert.Equal(existing.ConfirmedFacts, entry.ConfirmedFacts);
        Assert.Collection(
            result.Events,
            domainEvent =>
            {
                var added = Assert.IsType<KnowledgeObservationAddedEvent>(domainEvent);
                Assert.Equal(["caused_blindness"], added.Observations);
            },
            domainEvent =>
            {
                var counted = Assert.IsType<KnowledgeSampleCountedEvent>(domainEvent);
                Assert.Equal(existing.SampleCount + 1, counted.SampleCount);
            },
            domainEvent =>
            {
                var updated = Assert.IsType<KnowledgeConfidenceUpdatedEvent>(domainEvent);
                Assert.Equal(existing.SampleCount + 1, updated.SampleCount);
                Assert.Equal(100, updated.Confidence);
            });
    }

    [Fact]
    public void Add_preserves_observation_tags_but_counts_repeated_samples()
    {
        var state = ActiveState() with
        {
            Knowledge = new KnowledgeState([new KnowledgeEntry("fountain", observations: ["known"])]),
        };

        var result = KnowledgeObservationResolver.Add(
            state,
            new AddKnowledgeObservationCommand("fountain", ["known"]));

        var entry = Assert.Single(result.State.Knowledge.Entries);
        Assert.Equal(["known"], entry.Observations);
        Assert.Equal(1, entry.SampleCount);
        Assert.Collection(
            result.Events,
            domainEvent =>
            {
                var counted = Assert.IsType<KnowledgeSampleCountedEvent>(domainEvent);
                Assert.Equal("fountain", counted.SubjectId);
                Assert.Equal(1, counted.SampleCount);
            },
            domainEvent =>
            {
                var updated = Assert.IsType<KnowledgeConfidenceUpdatedEvent>(domainEvent);
                Assert.Equal(25, updated.Confidence);
            });

        var repeated = KnowledgeObservationResolver.Add(
            result.State,
            new AddKnowledgeObservationCommand("fountain", ["known"]));

        Assert.Equal(2, Assert.Single(repeated.State.Knowledge.Entries).SampleCount);
        var repeatedEvent = Assert.IsType<KnowledgeSampleCountedEvent>(repeated.Events[0]);
        Assert.Equal(2, repeatedEvent.SampleCount);
        var repeatedConfidence = Assert.IsType<KnowledgeConfidenceUpdatedEvent>(repeated.Events[1]);
        Assert.Equal(50, repeatedConfidence.Confidence);
    }

    [Fact]
    public void Add_validates_before_mutating_and_replays_deterministically()
    {
        var state = ActiveState();
        var command = new AddKnowledgeObservationCommand("fountain", ["known"]);

        Assert.Throws<InvalidOperationException>(() => KnowledgeObservationResolver.Add(
            state with { Expedition = new ExpeditionState() },
            command));
        Assert.Throws<InvalidOperationException>(() => KnowledgeObservationResolver.Add(
            state with { Player = new PlayerState { Alive = false } },
            command));
        Assert.Throws<ArgumentException>(() => new AddKnowledgeObservationCommand("fountain", []));
        Assert.Throws<ArgumentException>(() => new AddKnowledgeObservationCommand("fountain", ["known", "known"]));

        var maxed = state with
        {
            Knowledge = new KnowledgeState([
                new KnowledgeEntry("fountain", sampleCount: int.MaxValue)
            ])
        };
        Assert.Throws<OverflowException>(() => KnowledgeObservationResolver.Add(maxed, command));

        var first = KnowledgeObservationResolver.Add(state, command);
        var second = KnowledgeObservationResolver.Add(state, command);
        Assert.Equal(SaveGameSerializer.Serialize(first.State), SaveGameSerializer.Serialize(second.State));
        var firstEvent = Assert.IsType<KnowledgeObservationAddedEvent>(first.Events[0]);
        var secondEvent = Assert.IsType<KnowledgeObservationAddedEvent>(second.Events[0]);
        Assert.Equal(firstEvent.SubjectId, secondEvent.SubjectId);
        Assert.Equal(firstEvent.Observations, secondEvent.Observations);
        var firstSample = Assert.IsType<KnowledgeSampleCountedEvent>(first.Events[1]);
        var secondSample = Assert.IsType<KnowledgeSampleCountedEvent>(second.Events[1]);
        Assert.Equal(firstSample.SubjectId, secondSample.SubjectId);
        Assert.Equal(firstSample.SampleCount, secondSample.SampleCount);
        var firstConfidence = Assert.IsType<KnowledgeConfidenceUpdatedEvent>(first.Events[2]);
        var secondConfidence = Assert.IsType<KnowledgeConfidenceUpdatedEvent>(second.Events[2]);
        Assert.Equal(firstConfidence, secondConfidence);
    }

    [Fact]
    public void Observed_entries_round_trip_through_existing_save_contract()
    {
        var state = KnowledgeObservationResolver.Add(
            ActiveState(),
            new AddKnowledgeObservationCommand("fountain", ["known"])).State;

        var restored = SaveGameSerializer.Deserialize(SaveGameSerializer.Serialize(state));

        var expected = Assert.Single(state.Knowledge.Entries);
        var actual = Assert.Single(restored.Knowledge.Entries);
        Assert.Equal(expected.SubjectId, actual.SubjectId);
        Assert.Equal(expected.Observations, actual.Observations);
        Assert.Equal(expected.SampleCount, actual.SampleCount);
        Assert.Equal(expected.Hypotheses, actual.Hypotheses);
        Assert.Equal(expected.Confidence, actual.Confidence);
        Assert.Equal(expected.ConfirmedFacts, actual.ConfirmedFacts);
        Assert.Equal(GameState.CurrentSaveVersion, restored.SaveVersion);
    }

    private static GameState ActiveState() => GameState.Create(1234) with
    {
        Expedition = new ExpeditionState
        {
            Active = true,
            ExpeditionId = Guid.Parse("00000000-0000-0000-0000-000000000051")
        }
    };
}
