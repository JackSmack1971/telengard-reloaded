using Telengard.Core.Rng;
using Telengard.Core.Items;

namespace Telengard.Content;

public static class ItemAffixEngine
{
    public static ItemAffixGenerationResult Generate(
        ItemInstance item,
        ItemDefinition definition,
        int affixCount,
        DeterministicRngStream rng)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(definition);

        if (!string.Equals(item.DefinitionId, definition.Id, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("The item definition does not match the item instance.");
        }

        var affixes = Select(definition, affixCount, rng);
        return ItemAffixGenerationResolver.Generate(
            item,
            new GenerateItemAffixesCommand(item.InstanceId, affixes));
    }

    public static ItemAffixGenerationResult Generate(
        ItemInstance item,
        ItemDefinition definition,
        int affixCount,
        long worldSeed,
        string contentVersion)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(definition);

        if (!string.Equals(item.DefinitionId, definition.Id, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("The item definition does not match the item instance.");
        }

        var affixes = Select(definition, affixCount, worldSeed, contentVersion, item.InstanceId);
        return ItemAffixGenerationResolver.Generate(
            item,
            new GenerateItemAffixesCommand(item.InstanceId, affixes));
    }

    public static IReadOnlyList<string> Select(
        ItemDefinition definition,
        int affixCount,
        DeterministicRngStream rng)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentOutOfRangeException.ThrowIfNegative(affixCount);
        ArgumentNullException.ThrowIfNull(rng);

        if (affixCount > definition.AffixPool.Count)
        {
            throw new ArgumentOutOfRangeException(
                nameof(affixCount),
                affixCount,
                "The requested affix count exceeds the definition pool.");
        }

        var remaining = definition.AffixPool.ToList();
        var selected = new List<string>(affixCount);
        for (var index = 0; index < affixCount; index++)
        {
            var selectedIndex = rng.NextInt(0, remaining.Count);
            selected.Add(remaining[selectedIndex]);
            remaining.RemoveAt(selectedIndex);
        }

        return selected.ToArray();
    }

    public static IReadOnlyList<string> Select(
        ItemDefinition definition,
        int affixCount,
        long worldSeed,
        string contentVersion,
        Guid itemInstanceId)
    {
        ArgumentNullException.ThrowIfNull(definition);
        if (itemInstanceId == Guid.Empty)
        {
            throw new ArgumentException("Item instance id cannot be empty.", nameof(itemInstanceId));
        }

        var rng = new DeterministicRng(worldSeed, contentVersion).CreateStream(
            "item-affixes",
            $"definition:{definition.Id}",
            $"instance:{itemInstanceId}");
        return Select(definition, affixCount, rng);
    }
}
