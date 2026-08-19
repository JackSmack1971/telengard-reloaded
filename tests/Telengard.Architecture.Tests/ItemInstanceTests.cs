using System.Text.Json;
using Telengard.Core.Items;
using Telengard.Save.Dto;
using Xunit;

namespace Telengard.Architecture.Tests;

public sealed class ItemInstanceTests
{
    [Fact]
    public void Instance_preserves_the_specified_runtime_fields()
    {
        var instance = new ItemInstance(
            Guid.Parse("00000000-0000-0000-0000-000000000001"),
            "ember-blade",
            ["keen", "flaming"],
            "brittle",
            identifiedState: true,
            durability: 37);

        Assert.Equal(Guid.Parse("00000000-0000-0000-0000-000000000001"), instance.InstanceId);
        Assert.Equal("ember-blade", instance.DefinitionId);
        Assert.Equal(["keen", "flaming"], instance.GeneratedAffixes);
        Assert.Equal("brittle", instance.Curse);
        Assert.True(instance.IdentifiedState);
        Assert.Equal(37, instance.Durability);
    }

    [Fact]
    public void Instance_rejects_invalid_runtime_boundaries()
    {
        var id = Guid.Parse("00000000-0000-0000-0000-000000000001");

        Assert.Throws<ArgumentException>(() => new ItemInstance(Guid.Empty, "sword"));
        Assert.Throws<ArgumentException>(() => new ItemInstance(id, ""));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ItemInstance(id, "sword", durability: -1));
        Assert.Throws<ArgumentException>(() => new ItemInstance(id, "sword", [" "]));
        Assert.Throws<ArgumentException>(() => new ItemInstance(id, "sword", ["keen", "keen"]));
    }

    [Fact]
    public void Instance_copies_mutable_inputs_and_normalizes_optional_curse()
    {
        var affixes = new List<string> { "keen" };
        var instance = new ItemInstance(
            Guid.Parse("00000000-0000-0000-0000-000000000001"),
            "sword",
            affixes,
            curse: " ");

        affixes.Add("flaming");

        Assert.Equal(["keen"], instance.GeneratedAffixes);
        Assert.Null(instance.Curse);
    }

    [Fact]
    public void Instance_does_not_expose_a_mutable_affix_snapshot()
    {
        var instance = new ItemInstance(
            Guid.Parse("00000000-0000-0000-0000-000000000001"),
            "sword",
            ["keen"]);

        Assert.Throws<NotSupportedException>(() => ((IList<string>)instance.GeneratedAffixes)[0] = "flaming");
        Assert.Equal(["keen"], instance.GeneratedAffixes);
    }

    [Fact]
    public void Observed_state_does_not_expose_a_mutable_affix_snapshot()
    {
        var observed = new ItemObservedState(
            Guid.Parse("00000000-0000-0000-0000-000000000001"),
            identified: true,
            ["keen"]);

        Assert.Throws<NotSupportedException>(() => ((IList<string>)observed.GeneratedAffixes)[0] = "flaming");
        Assert.Equal(["keen"], observed.GeneratedAffixes);
    }

    [Fact]
    public void Instance_serializes_runtime_state_without_content_definition_fields()
    {
        var instance = new ItemInstance(
            Guid.Parse("00000000-0000-0000-0000-000000000001"),
            "sword",
            ["keen"],
            durability: 12);

        var json = JsonSerializer.Serialize(instance);

        Assert.Contains("\"InstanceId\"", json);
        Assert.Contains("\"DefinitionId\":\"sword\"", json);
        Assert.Contains("\"Durability\":12", json);
        Assert.DoesNotContain("DisplayName", json);
        Assert.DoesNotContain("BaseProperties", json);
    }

    [Fact]
    public void Generated_instance_state_round_trips_through_the_explicit_save_dto()
    {
        var item = new ItemInstance(
            Guid.Parse("00000000-0000-0000-0000-000000000001"),
            "sword",
            ["keen", "flaming"],
            "brittle",
            identifiedState: true,
            durability: 12);

        var json = JsonSerializer.Serialize(ItemInstanceDto.FromState(item));
        var restored = JsonSerializer.Deserialize<ItemInstanceDto>(json)!.ToState();

        Assert.Equal(item.InstanceId, restored.InstanceId);
        Assert.Equal(item.DefinitionId, restored.DefinitionId);
        Assert.Equal(item.GeneratedAffixes, restored.GeneratedAffixes);
        Assert.Equal(item.Curse, restored.Curse);
        Assert.Equal(item.IdentifiedState, restored.IdentifiedState);
        Assert.Equal(item.Durability, restored.Durability);
    }

    [Fact]
    public void Unidentified_observation_redacts_generated_properties_until_identification()
    {
        var item = new ItemInstance(
            Guid.Parse("00000000-0000-0000-0000-000000000001"),
            "sword",
            ["keen"],
            "brittle",
            durability: 12);

        var observed = item.ToObservedState();

        Assert.False(observed.Identified);
        Assert.Empty(observed.GeneratedAffixes);
        Assert.Null(observed.Curse);
        Assert.Null(observed.Durability);
        var observedJson = JsonSerializer.Serialize(observed);
        Assert.DoesNotContain("keen", observedJson);
        Assert.DoesNotContain("brittle", observedJson);

        var identified = item.Identify().ToObservedState();
        Assert.True(identified.Identified);
        Assert.Equal(["keen"], identified.GeneratedAffixes);
        Assert.Equal("brittle", identified.Curse);
        Assert.Equal(12, identified.Durability);
    }

    [Fact]
    public void Instance_replaces_generated_affixes_immutably()
    {
        var instance = new ItemInstance(
            Guid.Parse("00000000-0000-0000-0000-000000000001"),
            "sword");

        var result = instance.WithGeneratedAffixes(["keen", "flaming"]);

        Assert.Empty(instance.GeneratedAffixes);
        Assert.Equal(["keen", "flaming"], result.GeneratedAffixes);
        Assert.Equal(instance.InstanceId, result.InstanceId);
        Assert.Equal(instance.DefinitionId, result.DefinitionId);
    }

    [Fact]
    public void Instance_replaces_a_curse_immutably_and_preserves_other_state()
    {
        var instance = new ItemInstance(
            Guid.Parse("00000000-0000-0000-0000-000000000001"),
            "sword",
            ["keen"],
            identifiedState: true,
            durability: 12);

        var result = instance.WithCurse("brittle");

        Assert.Null(instance.Curse);
        Assert.Equal("brittle", result.Curse);
        Assert.Equal(instance.GeneratedAffixes, result.GeneratedAffixes);
        Assert.True(result.IdentifiedState);
        Assert.Equal(12, result.Durability);
        Assert.Throws<ArgumentException>(() => instance.WithCurse(" "));
    }
}
