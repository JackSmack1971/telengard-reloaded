using Telengard.Core.Simulation;

namespace Telengard.Core.Items;

public sealed record ApplyItemCurseCommand : ICommand
{
    public ApplyItemCurseCommand(Guid itemId, string curse)
    {
        if (itemId == Guid.Empty)
        {
            throw new ArgumentException("Item id cannot be empty.", nameof(itemId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(curse);

        ItemId = itemId;
        Curse = curse;
    }

    public Guid ItemId { get; }
    public string Curse { get; }
}

public sealed record ItemCursedEvent(Guid ItemId) : IDomainEvent;

public sealed record ItemCurseResult
{
    public ItemCurseResult(ItemInstance item, IEnumerable<IDomainEvent>? events = null)
    {
        Item = item ?? throw new ArgumentNullException(nameof(item));
        Events = events?.ToArray() ?? Array.Empty<IDomainEvent>();
    }

    public ItemInstance Item { get; }
    public IReadOnlyList<IDomainEvent> Events { get; }
}

public static class ItemCurseResolver
{
    public static ItemCurseResult Apply(
        ItemInstance item,
        ApplyItemCurseCommand command)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(command);

        if (item.InstanceId != command.ItemId)
        {
            throw new InvalidOperationException("The curse command does not target the supplied item.");
        }

        var cursed = item.WithCurse(command.Curse);
        return new ItemCurseResult(
            cursed,
            [new ItemCursedEvent(cursed.InstanceId)]);
    }
}
