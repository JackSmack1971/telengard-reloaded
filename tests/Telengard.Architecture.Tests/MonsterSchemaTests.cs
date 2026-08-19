using System.Collections;
using System.Text.Json;
using Telengard.Content;
using Telengard.Core.Combat;
using Telengard.Core.Simulation;
using Xunit;

namespace Telengard.Architecture.Tests;

public sealed class MonsterSchemaTests
{
    [Fact]
    public void Definition_preserves_the_specified_content_fields()
    {
        var definition = new MonsterDefinition(
            "crypt-stalker",
            "Crypt Stalker",
            MonsterFamily.Undead,
            new MonsterStats(new Dictionary<string, int> { ["hit_points"] = 12 }),
            ["ambusher"],
            ["poison"],
            ["fire"],
            ["claw"],
            ["pursuer"],
            new MonsterSpawnRules(new Dictionary<string, string> { ["depth_band"] = "lower" }),
            "undead-common");

        Assert.Equal("crypt-stalker", definition.Id);
        Assert.Equal("Crypt Stalker", definition.DisplayName);
        Assert.Equal(MonsterFamily.Undead, definition.Family);
        Assert.Equal(12, definition.BaseStats["hit_points"]);
        Assert.Equal(["ambusher"], definition.Traits);
        Assert.Equal(["poison"], definition.Resistances);
        Assert.Equal(["fire"], definition.Vulnerabilities);
        Assert.Equal(["claw"], definition.Actions);
        Assert.Equal(["pursuer"], definition.Behaviors);
        Assert.Equal("lower", definition.SpawnRules["depth_band"]);
        Assert.Equal("undead-common", definition.LootTable);
    }

    [Fact]
    public void Definition_rejects_invalid_identity_family_and_tags()
    {
        Assert.Throws<ArgumentException>(() => new MonsterDefinition("", "Rat", MonsterFamily.Beasts));
        Assert.Throws<ArgumentException>(() => new MonsterDefinition("rat", "", MonsterFamily.Beasts));
        Assert.Throws<ArgumentOutOfRangeException>(() => new MonsterDefinition("rat", "Rat", (MonsterFamily)999));
        Assert.Throws<ArgumentException>(() => new MonsterDefinition("rat", "Rat", MonsterFamily.Beasts, traits: [""]));
        Assert.Throws<ArgumentException>(() => new MonsterDefinition("rat", "Rat", MonsterFamily.Beasts, behaviors: ["flee", "flee"]));
        Assert.Throws<ArgumentException>(() => new MonsterStats(new Dictionary<string, int> { [" "] = 1 }));
        Assert.Throws<ArgumentException>(() => new MonsterSpawnRules(new Dictionary<string, string> { [" "] = "value" }));
        Assert.Throws<ArgumentException>(() => new MonsterSpawnRules(new Dictionary<string, string> { ["key"] = " " }));
        Assert.Throws<ArgumentException>(() => new MonsterStats(new DuplicateReadOnlyDictionary<int>()));
        Assert.Throws<ArgumentException>(() => new MonsterSpawnRules(new DuplicateReadOnlyDictionary<string>()));
    }

    [Fact]
    public void Definition_copies_mutable_input_collections()
    {
        var stats = new Dictionary<string, int> { ["hit_points"] = 5 };
        var traits = new List<string> { "territorial" };
        var definition = new MonsterDefinition("rat", "Rat", MonsterFamily.Beasts, new MonsterStats(stats), traits);

        stats["hit_points"] = 99;
        traits.Add("pack_hunter");

        Assert.Equal(5, definition.BaseStats["hit_points"]);
        Assert.Equal(["territorial"], definition.Traits);
        Assert.Null(new MonsterDefinition("rat", "Rat", MonsterFamily.Beasts, lootTable: " ").LootTable);
    }

    [Fact]
    public void Instance_contains_runtime_state_separate_from_definition()
    {
        var position = new DungeonPosition(3, 4, 5);
        var instance = new MonsterInstance(
            Guid.Parse("00000000-0000-0000-0000-000000000001"),
            "crypt-stalker",
            2,
            9,
            position,
            ["slowed"],
            "pursuing");

        Assert.Equal("crypt-stalker", instance.DefinitionId);
        Assert.Equal(2, instance.Level);
        Assert.Equal(9, instance.CurrentHitPoints);
        Assert.Equal(position, instance.Position);
        Assert.Equal(["slowed"], instance.TemporaryEffects);
        Assert.Equal("pursuing", instance.CurrentBehaviorState);
        Assert.Null(new MonsterInstance(
            Guid.Parse("00000000-0000-0000-0000-000000000002"),
            "rat",
            1,
            1,
            position,
            currentBehaviorState: " ").CurrentBehaviorState);
    }

    [Fact]
    public void Instance_does_not_expose_a_mutable_effect_snapshot()
    {
        var instance = new MonsterInstance(
            Guid.Parse("00000000-0000-0000-0000-000000000001"),
            "rat",
            1,
            1,
            new DungeonPosition(1, 0, 0),
            ["slowed"]);

        Assert.Throws<NotSupportedException>(() => ((IList<string>)instance.TemporaryEffects)[0] = "poison");
        Assert.Equal(["slowed"], instance.TemporaryEffects);
    }

    [Fact]
    public void Instance_rejects_invalid_runtime_boundaries()
    {
        var position = new DungeonPosition(1, 0, 0);
        var id = Guid.Parse("00000000-0000-0000-0000-000000000001");

        Assert.Throws<ArgumentException>(() => new MonsterInstance(Guid.Empty, "rat", 1, 1, position));
        Assert.Throws<ArgumentException>(() => new MonsterInstance(id, "", 1, 1, position));
        Assert.Throws<ArgumentOutOfRangeException>(() => new MonsterInstance(id, "rat", 0, 1, position));
        Assert.Throws<ArgumentOutOfRangeException>(() => new MonsterInstance(id, "rat", 1, -1, position));
        Assert.Throws<ArgumentNullException>(() => new MonsterInstance(id, "rat", 1, 1, null!));
        Assert.Throws<ArgumentException>(() => new MonsterInstance(id, "rat", 1, 1, position, [" "]));
    }

    [Fact]
    public void Definition_is_serializable_without_runtime_or_renderer_state()
    {
        var definition = new MonsterDefinition("rat", "Rat", MonsterFamily.Beasts);

        var json = JsonSerializer.Serialize(definition);

        Assert.Contains("\"Id\":\"rat\"", json);
        Assert.DoesNotContain("Position", json);
        Assert.DoesNotContain("CurrentHitPoints", json);
    }

    private sealed class DuplicateReadOnlyDictionary<TValue> : IReadOnlyDictionary<string, TValue>
    {
        public TValue this[string key] => throw new KeyNotFoundException();
        public IEnumerable<string> Keys => ["duplicate"];
        public IEnumerable<TValue> Values => [];
        public int Count => 2;
        public bool ContainsKey(string key) => true;
        public bool TryGetValue(string key, out TValue value)
        {
            value = default!;
            return false;
        }

        public IEnumerator<KeyValuePair<string, TValue>> GetEnumerator()
        {
            yield return new KeyValuePair<string, TValue>("duplicate", DuplicateValue());
            yield return new KeyValuePair<string, TValue>("duplicate", DuplicateValue());
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private static TValue DuplicateValue() => typeof(TValue) == typeof(string) ? (TValue)(object)"value" : default!;
    }
}
