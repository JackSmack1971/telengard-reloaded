using Telengard.Core.Simulation;

namespace Telengard.Core.Items;

public sealed record GenerateItemAffixesCommand : ICommand
{
    public GenerateItemAffixesCommand(Guid itemId, IEnumerable<string>? affixes = null)
    {
        if (itemId == Guid.Empty)
        {
            throw new ArgumentException("Item id cannot be empty.", nameof(itemId));
        }

        ItemId = itemId;
        Affixes = CopyAffixes(affixes);
    }

    public Guid ItemId { get; }
    public IReadOnlyList<string> Affixes { get; }

    private static IReadOnlyList<string> CopyAffixes(IEnumerable<string>? affixes)
    {
        if (affixes is null)
        {
            return Array.Empty<string>();
        }

        var copy = new List<string>();
        foreach (var affix in affixes)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(affix);
            if (!copy.Contains(affix, StringComparer.Ordinal))
            {
                copy.Add(affix);
            }
            else
            {
                throw new ArgumentException("Affixes must be unique.", nameof(affixes));
            }
        }

        return copy.ToArray();
    }
}

public sealed record ItemAffixesGeneratedEvent(Guid ItemId) : IDomainEvent;

public sealed record ItemAffixGenerationResult
{
    public ItemAffixGenerationResult(ItemInstance item, IEnumerable<IDomainEvent>? events = null)
    {
        Item = item ?? throw new ArgumentNullException(nameof(item));
        Events = events?.ToArray() ?? Array.Empty<IDomainEvent>();
    }

    public ItemInstance Item { get; }
    public IReadOnlyList<IDomainEvent> Events { get; }
}

public static class ItemAffixGenerationResolver
{
    public static ItemAffixGenerationResult Generate(
        ItemInstance item,
        GenerateItemAffixesCommand command)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(command);

        if (item.InstanceId != command.ItemId)
        {
            throw new InvalidOperationException("The affix-generation command does not target the supplied item.");
        }

        var generated = item.WithGeneratedAffixes(command.Affixes);
        return new ItemAffixGenerationResult(
            generated,
            [new ItemAffixesGeneratedEvent(generated.InstanceId)]);
    }
}
