using System.Text.Json;
using Telengard.Content;
using Telengard.Core.Items;
using Telengard.Core.Rng;
using Xunit;

namespace Telengard.Architecture.Tests;

public sealed class ItemAffixTests
{
    [Fact]
    public void Generate_commits_affixes_without_mutating_the_source_and_emits_an_opaque_event()
    {
        var item = CreateItem();
        var result = ItemAffixGenerationResolver.Generate(
            item,
            new GenerateItemAffixesCommand(item.InstanceId, ["keen", "flaming"]));

        Assert.Empty(item.GeneratedAffixes);
        Assert.Equal(["keen", "flaming"], result.Item.GeneratedAffixes);
        Assert.Equal(item.InstanceId, result.Item.InstanceId);
        var generated = Assert.IsType<ItemAffixesGeneratedEvent>(Assert.Single(result.Events));
        Assert.Equal(item.InstanceId, generated.ItemId);
        Assert.DoesNotContain("keen", JsonSerializer.Serialize(generated));
    }

    [Fact]
    public void Generate_validates_boundaries_before_mutation()
    {
        var item = CreateItem();
        var otherId = Guid.Parse("00000000-0000-0000-0000-000000000002");

        Assert.Throws<ArgumentException>(() => new GenerateItemAffixesCommand(Guid.Empty));
        Assert.Throws<ArgumentException>(() => new GenerateItemAffixesCommand(item.InstanceId, ["keen", "keen"]));
        Assert.Throws<ArgumentException>(() => new GenerateItemAffixesCommand(item.InstanceId, [" "]));
        Assert.Throws<InvalidOperationException>(() => ItemAffixGenerationResolver.Generate(
            item,
            new GenerateItemAffixesCommand(otherId, ["keen"])));
        Assert.Empty(item.GeneratedAffixes);
    }

    [Fact]
    public void Generate_replays_deterministically()
    {
        var item = CreateItem();
        var command = new GenerateItemAffixesCommand(item.InstanceId, ["keen", "flaming"]);

        var first = ItemAffixGenerationResolver.Generate(item, command);
        var second = ItemAffixGenerationResolver.Generate(item, command);

        Assert.Equal(JsonSerializer.Serialize(first.Item), JsonSerializer.Serialize(second.Item));
        Assert.Equal(first.Events, second.Events);
    }

    [Fact]
    public void Content_selection_is_without_replacement_and_replays_from_stable_inputs()
    {
        var definition = new ItemDefinition("sword", "Sword", "weapon", affixPool: ["keen", "flaming", "frost"]);
        var first = ItemAffixEngine.Select(definition, 2, worldSeed: 42, contentVersion: "content-1", CreateItem().InstanceId);
        var second = ItemAffixEngine.Select(definition, 2, worldSeed: 42, contentVersion: "content-1", CreateItem().InstanceId);

        Assert.Equal(first, second);
        Assert.Equal(2, first.Count);
        Assert.Equal(first.Count, first.Distinct(StringComparer.Ordinal).Count());
        Assert.All(first, affix => Assert.Contains(affix, definition.AffixPool));
    }

    [Fact]
    public void Content_generation_integrates_definition_selection_with_the_core_transition()
    {
        var item = CreateItem();
        var definition = new ItemDefinition("ember-blade", "Ember Blade", "weapon", affixPool: ["keen", "flaming"]);

        var result = ItemAffixEngine.Generate(item, definition, 1, 42, "content-1");

        Assert.Single(result.Item.GeneratedAffixes);
        Assert.Contains(result.Item.GeneratedAffixes[0], definition.AffixPool);
        Assert.IsType<ItemAffixesGeneratedEvent>(Assert.Single(result.Events));
    }

    [Fact]
    public void Content_generation_rejects_a_definition_for_another_item()
    {
        var item = CreateItem();
        var definition = new ItemDefinition("different-item", "Different Item", "weapon", affixPool: ["keen"]);

        Assert.Throws<InvalidOperationException>(() => ItemAffixEngine.Generate(item, definition, 1, 42, "content-1"));
        Assert.Empty(item.GeneratedAffixes);
    }

    [Fact]
    public void Content_selection_validates_count_and_empty_pool_boundaries()
    {
        var definition = new ItemDefinition("sword", "Sword", "weapon", affixPool: ["keen"]);
        var rng = new DeterministicRng(42, "content-1").CreateStream("test");

        Assert.Empty(ItemAffixEngine.Select(definition, 0, rng));
        Assert.Throws<ArgumentOutOfRangeException>(() => ItemAffixEngine.Select(definition, -1, rng));
        Assert.Throws<ArgumentOutOfRangeException>(() => ItemAffixEngine.Select(definition, 2, rng));
        Assert.Throws<ArgumentException>(() => ItemAffixEngine.Select(
            definition,
            1,
            worldSeed: 42,
            contentVersion: "content-1",
            Guid.Empty));
    }

    private static ItemInstance CreateItem() => new(
        Guid.Parse("00000000-0000-0000-0000-000000000001"),
        "ember-blade");
}
