using System.Text.Json;
using Telengard.Content;
using Xunit;

namespace Telengard.Architecture.Tests;

public sealed class SpellSchemaTests
{
    [Fact]
    public void Definition_preserves_the_specified_content_fields()
    {
        var definition = new SpellDefinition(
            "ember-bolt",
            "Ember Bolt",
            "A spark gathers at your fingertips.",
            ["The spark seeks a nearby foe.", "The bolt burns with fire."],
            3,
            "single_target",
            ["damage:fire"],
            ["water_target"]);

        Assert.Equal("ember-bolt", definition.Id);
        Assert.Equal("Ember Bolt", definition.Name);
        Assert.Equal("A spark gathers at your fingertips.", definition.InitialDescription);
        Assert.Equal(["The spark seeks a nearby foe.", "The bolt burns with fire."], definition.DiscoveredDescriptions);
        Assert.Equal(3, definition.Cost);
        Assert.Equal("single_target", definition.TargetingRule);
        Assert.Equal(["damage:fire"], definition.Effects);
        Assert.Equal(["water_target"], definition.Interactions);
    }

    [Fact]
    public void Definition_rejects_invalid_identity_cost_and_tags()
    {
        Assert.Throws<ArgumentException>(() => new SpellDefinition(
            "", "Spark", "A spark.", [], 1, "single_target"));
        Assert.Throws<ArgumentException>(() => new SpellDefinition(
            "spark", "", "A spark.", [], 1, "single_target"));
        Assert.Throws<ArgumentException>(() => new SpellDefinition(
            "spark", "Spark", "", [], 1, "single_target"));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SpellDefinition(
            "spark", "Spark", "A spark.", [], -1, "single_target"));
        Assert.Throws<ArgumentException>(() => new SpellDefinition(
            "spark", "Spark", "A spark.", [], 1, ""));
        Assert.Throws<ArgumentException>(() => new SpellDefinition(
            "spark", "Spark", "A spark.", ["known", "known"], 1, "single_target"));
        Assert.Throws<ArgumentException>(() => new SpellDefinition(
            "spark", "Spark", "A spark.", [" "], 1, "single_target"));
        Assert.Throws<ArgumentException>(() => new SpellDefinition(
            "spark", "Spark", "A spark.", [], 1, "single_target", ["damage", "damage"]));
        Assert.Throws<ArgumentException>(() => new SpellDefinition(
            "spark", "Spark", "A spark.", [], 1, "single_target", interactions: [" "]));
    }

    [Fact]
    public void Definition_copies_mutable_inputs_and_defaults_optional_collections()
    {
        var discoveredDescriptions = new List<string> { "The spark is warm." };
        var effects = new List<string> { "damage:fire" };
        var interactions = new List<string> { "oil_target" };
        var definition = new SpellDefinition(
            "spark",
            "Spark",
            "A spark.",
            discoveredDescriptions,
            1,
            "single_target",
            effects,
            interactions);

        discoveredDescriptions.Add("The spark is bright.");
        effects.Add("light");
        interactions.Add("water_target");

        Assert.Equal(["The spark is warm."], definition.DiscoveredDescriptions);
        Assert.Equal(["damage:fire"], definition.Effects);
        Assert.Equal(["oil_target"], definition.Interactions);

        var minimal = new SpellDefinition("spark", "Spark", "A spark.", [], 0, "self");
        Assert.Empty(minimal.Effects);
        Assert.Empty(minimal.Interactions);
    }

    [Fact]
    public void Definition_is_serializable_without_runtime_or_renderer_state()
    {
        var definition = new SpellDefinition("spark", "Spark", "A spark.", [], 1, "self");

        var json = JsonSerializer.Serialize(definition);

        Assert.Contains("\"Id\":\"spark\"", json);
        Assert.Contains("\"InitialDescription\":\"A spark.\"", json);
        Assert.DoesNotContain("CurrentSpellPower", json);
        Assert.DoesNotContain("Position", json);
    }
}
