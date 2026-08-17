using Telengard.Core.Items;
using Telengard.Core.Rng;

namespace Telengard.Content;

public static class ItemCurseEngine
{
    public static ItemCurseResult Generate(
        ItemInstance item,
        ItemDefinition definition,
        DeterministicRngStream rng)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(definition);

        if (!string.Equals(item.DefinitionId, definition.Id, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("The item definition does not match the item instance.");
        }

        return ItemCurseResolver.Apply(
            item,
            new ApplyItemCurseCommand(item.InstanceId, Select(definition, rng)));
    }

    public static ItemCurseResult Generate(
        ItemInstance item,
        ItemDefinition definition,
        long worldSeed,
        string contentVersion)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(definition);

        if (!string.Equals(item.DefinitionId, definition.Id, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("The item definition does not match the item instance.");
        }

        var curse = Select(definition, worldSeed, contentVersion, item.InstanceId);
        return ItemCurseResolver.Apply(
            item,
            new ApplyItemCurseCommand(item.InstanceId, curse));
    }

    public static string Select(
        ItemDefinition definition,
        DeterministicRngStream rng)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(rng);

        if (definition.CursePool.Count == 0)
        {
            throw new InvalidOperationException("The item definition has no curses to select.");
        }

        return definition.CursePool[rng.NextInt(0, definition.CursePool.Count)];
    }

    public static string Select(
        ItemDefinition definition,
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
            "item-curses",
            $"definition:{definition.Id}",
            $"instance:{itemInstanceId}");
        return Select(definition, rng);
    }
}
