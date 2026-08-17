using System.Text.Json.Nodes;
using Telengard.Core.Knowledge;
using Telengard.Core.Simulation;
using Telengard.Save;
using Xunit;

namespace Telengard.Architecture.Tests;

public sealed class KnowledgeEntryTests
{
    [Fact]
    public void Entry_preserves_observed_fields_and_copies_input_collections()
    {
        var observations = new List<string> { "restored_spell_power" };
        var hypotheses = new List<string> { "safe_when_azure" };
        var confirmedFacts = new List<string> { "restores_spell_power" };

        var entry = new KnowledgeEntry(
            "azure-fountain",
            observations,
            sampleCount: 7,
            hypotheses: hypotheses,
            confidence: 42,
            confirmedFacts: confirmedFacts);

        observations.Add("caused_blindness");
        hypotheses.Clear();
        confirmedFacts.Clear();

        Assert.Equal("azure-fountain", entry.SubjectId);
        Assert.Equal(["restored_spell_power"], entry.Observations);
        Assert.Equal(7, entry.SampleCount);
        Assert.Equal(["safe_when_azure"], entry.Hypotheses);
        Assert.Equal(42, entry.Confidence);
        Assert.Equal(["restores_spell_power"], entry.ConfirmedFacts);
    }

    [Fact]
    public void Entry_and_state_reject_invalid_or_duplicate_values()
    {
        Assert.Throws<ArgumentException>(() => new KnowledgeEntry(" "));
        Assert.Throws<ArgumentOutOfRangeException>(() => new KnowledgeEntry("fountain", sampleCount: -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new KnowledgeEntry("fountain", confidence: -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new KnowledgeEntry("fountain", confidence: 101));
        Assert.Throws<ArgumentException>(() => new KnowledgeEntry("fountain", observations: ["observed", "observed"]));
        Assert.Throws<ArgumentException>(() => new KnowledgeState([
            new KnowledgeEntry("fountain"),
            new KnowledgeEntry("fountain")
        ]));
    }

    [Fact]
    public void Entry_state_round_trips_through_explicit_save_dtos()
    {
        var entry = new KnowledgeEntry(
            "azure-fountain",
            observations: ["restored_spell_power", "caused_blindness"],
            sampleCount: 7,
            hypotheses: ["outcomes_depend_on_color"],
            confidence: 42,
            confirmedFacts: ["restored_spell_power"]);
        var state = GameState.Create(1234) with { Knowledge = new KnowledgeState([entry]) };

        var restored = SaveGameSerializer.Deserialize(SaveGameSerializer.Serialize(state));

        Assert.Equal(SaveGameSerializer.Serialize(state), SaveGameSerializer.Serialize(restored));
        var restoredEntry = Assert.Single(restored.Knowledge.Entries);
        Assert.Equal(entry.SubjectId, restoredEntry.SubjectId);
        Assert.Equal(entry.Observations, restoredEntry.Observations);
        Assert.Equal(entry.SampleCount, restoredEntry.SampleCount);
        Assert.Equal(entry.Hypotheses, restoredEntry.Hypotheses);
        Assert.Equal(entry.Confidence, restoredEntry.Confidence);
        Assert.Equal(entry.ConfirmedFacts, restoredEntry.ConfirmedFacts);
        Assert.Equal(GameState.CurrentSaveVersion, restored.SaveVersion);
    }

    [Fact]
    public void Version_seven_save_migrates_without_knowledge_entries()
    {
        var document = JsonNode.Parse(SaveGameSerializer.Serialize(GameState.Create(1234)))!.AsObject();
        document["saveVersion"] = 7;
        document["knowledge"] = new JsonObject();

        var restored = SaveGameSerializer.Deserialize(document.ToJsonString());

        Assert.Equal(GameState.CurrentSaveVersion, restored.SaveVersion);
        Assert.Empty(restored.Knowledge.Entries);
    }
}
