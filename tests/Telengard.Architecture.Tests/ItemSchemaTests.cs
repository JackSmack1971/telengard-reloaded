using System.Collections;
using System.Text.Json;
using Telengard.Content;
using Xunit;

namespace Telengard.Architecture.Tests;

public sealed class ItemSchemaTests
{
    [Fact]
    public void Definition_preserves_the_specified_content_fields()
    {
        var definition = new ItemDefinition(
            "ember-blade",
            "Ember Blade",
            "weapon",
            new ItemProperties(new Dictionary<string, string>
            {
                ["damage"] = "8",
                ["damage_type"] = "fire"
            }),
            ["keen", "flaming"],
            ["brittle"],
            new ItemRarityRules(new Dictionary<string, string> { ["tier"] = "rare" }),
            new ItemDepthRules(new Dictionary<string, string> { ["minimum_floor"] = "10" }),
            "Warm Sword");

        Assert.Equal("ember-blade", definition.Id);
        Assert.Equal("Ember Blade", definition.DisplayName);
        Assert.Equal("weapon", definition.Category);
        Assert.Equal("8", definition.BaseProperties["damage"]);
        Assert.Equal("fire", definition.BaseProperties["damage_type"]);
        Assert.Equal(["keen", "flaming"], definition.AffixPool);
        Assert.Equal(["brittle"], definition.CursePool);
        Assert.Equal("rare", definition.RarityRules["tier"]);
        Assert.Equal("10", definition.DepthRules["minimum_floor"]);
        Assert.Equal("Warm Sword", definition.UnidentifiedName);
    }

    [Fact]
    public void Definition_rejects_invalid_identity_rules_and_pools()
    {
        Assert.Throws<ArgumentException>(() => new ItemDefinition("", "Sword", "weapon"));
        Assert.Throws<ArgumentException>(() => new ItemDefinition("sword", "", "weapon"));
        Assert.Throws<ArgumentException>(() => new ItemDefinition("sword", "Sword", ""));
        Assert.Throws<ArgumentException>(() => new ItemDefinition("sword", "Sword", "weapon", affixPool: ["keen", "keen"]));
        Assert.Throws<ArgumentException>(() => new ItemDefinition("sword", "Sword", "weapon", cursePool: [" "]));
        Assert.Throws<ArgumentException>(() => new ItemProperties(new Dictionary<string, string> { [" "] = "8" }));
        Assert.Throws<ArgumentException>(() => new ItemProperties(new DuplicateReadOnlyDictionary()));
        Assert.Throws<ArgumentException>(() => new ItemRarityRules(new Dictionary<string, string> { ["tier"] = " " }));
        Assert.Throws<ArgumentException>(() => new ItemDepthRules(new Dictionary<string, string> { ["floor"] = " " }));
    }

    [Fact]
    public void Definition_copies_mutable_inputs_and_has_no_runtime_state()
    {
        var properties = new Dictionary<string, string> { ["damage"] = "8" };
        var affixes = new List<string> { "keen" };
        var definition = new ItemDefinition(
            "sword",
            "Sword",
            "weapon",
            new ItemProperties(properties),
            affixes,
            unidentifiedName: "Unknown Sword");

        properties["damage"] = "99";
        affixes.Add("flaming");

        Assert.Equal("8", definition.BaseProperties["damage"]);
        Assert.Equal(["keen"], definition.AffixPool);
        Assert.Null(new ItemDefinition("sword", "Sword", "weapon", unidentifiedName: " ").UnidentifiedName);

        var json = JsonSerializer.Serialize(definition);
        Assert.Contains("\"Id\":\"sword\"", json);
        Assert.Contains("\"Category\":\"weapon\"", json);
        Assert.DoesNotContain("InstanceId", json);
        Assert.DoesNotContain("Durability", json);
        Assert.DoesNotContain("IdentifiedState", json);
    }

    private sealed class DuplicateReadOnlyDictionary : IReadOnlyDictionary<string, string>
    {
        public string this[string key] => throw new KeyNotFoundException();
        public IEnumerable<string> Keys => ["duplicate"];
        public IEnumerable<string> Values => ["value"];
        public int Count => 2;
        public bool ContainsKey(string key) => true;
        public bool TryGetValue(string key, out string value)
        {
            value = "value";
            return false;
        }

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            yield return new KeyValuePair<string, string>("duplicate", "value");
            yield return new KeyValuePair<string, string>("duplicate", "value");
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
