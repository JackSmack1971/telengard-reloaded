using Telengard.Core.Combat;

namespace Telengard.Content;

public sealed record EncounterTableEntry
{
    public EncounterTableEntry(string monsterId, long weight, int level, int currentHitPoints)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(monsterId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(weight);
        ArgumentOutOfRangeException.ThrowIfLessThan(level, 1);
        ArgumentOutOfRangeException.ThrowIfNegative(currentHitPoints);

        MonsterId = monsterId;
        Weight = weight;
        Level = level;
        CurrentHitPoints = currentHitPoints;
    }

    public string MonsterId { get; }
    public long Weight { get; }
    public int Level { get; }
    public int CurrentHitPoints { get; }
}

public sealed record EncounterTable
{
    public EncounterTable(
        string id,
        int floorMin,
        int floorMax,
        IEnumerable<EncounterTableEntry> entries)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentOutOfRangeException.ThrowIfLessThan(floorMin, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(floorMax, 5);
        if (floorMin > floorMax)
        {
            throw new ArgumentException("An encounter table minimum floor cannot exceed its maximum floor.", nameof(floorMin));
        }

        ArgumentNullException.ThrowIfNull(entries);
        var copy = entries.ToArray();
        if (copy.Length == 0)
        {
            throw new ArgumentException("An encounter table must contain at least one entry.", nameof(entries));
        }

        if (copy.Any(entry => entry is null))
        {
            throw new ArgumentException("Encounter table entries cannot contain null values.", nameof(entries));
        }

        if (copy.Select(entry => entry.MonsterId).Distinct(StringComparer.Ordinal).Count() != copy.Length)
        {
            throw new ArgumentException("Encounter table monster ids must be unique.", nameof(entries));
        }

        _ = copy.Aggregate(0L, (total, entry) => checked(total + entry.Weight));
        Id = id;
        FloorMin = floorMin;
        FloorMax = floorMax;
        Entries = Array.AsReadOnly(copy);
    }

    public string Id { get; }
    public int FloorMin { get; }
    public int FloorMax { get; }
    public IReadOnlyList<EncounterTableEntry> Entries { get; }

    public bool CoversFloor(int floor) => floor >= FloorMin && floor <= FloorMax;

    public EncounterTriggerConfiguration ToTriggerConfiguration(double triggerChance)
    {
        return new EncounterTriggerConfiguration(
            triggerChance,
            Entries.Select(entry => new EncounterSpawnOption(
                entry.MonsterId,
                entry.Level,
                entry.CurrentHitPoints,
                entry.Weight)));
    }
}
