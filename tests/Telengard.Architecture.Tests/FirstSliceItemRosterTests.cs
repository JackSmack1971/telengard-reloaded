using Telengard.Content;
using Telengard.Core.Items;
using Xunit;

namespace Telengard.Architecture.Tests;

public sealed class FirstSliceItemRosterTests
{
    [Fact]
    public void Production_pack_contains_a_valid_distinct_first_slice_roster()
    {
        var pack = ContentPackLoader.Load(RepositoryContentRoot());

        Assert.InRange(pack.Items.Count, 10, 15);
        Assert.Equal(pack.Items.Count, pack.Items.Definitions.Keys.Distinct(StringComparer.Ordinal).Count());
        Assert.All(pack.Items.Definitions.Values, item =>
        {
            Assert.NotEmpty(item.DisplayName);
            Assert.NotEmpty(item.Category);
            Assert.NotEmpty(item.BaseProperties.Values);
            Assert.NotEmpty(item.RarityRules.Values);
            Assert.NotEmpty(item.DepthRules.Values);
            Assert.Contains("CONFIGURATION/TUNING DECISION REQUIRED", item.RarityRules.Values.Values);
            Assert.Contains("CONFIGURATION/TUNING DECISION REQUIRED", item.DepthRules.Values.Values);
        });

        Assert.True(pack.Items.Definitions.Values.Select(item => item.Category).Distinct(StringComparer.Ordinal).Count() >= 6);
        Assert.Contains(pack.Items.Definitions.Values, item => item.UnidentifiedName is not null);
        Assert.Contains(pack.Items.Definitions.Values, item => item.AffixPool.Count > 0);
        Assert.Contains(pack.Items.Definitions.Values, item => item.CursePool.Count > 0);
    }

    [Fact]
    public void Loading_items_does_not_create_instances_or_knowledge_and_is_canonical()
    {
        var first = ContentPackLoader.Load(RepositoryContentRoot());
        var second = ContentPackLoader.Load(RepositoryContentRoot());

        Assert.Equal(first.ContentVersion, second.ContentVersion);
        Assert.Equal(first.Items.Definitions.Keys, second.Items.Definitions.Keys);
        Assert.Equal(first.Items.Definitions.Values.Select(Fingerprint), second.Items.Definitions.Values.Select(Fingerprint));

        Assert.Contains(first.Items.Definitions.Values, item => item.UnidentifiedName is not null);
        Assert.DoesNotContain(first.Items.Definitions.Values, item => item.GetType() == typeof(ItemInstance));
    }

    private static string Fingerprint(ItemDefinition item) => string.Join(
        "|",
        item.Id,
        item.DisplayName,
        item.Category,
        string.Join(",", item.BaseProperties.Values.OrderBy(pair => pair.Key).Select(pair => $"{pair.Key}={pair.Value}")),
        string.Join(",", item.AffixPool),
        string.Join(",", item.CursePool),
        string.Join(",", item.RarityRules.Values.OrderBy(pair => pair.Key).Select(pair => $"{pair.Key}={pair.Value}")),
        string.Join(",", item.DepthRules.Values.OrderBy(pair => pair.Key).Select(pair => $"{pair.Key}={pair.Value}")),
        item.UnidentifiedName ?? "");

    private static string RepositoryContentRoot() =>
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "content");
}
