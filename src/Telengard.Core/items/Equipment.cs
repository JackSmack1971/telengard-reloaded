using Telengard.Core.Simulation;

namespace Telengard.Core.Items;

public sealed record EquipmentSlotState
{
    public EquipmentSlotState(string slotId, Guid? itemInstanceId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(slotId);
        if (itemInstanceId == Guid.Empty)
        {
            throw new ArgumentException("An item instance id cannot be empty.", nameof(itemInstanceId));
        }

        SlotId = slotId;
        ItemInstanceId = itemInstanceId;
    }

    public string SlotId { get; }
    public Guid? ItemInstanceId { get; }

    public EquipmentSlotState Equip(Guid itemInstanceId)
    {
        if (itemInstanceId == Guid.Empty)
        {
            throw new ArgumentException("Item instance id cannot be empty.", nameof(itemInstanceId));
        }

        if (ItemInstanceId.HasValue)
        {
            throw new InvalidOperationException("The equipment slot is already occupied.");
        }

        return new EquipmentSlotState(SlotId, itemInstanceId);
    }

    public EquipmentSlotState Unequip()
    {
        if (!ItemInstanceId.HasValue)
        {
            throw new InvalidOperationException("The equipment slot is already empty.");
        }

        return new EquipmentSlotState(SlotId);
    }
}

public sealed record EquipItemCommand : ICommand
{
    public EquipItemCommand(string slotId, Guid itemInstanceId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(slotId);
        if (itemInstanceId == Guid.Empty)
        {
            throw new ArgumentException("Item instance id cannot be empty.", nameof(itemInstanceId));
        }

        SlotId = slotId;
        ItemInstanceId = itemInstanceId;
    }

    public string SlotId { get; }
    public Guid ItemInstanceId { get; }
}

public sealed record UnequipItemCommand : ICommand
{
    public UnequipItemCommand(string slotId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(slotId);
        SlotId = slotId;
    }

    public string SlotId { get; }
}

public sealed record ItemEquippedEvent(string SlotId, Guid ItemInstanceId) : IDomainEvent;

public sealed record ItemUnequippedEvent(string SlotId, Guid ItemInstanceId) : IDomainEvent;

public sealed record EquipmentResult
{
    public EquipmentResult(PlayerState player, IEnumerable<IDomainEvent>? events = null)
    {
        Player = player ?? throw new ArgumentNullException(nameof(player));
        Events = events?.ToArray() ?? Array.Empty<IDomainEvent>();
    }

    public PlayerState Player { get; }
    public IReadOnlyList<IDomainEvent> Events { get; }
}

public static class EquipmentResolver
{
    public static CommandResult Equip(GameState state, EquipItemCommand command)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(command);
        if (!state.Player.Alive)
        {
            throw new InvalidOperationException("A dead player cannot equip an item.");
        }

        var result = Equip(state.Player, command);
        return new CommandResult(state with { Player = result.Player }, result.Events);
    }

    public static CommandResult Unequip(GameState state, UnequipItemCommand command)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(command);
        if (!state.Player.Alive)
        {
            throw new InvalidOperationException("A dead player cannot unequip an item.");
        }

        var result = Unequip(state.Player, command);
        return new CommandResult(state with { Player = result.Player }, result.Events);
    }

    public static EquipmentResult Equip(PlayerState player, EquipItemCommand command)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(command);

        var slotIndex = FindSlot(player, command.SlotId);
        var slot = player.EquipmentSlots[slotIndex];
        if (player.EquipmentSlots.Any(candidate => candidate.ItemInstanceId == command.ItemInstanceId))
        {
            throw new InvalidOperationException("The item is already equipped.");
        }

        var updatedSlots = player.EquipmentSlots.ToArray();
        updatedSlots[slotIndex] = slot.Equip(command.ItemInstanceId);
        var next = player with { EquipmentSlots = updatedSlots };
        return new EquipmentResult(next, [new ItemEquippedEvent(command.SlotId, command.ItemInstanceId)]);
    }

    public static EquipmentResult Unequip(PlayerState player, UnequipItemCommand command)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(command);

        var slotIndex = FindSlot(player, command.SlotId);
        var slot = player.EquipmentSlots[slotIndex];
        var itemInstanceId = slot.ItemInstanceId
            ?? throw new InvalidOperationException("The equipment slot is already empty.");
        var updatedSlots = player.EquipmentSlots.ToArray();
        updatedSlots[slotIndex] = slot.Unequip();
        var next = player with { EquipmentSlots = updatedSlots };
        return new EquipmentResult(next, [new ItemUnequippedEvent(command.SlotId, itemInstanceId)]);
    }

    private static int FindSlot(PlayerState player, string slotId)
    {
        var index = Array.FindIndex(
            player.EquipmentSlots.ToArray(),
            slot => string.Equals(slot.SlotId, slotId, StringComparison.Ordinal));
        if (index < 0)
        {
            throw new InvalidOperationException("The requested equipment slot is not configured.");
        }

        return index;
    }
}
