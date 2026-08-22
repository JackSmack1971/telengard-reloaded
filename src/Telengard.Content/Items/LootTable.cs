using Telengard.Core.Rng;
using Telengard.Core.Simulation;

namespace Telengard.Content;

public sealed record LootTableEntry
{
    public LootTableEntry(string itemId, long weight)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(itemId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(weight);

        ItemId = itemId;
        Weight = weight;
    }

    public string ItemId { get; }
    public long Weight { get; }
}

public sealed record LootTable
{
    public LootTable(string id, IEnumerable<LootTableEntry> entries)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentNullException.ThrowIfNull(entries);

        var copy = entries.ToArray();
        if (copy.Length == 0)
        {
            throw new ArgumentException("A loot table must contain at least one entry.", nameof(entries));
        }

        if (copy.Any(entry => entry is null))
        {
            throw new ArgumentException("Loot table entries cannot contain null values.", nameof(entries));
        }

        if (copy.Select(entry => entry.ItemId).Distinct(StringComparer.Ordinal).Count() != copy.Length)
        {
            throw new ArgumentException("Loot table item ids must be unique.", nameof(entries));
        }

        Id = id;
        Entries = Array.AsReadOnly(copy);
    }

    public string Id { get; }
    public IReadOnlyList<LootTableEntry> Entries { get; }
}

public static class LootTableEngine
{
    public static string Select(LootTable table, DeterministicRngStream rng)
    {
        ArgumentNullException.ThrowIfNull(table);
        ArgumentNullException.ThrowIfNull(rng);

        var totalWeight = 0L;
        foreach (var entry in table.Entries)
        {
            totalWeight = checked(totalWeight + entry.Weight);
        }

        var roll = rng.NextLong(0, totalWeight);
        var cumulativeWeight = 0L;
        foreach (var entry in table.Entries)
        {
            cumulativeWeight += entry.Weight;
            if (roll < cumulativeWeight)
            {
                return entry.ItemId;
            }
        }

        throw new InvalidOperationException("The loot table roll did not resolve.");
    }

    public static string Select(
        LootTable table,
        long worldSeed,
        string contentVersion,
        DungeonPosition position,
        Guid? expeditionId,
        int acquisitionCount)
    {
        ArgumentNullException.ThrowIfNull(table);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentVersion);
        ArgumentNullException.ThrowIfNull(position);
        ArgumentOutOfRangeException.ThrowIfNegative(acquisitionCount);

        var rng = new DeterministicRng(worldSeed, contentVersion).CreateStream(
            "loot-table",
            $"table:{table.Id}",
            $"expedition:{expeditionId?.ToString("D") ?? "none"}",
            $"floor:{position.Floor}",
            $"x:{position.X}",
            $"y:{position.Y}",
            $"acquisition:{acquisitionCount}");
        return Select(table, rng);
    }
}
