using System.Collections.ObjectModel;

namespace Telengard.Content;

public enum MonsterFamily
{
    Undead,
    Beasts,
    Demons,
    Humanoids,
    Constructs,
    Aberrations
}

public sealed record MonsterStats
{
    public MonsterStats(IReadOnlyDictionary<string, int>? values = null)
    {
        Values = Copy(values, nameof(values));
    }

    public IReadOnlyDictionary<string, int> Values { get; }

    public int this[string name] => Values[name];

    private static IReadOnlyDictionary<string, int> Copy(
        IReadOnlyDictionary<string, int>? values,
        string parameterName)
    {
        if (values is null) return new ReadOnlyDictionary<string, int>(new Dictionary<string, int>());

        var copy = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var pair in values)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(pair.Key);
            if (!copy.TryAdd(pair.Key, pair.Value))
            {
                throw new ArgumentException("Stat names must be unique.", parameterName);
            }
        }

        return new ReadOnlyDictionary<string, int>(copy);
    }
}

public sealed record MonsterSpawnRules
{
    public MonsterSpawnRules(IReadOnlyDictionary<string, string>? values = null)
    {
        Values = Copy(values, nameof(values));
    }

    public IReadOnlyDictionary<string, string> Values { get; }

    public string this[string name] => Values[name];

    private static IReadOnlyDictionary<string, string> Copy(
        IReadOnlyDictionary<string, string>? values,
        string parameterName)
    {
        if (values is null) return new ReadOnlyDictionary<string, string>(new Dictionary<string, string>());

        var copy = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var pair in values)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(pair.Key);
            ArgumentException.ThrowIfNullOrWhiteSpace(pair.Value);
            if (!copy.TryAdd(pair.Key, pair.Value))
            {
                throw new ArgumentException("Spawn-rule names must be unique.", parameterName);
            }
        }

        return new ReadOnlyDictionary<string, string>(copy);
    }
}

public sealed record MonsterDefinition
{
    public MonsterDefinition(
        string id,
        string displayName,
        MonsterFamily family,
        MonsterStats? baseStats = null,
        IEnumerable<string>? traits = null,
        IEnumerable<string>? resistances = null,
        IEnumerable<string>? vulnerabilities = null,
        IEnumerable<string>? actions = null,
        IEnumerable<string>? behaviors = null,
        MonsterSpawnRules? spawnRules = null,
        string? lootTable = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        if (!Enum.IsDefined(family)) throw new ArgumentOutOfRangeException(nameof(family), family, "Unknown monster family.");

        Id = id;
        DisplayName = displayName;
        Family = family;
        BaseStats = baseStats ?? new MonsterStats();
        Traits = CopyTags(traits, nameof(traits));
        Resistances = CopyTags(resistances, nameof(resistances));
        Vulnerabilities = CopyTags(vulnerabilities, nameof(vulnerabilities));
        Actions = CopyTags(actions, nameof(actions));
        Behaviors = CopyTags(behaviors, nameof(behaviors));
        SpawnRules = spawnRules ?? new MonsterSpawnRules();
        LootTable = string.IsNullOrWhiteSpace(lootTable) ? null : lootTable;
    }

    public string Id { get; }
    public string DisplayName { get; }
    public MonsterFamily Family { get; }
    public MonsterStats BaseStats { get; }
    public IReadOnlyList<string> Traits { get; }
    public IReadOnlyList<string> Resistances { get; }
    public IReadOnlyList<string> Vulnerabilities { get; }
    public IReadOnlyList<string> Actions { get; }
    public IReadOnlyList<string> Behaviors { get; }
    public MonsterSpawnRules SpawnRules { get; }
    public string? LootTable { get; }

    private static IReadOnlyList<string> CopyTags(IEnumerable<string>? tags, string parameterName)
    {
        if (tags is null) return Array.Empty<string>();

        var copy = new List<string>();
        foreach (var tag in tags)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(tag);
            if (copy.Contains(tag, StringComparer.Ordinal))
            {
                throw new ArgumentException("Tags must be unique.", parameterName);
            }

            copy.Add(tag);
        }

        return copy.ToArray();
    }
}
