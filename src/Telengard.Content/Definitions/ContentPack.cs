using System.Collections.ObjectModel;

namespace Telengard.Content;

public sealed class ContentCatalog<T>
{
    private readonly IReadOnlyDictionary<string, T> _definitions;

    internal ContentCatalog(IEnumerable<T> definitions, Func<T, string> idSelector)
    {
        ArgumentNullException.ThrowIfNull(definitions);
        ArgumentNullException.ThrowIfNull(idSelector);

        var values = new Dictionary<string, T>(StringComparer.Ordinal);
        foreach (var definition in definitions)
        {
            ArgumentNullException.ThrowIfNull(definition);
            var id = idSelector(definition);
            ArgumentException.ThrowIfNullOrWhiteSpace(id);
            if (!values.TryAdd(id, definition))
            {
                throw new InvalidDataException($"Duplicate content id '{id}'.");
            }
        }

        _definitions = new ReadOnlyDictionary<string, T>(values);
    }

    public IReadOnlyDictionary<string, T> Definitions => _definitions;

    public int Count => _definitions.Count;

    public bool TryGet(string id, out T definition)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        return _definitions.TryGetValue(id, out definition!);
    }

    public T GetRequired(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        return _definitions.TryGetValue(id, out var definition)
            ? definition
            : throw new KeyNotFoundException($"Content id '{id}' was not found.");
    }
}

public sealed class ContentPack
{
    internal ContentPack(
        string contentVersion,
        IEnumerable<MonsterDefinition>? monsters = null,
        IEnumerable<ItemDefinition>? items = null,
        IEnumerable<SpellDefinition>? spells = null,
        IEnumerable<FeatureDefinition>? features = null,
        IEnumerable<TalentDefinition>? talents = null,
        IEnumerable<LootTable>? lootTables = null,
        IEnumerable<DungeonBandDefinition>? bands = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(contentVersion);

        ContentVersion = contentVersion;
        Monsters = new ContentCatalog<MonsterDefinition>(monsters ?? [], definition => definition.Id);
        Items = new ContentCatalog<ItemDefinition>(items ?? [], definition => definition.Id);
        Spells = new ContentCatalog<SpellDefinition>(spells ?? [], definition => definition.Id);
        Features = new ContentCatalog<FeatureDefinition>(features ?? [], definition => definition.Id);
        Talents = new ContentCatalog<TalentDefinition>(talents ?? [], definition => definition.Id);
        LootTables = new ContentCatalog<LootTable>(lootTables ?? [], table => table.Id);
        Bands = new ContentCatalog<DungeonBandDefinition>(bands ?? [], band => band.Id);
    }

    public string ContentVersion { get; }
    public ContentCatalog<MonsterDefinition> Monsters { get; }
    public ContentCatalog<ItemDefinition> Items { get; }
    public ContentCatalog<SpellDefinition> Spells { get; }
    public ContentCatalog<FeatureDefinition> Features { get; }
    public ContentCatalog<TalentDefinition> Talents { get; }
    public ContentCatalog<LootTable> LootTables { get; }
    public ContentCatalog<DungeonBandDefinition> Bands { get; }

    public bool TryGetBandForFloor(int floor, out DungeonBandDefinition band)
    {
        foreach (var candidate in Bands.Definitions.Values)
        {
            if (candidate.CoversFloor(floor))
            {
                band = candidate;
                return true;
            }
        }

        band = null!;
        return false;
    }
}
