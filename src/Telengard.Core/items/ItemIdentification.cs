using Telengard.Core.Simulation;

namespace Telengard.Core.Items;

public sealed record IdentifyItemCommand : ICommand
{
    public IdentifyItemCommand(Guid itemId)
    {
        if (itemId == Guid.Empty)
        {
            throw new ArgumentException("Item id cannot be empty.", nameof(itemId));
        }

        ItemId = itemId;
    }

    public Guid ItemId { get; }
}

public sealed record ItemIdentifiedEvent(Guid ItemId) : IDomainEvent;

public sealed record ItemIdentificationResult
{
    public ItemIdentificationResult(ItemInstance item, IEnumerable<IDomainEvent>? events = null)
    {
        Item = item ?? throw new ArgumentNullException(nameof(item));
        Events = events?.ToArray() ?? Array.Empty<IDomainEvent>();
    }

    public ItemInstance Item { get; }
    public IReadOnlyList<IDomainEvent> Events { get; }
}

public static class ItemIdentificationResolver
{
    public static ItemIdentificationResult Identify(
        ItemInstance item,
        IdentifyItemCommand command)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(command);

        if (item.InstanceId != command.ItemId)
        {
            throw new InvalidOperationException("The identification command does not target the supplied item.");
        }

        if (item.IdentifiedState)
        {
            return new ItemIdentificationResult(item);
        }

        var identified = item.Identify();
        return new ItemIdentificationResult(
            identified,
            [new ItemIdentifiedEvent(identified.InstanceId)]);
    }
}
