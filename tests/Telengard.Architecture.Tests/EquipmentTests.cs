using System.Text.Json;
using System.Text.Json.Nodes;
using Telengard.Core.Items;
using Telengard.Core.Simulation;
using Telengard.Save;
using Xunit;

namespace Telengard.Architecture.Tests;

public sealed class EquipmentTests
{
    private static readonly Guid ItemId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid OtherItemId = Guid.Parse("00000000-0000-0000-0000-000000000002");

    [Fact]
    public void Slot_state_validates_identity_and_is_immutable()
    {
        Assert.Throws<ArgumentException>(() => new EquipmentSlotState(" "));
        Assert.Throws<ArgumentNullException>(() => new EquipmentSlotState(null!));
        Assert.Throws<ArgumentException>(() => new EquipmentSlotState("weapon", Guid.Empty));

        var empty = new EquipmentSlotState("weapon");
        Assert.Null(new EquipmentSlotState("weapon", null).ItemInstanceId);
        Assert.Throws<ArgumentException>(() => empty.Equip(Guid.Empty));
        var equipped = empty.Equip(ItemId);

        Assert.Null(empty.ItemInstanceId);
        Assert.Equal(ItemId, equipped.ItemInstanceId);
        Assert.Null(equipped.Unequip().ItemInstanceId);
        Assert.Throws<InvalidOperationException>(() => equipped.Equip(OtherItemId));
        Assert.Throws<InvalidOperationException>(() => empty.Unequip());
    }

    [Fact]
    public void Player_state_rejects_duplicate_slots_and_duplicate_equipped_items()
    {
        Assert.Throws<ArgumentException>(() => new PlayerState
        {
            EquipmentSlots = [new EquipmentSlotState("weapon"), new EquipmentSlotState("weapon")]
        });
        Assert.Throws<ArgumentException>(() => new PlayerState
        {
            EquipmentSlots = [
                new EquipmentSlotState("weapon", ItemId),
                new EquipmentSlotState("off-hand", ItemId)]
        });
    }

    [Fact]
    public void Equip_and_unequip_commit_through_the_game_state_boundary()
    {
        var state = CreateState([
            new EquipmentSlotState("weapon"),
            new EquipmentSlotState("off-hand")]);

        var equipped = EquipmentResolver.Equip(state, new EquipItemCommand("weapon", ItemId));

        Assert.Equal(ItemId, equipped.State.Player.EquipmentSlots[0].ItemInstanceId);
        var equipEvent = Assert.IsType<ItemEquippedEvent>(Assert.Single(equipped.Events));
        Assert.Equal("weapon", equipEvent.SlotId);
        Assert.Equal(ItemId, equipEvent.ItemInstanceId);

        var unequipped = EquipmentResolver.Unequip(
            equipped.State,
            new UnequipItemCommand("weapon"));

        Assert.Null(unequipped.State.Player.EquipmentSlots[0].ItemInstanceId);
        var unequipEvent = Assert.IsType<ItemUnequippedEvent>(Assert.Single(unequipped.Events));
        Assert.Equal(ItemId, unequipEvent.ItemInstanceId);
    }

    [Fact]
    public void Equipment_commands_validate_before_mutation()
    {
        var state = CreateState([new EquipmentSlotState("weapon")]);

        Assert.Throws<ArgumentException>(() => new EquipItemCommand("weapon", Guid.Empty));
        Assert.Throws<ArgumentException>(() => new EquipItemCommand(" ", ItemId));
        Assert.Throws<ArgumentException>(() => new UnequipItemCommand(" "));
        Assert.Throws<InvalidOperationException>(() => EquipmentResolver.Equip(
            state,
            new EquipItemCommand("missing", ItemId)));

        var equipped = EquipmentResolver.Equip(state, new EquipItemCommand("weapon", ItemId));
        Assert.Throws<InvalidOperationException>(() => EquipmentResolver.Equip(
            equipped.State,
            new EquipItemCommand("weapon", OtherItemId)));
        Assert.Throws<InvalidOperationException>(() => EquipmentResolver.Equip(
            equipped.State,
            new EquipItemCommand("off-hand", ItemId)));
        Assert.Throws<InvalidOperationException>(() => EquipmentResolver.Equip(
            CreateState([new EquipmentSlotState("weapon"), new EquipmentSlotState("off-hand", ItemId)]),
            new EquipItemCommand("weapon", ItemId)));
        Assert.Equal(OtherItemId, EquipmentResolver.Equip(
            CreateState([new EquipmentSlotState("weapon"), new EquipmentSlotState("off-hand")]),
            new EquipItemCommand("off-hand", OtherItemId)).State.Player.EquipmentSlots[1].ItemInstanceId);
        Assert.Throws<InvalidOperationException>(() => EquipmentResolver.Unequip(
            state,
            new UnequipItemCommand("weapon")));
        Assert.Throws<InvalidOperationException>(() => EquipmentResolver.Equip(
            state with { Player = state.Player with { Alive = false } },
            new EquipItemCommand("weapon", ItemId)));
        Assert.Null(state.Player.EquipmentSlots[0].ItemInstanceId);
    }

    [Fact]
    public void Equipment_replays_deterministically()
    {
        var state = CreateState([new EquipmentSlotState("weapon")]);
        var command = new EquipItemCommand("weapon", ItemId);

        var first = EquipmentResolver.Equip(state, command);
        var second = EquipmentResolver.Equip(state, command);

        Assert.Equal(JsonSerializer.Serialize(first.State), JsonSerializer.Serialize(second.State));
        Assert.Equal(first.Events, second.Events);
    }

    [Fact]
    public void Save_round_trip_preserves_slot_assignments_and_migrates_legacy_slot_names()
    {
        var state = CreateState([new EquipmentSlotState("weapon", ItemId), new EquipmentSlotState("off-hand")]);

        var roundTrip = SaveGameSerializer.Deserialize(SaveGameSerializer.Serialize(state));

        Assert.Equal(state.SaveVersion, roundTrip.SaveVersion);
        Assert.Equal(state.Player.EquipmentSlots, roundTrip.Player.EquipmentSlots);

        var legacy = JsonNode.Parse(SaveGameSerializer.Serialize(state))!.AsObject();
        legacy["saveVersion"] = 9;
        legacy["player"]!["equipmentSlots"] = new JsonArray("weapon");

        var migrated = SaveGameSerializer.Deserialize(legacy.ToJsonString());

        Assert.Equal(GameState.CurrentSaveVersion, migrated.SaveVersion);
        var legacySlot = Assert.Single(migrated.Player.EquipmentSlots);
        Assert.Equal("weapon", legacySlot.SlotId);
        Assert.Null(legacySlot.ItemInstanceId);
    }

    private static GameState CreateState(IReadOnlyList<EquipmentSlotState> slots) =>
        GameState.Create(1234) with
        {
            Player = new PlayerState
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000010"),
                EquipmentSlots = slots
            }
        };
}
