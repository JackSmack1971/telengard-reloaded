using System.Text.Json;
using Telengard.Content;
using Xunit;

namespace Telengard.Architecture.Tests;

public sealed class TalentSchemaTests
{
    [Fact]
    public void Definition_preserves_the_specified_constellation_fields()
    {
        var definition = new TalentDefinition(
            "counter-opening",
            "Steel",
            ["shield-discipline"],
            ["successful-defense:counterattack"],
            2);

        Assert.Equal("counter-opening", definition.Id);
        Assert.Equal("Steel", definition.Constellation);
        Assert.Equal(["shield-discipline"], definition.Prerequisites);
        Assert.Equal(["successful-defense:counterattack"], definition.Effects);
        Assert.Equal(2, definition.Cost);
    }

    [Fact]
    public void Definition_rejects_invalid_identity_cost_and_tags()
    {
        Assert.Throws<ArgumentException>(() => new TalentDefinition("", "Steel", [], [], 1));
        Assert.Throws<ArgumentException>(() => new TalentDefinition("talent", "", [], [], 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TalentDefinition("talent", "Steel", [], [], -1));
        Assert.Throws<ArgumentException>(() => new TalentDefinition("talent", "Steel", ["prerequisite", "prerequisite"], [], 1));
        Assert.Throws<ArgumentException>(() => new TalentDefinition("talent", "Steel", [" "], [], 1));
        Assert.Throws<ArgumentException>(() => new TalentDefinition("talent", "Steel", [], ["effect", "effect"], 1));
        Assert.Throws<ArgumentException>(() => new TalentDefinition("talent", "Steel", [], [" "], 1));
    }

    [Fact]
    public void Definition_copies_mutable_inputs_and_defaults_optional_collections()
    {
        var prerequisites = new List<string> { "shield-discipline" };
        var effects = new List<string> { "successful-defense:counterattack" };
        var definition = new TalentDefinition("counter-opening", "Steel", prerequisites, effects, 1);

        prerequisites.Add("iron-will");
        effects.Add("damage:physical");

        Assert.Equal(["shield-discipline"], definition.Prerequisites);
        Assert.Equal(["successful-defense:counterattack"], definition.Effects);

        var minimal = new TalentDefinition("first-step", "Survival", null, null, 0);
        Assert.Empty(minimal.Prerequisites);
        Assert.Empty(minimal.Effects);
    }

    [Fact]
    public void Definition_is_serializable_without_runtime_or_renderer_state()
    {
        var definition = new TalentDefinition("first-step", "Survival", [], ["safe-descent"], 1);

        var json = JsonSerializer.Serialize(definition);

        Assert.Contains("\"Id\":\"first-step\"", json);
        Assert.Contains("\"Constellation\":\"Survival\"", json);
        Assert.DoesNotContain("GameState", json);
        Assert.DoesNotContain("Renderer", json);
    }
}
