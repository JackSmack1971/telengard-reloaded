namespace Telengard.Core.Presentation;

/// <summary>
/// Resolves stable presentation identities without placing resource paths in
/// authoritative simulation state or save DTOs.
/// </summary>
public sealed class PresentationAssetRegistry
{
    private readonly IReadOnlyDictionary<string, PresentationAssetEntry> _entries;

    public PresentationAssetRegistry(
        IEnumerable<PresentationAssetEntry> entries,
        string placeholderResource = "res://presentation/placeholders/missing_asset.tres")
    {
        ArgumentNullException.ThrowIfNull(entries);
        ArgumentException.ThrowIfNullOrWhiteSpace(placeholderResource);

        var map = new Dictionary<string, PresentationAssetEntry>(StringComparer.Ordinal);
        foreach (var entry in entries)
        {
            ArgumentNullException.ThrowIfNull(entry);
            if (!map.TryAdd(entry.Key, entry))
            {
                throw new ArgumentException($"Duplicate presentation asset key '{entry.Key}'.", nameof(entries));
            }
        }

        _entries = map;
        PlaceholderResource = placeholderResource;
    }

    public string PlaceholderResource { get; }

    public IReadOnlyList<PresentationAssetEntry> Entries =>
        _entries.Values.OrderBy(entry => entry.Key, StringComparer.Ordinal).ToArray();

    public string Resolve(string key, bool allowPlaceholder = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        if (!_entries.TryGetValue(key, out var entry))
        {
            if (allowPlaceholder) return PlaceholderResource;
            throw new KeyNotFoundException($"No presentation asset is registered for '{key}'.");
        }

        if (!string.IsNullOrWhiteSpace(entry.Resource)) return entry.Resource;
        if (allowPlaceholder) return PlaceholderResource;
        throw new InvalidOperationException($"Required presentation asset '{key}' has no resource mapping.");
    }

    public void Validate(bool allowPlaceholders = true)
    {
        var missing = _entries.Values
            .Where(entry => entry.Required && string.IsNullOrWhiteSpace(entry.Resource))
            .Select(entry => entry.Key)
            .OrderBy(key => key, StringComparer.Ordinal)
            .ToArray();

        if (missing.Length > 0 && !allowPlaceholders)
        {
            throw new InvalidOperationException(
                $"Required presentation assets are missing: {string.Join(", ", missing)}.");
        }
    }
}

public sealed record PresentationAssetEntry
{
    public PresentationAssetEntry(string key, string? resource = null, bool required = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        if (resource is not null) ArgumentException.ThrowIfNullOrWhiteSpace(resource);
        Key = key;
        Resource = resource;
        Required = required;
    }

    public string Key { get; }
    public string? Resource { get; }
    public bool Required { get; }
}
