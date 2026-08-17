using System.Text.Json;
using Telengard.Core.Items;
using Xunit;

namespace Telengard.Architecture.Tests;

public sealed class ItemIdentificationTests
{
    [Fact]
    public void Identify_commits_an_immutable_transition_and_emits_an_opaque_event()
    {
        var item = CreateItem();

        var result = ItemIdentificationResolver.Identify(
            item,
            new IdentifyItemCommand(item.InstanceId));

        Assert.False(item.IdentifiedState);
        Assert.True(result.Item.IdentifiedState);
        Assert.Equal(item.InstanceId, result.Item.InstanceId);
        Assert.Equal(item.DefinitionId, result.Item.DefinitionId);
        Assert.Equal(item.GeneratedAffixes, result.Item.GeneratedAffixes);
        Assert.Equal(item.Curse, result.Item.Curse);
        Assert.Equal(item.Durability, result.Item.Durability);

        var identified = Assert.IsType<ItemIdentifiedEvent>(Assert.Single(result.Events));
        Assert.Equal(item.InstanceId, identified.ItemId);
    }

    [Fact]
    public void Identify_is_idempotent_without_republishing_a_transition_event()
    {
        var item = CreateItem().Identify();

        var result = ItemIdentificationResolver.Identify(
            item,
            new IdentifyItemCommand(item.InstanceId));

        Assert.Same(item, result.Item);
        Assert.Empty(result.Events);
    }

    [Fact]
    public void Identify_validates_target_and_command_boundaries_before_mutation()
    {
        var item = CreateItem();
        var otherId = Guid.Parse("00000000-0000-0000-0000-000000000002");

        Assert.Throws<ArgumentException>(() => new IdentifyItemCommand(Guid.Empty));
        Assert.Throws<InvalidOperationException>(() => ItemIdentificationResolver.Identify(
            item,
            new IdentifyItemCommand(otherId)));
        Assert.False(item.IdentifiedState);
        Assert.Throws<ArgumentNullException>(() => ItemIdentificationResolver.Identify(
            null!,
            new IdentifyItemCommand(item.InstanceId)));
        Assert.Throws<ArgumentNullException>(() => ItemIdentificationResolver.Identify(
            item,
            null!));
    }

    [Fact]
    public void Identify_replays_deterministically()
    {
        var item = CreateItem();
        var command = new IdentifyItemCommand(item.InstanceId);

        var first = ItemIdentificationResolver.Identify(item, command);
        var second = ItemIdentificationResolver.Identify(item, command);

        Assert.Equal(JsonSerializer.Serialize(first.Item), JsonSerializer.Serialize(second.Item));
        Assert.Equal(first.Events, second.Events);
    }

    private static ItemInstance CreateItem() => new(
        Guid.Parse("00000000-0000-0000-0000-000000000001"),
        "ember-blade",
        ["keen"],
        "brittle",
        durability: 37);
}
