using System.Collections.ObjectModel;

namespace Telengard.Content;

public sealed record ItemProperties
{
    public ItemProperties(IReadOnlyDictionary<string, string>? values = null)
    {
        Values = Copy(values, nameof(values), "Property names must be unique.");
    }

    public IReadOnlyDictionary<string, string> Values { get; }

    public string this[string name] => Values[name];

    internal static IReadOnlyDictionary<string, string> Copy(
        IReadOnlyDictionary<string, string>? values,
        string parameterName,
        string duplicateMessage)
    {
        if (values is null)
        {
            return new ReadOnlyDictionary<string, string>(
                new Dictionary<string, string>(StringComparer.Ordinal));
        }

        var copy = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var pair in values)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(pair.Key);
            ArgumentException.ThrowIfNullOrWhiteSpace(pair.Value);
            if (!copy.TryAdd(pair.Key, pair.Value))
            {
                throw new ArgumentException(duplicateMessage, parameterName);
            }
        }

        return new ReadOnlyDictionary<string, string>(copy);
    }
}

public sealed record ItemRarityRules
{
    public ItemRarityRules(IReadOnlyDictionary<string, string>? values = null)
    {
        Values = ItemProperties.Copy(values, nameof(values), "Rarity-rule names must be unique.");
    }

    public IReadOnlyDictionary<string, string> Values { get; }

    public string this[string name] => Values[name];
}

public sealed record ItemDepthRules
{
    public ItemDepthRules(IReadOnlyDictionary<string, string>? values = null)
    {
        Values = ItemProperties.Copy(values, nameof(values), "Depth-rule names must be unique.");
    }

    public IReadOnlyDictionary<string, string> Values { get; }

    public string this[string name] => Values[name];
}

public sealed record ItemDefinition
{
    public ItemDefinition(
        string id,
        string displayName,
        string category,
        ItemProperties? baseProperties = null,
        IEnumerable<string>? affixPool = null,
        IEnumerable<string>? cursePool = null,
        ItemRarityRules? rarityRules = null,
        ItemDepthRules? depthRules = null,
        string? unidentifiedName = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentException.ThrowIfNullOrWhiteSpace(category);

        Id = id;
        DisplayName = displayName;
        Category = category;
        BaseProperties = baseProperties ?? new ItemProperties();
        AffixPool = CopyTags(affixPool, nameof(affixPool));
        CursePool = CopyTags(cursePool, nameof(cursePool));
        RarityRules = rarityRules ?? new ItemRarityRules();
        DepthRules = depthRules ?? new ItemDepthRules();
        UnidentifiedName = string.IsNullOrWhiteSpace(unidentifiedName) ? null : unidentifiedName;
    }

    public string Id { get; }
    public string DisplayName { get; }
    public string Category { get; }
    public ItemProperties BaseProperties { get; }
    public IReadOnlyList<string> AffixPool { get; }
    public IReadOnlyList<string> CursePool { get; }
    public ItemRarityRules RarityRules { get; }
    public ItemDepthRules DepthRules { get; }
    public string? UnidentifiedName { get; }

    private static IReadOnlyList<string> CopyTags(IEnumerable<string>? tags, string parameterName)
    {
        if (tags is null) return Array.Empty<string>();

        var copy = new List<string>();
        foreach (var tag in tags)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(tag);
            if (copy.Contains(tag, StringComparer.Ordinal))
            {
                throw new ArgumentException("Values must be unique.", parameterName);
            }

            copy.Add(tag);
        }

        return copy.ToArray();
    }
}
