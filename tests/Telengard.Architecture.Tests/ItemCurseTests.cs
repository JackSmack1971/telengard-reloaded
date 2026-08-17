using System.Text.Json;
using Telengard.Content;
using Telengard.Core.Items;
using Telengard.Core.Rng;
using Xunit;

namespace Telengard.Architecture.Tests;

public sealed class ItemCurseTests
{
    [Fact]
    public void Apply_commits_a_curse_without_mutating_the_source_and_emits_an_opaque_event()
    {
        var item = CreateItem();
        var result = ItemCurseResolver.Apply(
            item,
            new ApplyItemCurseCommand(item.InstanceId, "brittle"));

        Assert.Null(item.Curse);
        Assert.Equal("brittle", result.Item.Curse);
        var cursed = Assert.IsType<ItemCursedEvent>(Assert.Single(result.Events));
        Assert.Equal(item.InstanceId, cursed.ItemId);
        Assert.DoesNotContain("brittle", JsonSerializer.Serialize(cursed));
    }

    [Fact]
    public void Apply_validates_the_command_before_mutation()
    {
        var item = CreateItem();
        var otherId = Guid.Parse("00000000-0000-0000-0000-000000000002");

        Assert.Throws<ArgumentException>(() => new ApplyItemCurseCommand(Guid.Empty, "brittle"));
        Assert.Throws<ArgumentException>(() => new ApplyItemCurseCommand(item.InstanceId, " "));
        Assert.Throws<InvalidOperationException>(() => ItemCurseResolver.Apply(
            item,
            new ApplyItemCurseCommand(otherId, "brittle")));
        Assert.Null(item.Curse);
    }

    [Fact]
    public void Apply_replays_deterministically()
    {
        var item = CreateItem();
        var command = new ApplyItemCurseCommand(item.InstanceId, "brittle");

        var first = ItemCurseResolver.Apply(item, command);
        var second = ItemCurseResolver.Apply(item, command);

        Assert.Equal(JsonSerializer.Serialize(first.Item), JsonSerializer.Serialize(second.Item));
        Assert.Equal(first.Events, second.Events);
    }

    [Fact]
    public void Content_selection_replays_from_stable_inputs_and_uses_the_curse_pool()
    {
        var definition = new ItemDefinition("sword", "Sword", "weapon", cursePool: ["brittle", "hungry"]);

        var first = ItemCurseEngine.Select(
            definition,
            worldSeed: 42,
            contentVersion: "content-1",
            CreateItem().InstanceId);
        var second = ItemCurseEngine.Select(
            definition,
            worldSeed: 42,
            contentVersion: "content-1",
            CreateItem().InstanceId);

        Assert.Equal(first, second);
        Assert.Contains(first, definition.CursePool);
    }

    [Fact]
    public void Content_generation_integrates_definition_selection_with_the_core_transition()
    {
        var item = CreateItem();
        var definition = new ItemDefinition("sword", "Sword", "weapon", cursePool: ["brittle"]);

        var result = ItemCurseEngine.Generate(item, definition, 42, "content-1");

        Assert.Equal("brittle", result.Item.Curse);
        Assert.IsType<ItemCursedEvent>(Assert.Single(result.Events));
    }

    [Fact]
    public void Generated_curse_replays_with_affixes_from_the_same_stable_item_inputs()
    {
        var definition = new ItemDefinition(
            "sword",
            "Sword",
            "weapon",
            affixPool: ["keen", "flaming"],
            cursePool: ["brittle", "hungry"]);

        var first = ItemCurseEngine.Generate(
            ItemAffixEngine.Generate(CreateItem(), definition, 1, 42, "content-1").Item,
            definition,
            42,
            "content-1");
        var second = ItemCurseEngine.Generate(
            ItemAffixEngine.Generate(CreateItem(), definition, 1, 42, "content-1").Item,
            definition,
            42,
            "content-1");

        Assert.Equal(first.Item.GeneratedAffixes, second.Item.GeneratedAffixes);
        Assert.Equal(first.Item.Curse, second.Item.Curse);
        Assert.Equal(first.Events, second.Events);
    }

    [Fact]
    public void Content_generation_rejects_a_definition_for_another_item()
    {
        var item = CreateItem();
        var definition = new ItemDefinition("different-item", "Different Item", "weapon", cursePool: ["brittle"]);

        Assert.Throws<InvalidOperationException>(() => ItemCurseEngine.Generate(item, definition, 42, "content-1"));
        Assert.Null(item.Curse);
    }

    [Fact]
    public void Content_selection_rejects_empty_pools_and_invalid_identifiers()
    {
        var definition = new ItemDefinition("sword", "Sword", "weapon");
        var rng = new DeterministicRng(42, "content-1").CreateStream("test");

        Assert.Throws<InvalidOperationException>(() => ItemCurseEngine.Select(definition, rng));
        Assert.Throws<ArgumentException>(() => ItemCurseEngine.Select(
            definition,
            worldSeed: 42,
            contentVersion: "content-1",
            Guid.Empty));
    }

    private static ItemInstance CreateItem() => new(
        Guid.Parse("00000000-0000-0000-0000-000000000001"),
        "sword");
}
