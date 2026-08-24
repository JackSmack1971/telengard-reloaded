using Telengard.Content;
using Xunit;

namespace Telengard.Architecture.Tests;

public sealed class FirstSliceMonsterRosterTests
{
    [Fact]
    public void Production_pack_contains_a_valid_distinct_first_slice_roster()
    {
        var pack = ContentPackLoader.Load(RepositoryContentRoot());

        Assert.InRange(pack.Monsters.Count, 8, 12);
        Assert.Equal(pack.Monsters.Count, pack.Monsters.Definitions.Keys.Distinct(StringComparer.Ordinal).Count());
        Assert.All(pack.Monsters.Definitions.Values, monster =>
        {
            Assert.NotEmpty(monster.DisplayName);
            Assert.NotEmpty(monster.BaseStats.Values);
            Assert.NotEmpty(monster.Traits);
            Assert.NotEmpty(monster.Actions);
            Assert.NotEmpty(monster.Behaviors);
            Assert.NotEmpty(monster.SpawnRules.Values);
            Assert.Equal("upper-ruins-loot", monster.LootTable);
        });

        Assert.True(pack.Monsters.Definitions.Values.Select(monster => monster.Family).Distinct().Count() >= 5);
        Assert.True(pack.Monsters.Definitions.Values.SelectMany(monster => monster.Resistances).Distinct().Any());
        Assert.True(pack.Monsters.Definitions.Values.SelectMany(monster => monster.Vulnerabilities).Distinct().Any());
        Assert.True(pack.Monsters.Definitions.Values.SelectMany(monster => monster.Behaviors).Distinct().Count() >= 8);
    }

    [Fact]
    public void Production_roster_loads_canonically()
    {
        var first = ContentPackLoader.Load(RepositoryContentRoot());
        var second = ContentPackLoader.Load(RepositoryContentRoot());

        Assert.Equal(first.ContentVersion, second.ContentVersion);
        Assert.Equal(first.Monsters.Definitions.Keys, second.Monsters.Definitions.Keys);
        Assert.Equal(
            first.Monsters.Definitions.Values.Select(Fingerprint),
            second.Monsters.Definitions.Values.Select(Fingerprint));
    }

    private static string Fingerprint(MonsterDefinition monster) => string.Join(
        "|",
        monster.Id,
        monster.DisplayName,
        monster.Family,
        string.Join(",", monster.BaseStats.Values.OrderBy(pair => pair.Key).Select(pair => $"{pair.Key}={pair.Value}")),
        string.Join(",", monster.Traits),
        string.Join(",", monster.Resistances),
        string.Join(",", monster.Vulnerabilities),
        string.Join(",", monster.Actions),
        string.Join(",", monster.Behaviors),
        string.Join(",", monster.SpawnRules.Values.OrderBy(pair => pair.Key).Select(pair => $"{pair.Key}={pair.Value}")),
        monster.LootTable ?? "");

    private static string RepositoryContentRoot() =>
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "content");
}
