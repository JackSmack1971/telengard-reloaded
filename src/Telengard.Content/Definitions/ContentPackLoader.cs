using System.Text.Json;
using System.Collections.ObjectModel;

namespace Telengard.Content;

public static class ContentPackLoader
{
    public static ContentPack Load(string contentRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(contentRoot);
        if (!Directory.Exists(contentRoot))
        {
            throw new DirectoryNotFoundException($"Content root '{contentRoot}' was not found.");
        }

        var contentVersion = ReadManifest(contentRoot);
        var monsters = LoadDefinitions(
            contentRoot,
            "monsters",
            ParseMonster);
        var items = LoadDefinitions(
            contentRoot,
            "items",
            ParseItem);
        var spells = LoadDefinitions(
            contentRoot,
            "spells",
            ParseSpell);
        var features = LoadDefinitions(
            contentRoot,
            "features",
            ParseFeature);
        var talents = LoadDefinitions(
            contentRoot,
            "talents",
            ParseTalent);
        var lootTables = LoadDefinitions(
            contentRoot,
            "loot_tables",
            ParseLootTable);
        var bands = LoadDefinitions(
            contentRoot,
            "bands",
            ParseDungeonBand);
        var encounterTables = LoadDefinitions(
            contentRoot,
            "encounter_tables",
            ParseEncounterTable);

        var pack = new ContentPack(contentVersion, monsters, items, spells, features, talents, lootTables, bands, encounterTables);
        ValidateReferences(pack, Directory.Exists(Path.Combine(contentRoot, "encounter_tables")));
        return pack;
    }

    private static string ReadManifest(string contentRoot)
    {
        var manifestPath = Path.Combine(contentRoot, "manifest.json");
        if (!File.Exists(manifestPath))
        {
            throw new InvalidDataException($"Content manifest '{manifestPath}' was not found.");
        }

        try
        {
            using var document = JsonDocument.Parse(
                File.ReadAllText(manifestPath),
                new JsonDocumentOptions
                {
                    AllowTrailingCommas = true,
                    CommentHandling = JsonCommentHandling.Skip
                });
            if (!TryGetProperty(document.RootElement, "contentVersion", out var version))
            {
                throw new InvalidDataException("Content manifest must define contentVersion.");
            }

            var contentVersion = version.GetString();
            if (string.IsNullOrWhiteSpace(contentVersion))
            {
                throw new InvalidDataException("Content manifest contentVersion must not be blank.");
            }

            return contentVersion;
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException($"Content manifest '{manifestPath}' is invalid JSON.", exception);
        }
        catch (InvalidOperationException exception)
        {
            throw new InvalidDataException($"Content manifest '{manifestPath}' is invalid.", exception);
        }
    }

    private static T[] LoadDefinitions<T>(
        string contentRoot,
        string directoryName,
        Func<string, T> deserialize)
    {
        var directory = Path.Combine(contentRoot, directoryName);
        if (!Directory.Exists(directory))
        {
            return [];
        }

        return Directory.EnumerateFiles(directory, "*.json", SearchOption.TopDirectoryOnly)
            .Order(StringComparer.Ordinal)
            .Select(path => LoadDefinition(path, deserialize))
            .ToArray();
    }

    private static T LoadDefinition<T>(string path, Func<string, T> deserialize)
    {
        try
        {
            return deserialize(File.ReadAllText(path));
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException($"Content definition '{path}' is invalid JSON.", exception);
        }
        catch (ArgumentException exception)
        {
            throw new InvalidDataException($"Content definition '{path}' is invalid.", exception);
        }
        catch (InvalidOperationException exception)
        {
            throw new InvalidDataException($"Content definition '{path}' is invalid.", exception);
        }
        catch (FormatException exception)
        {
            throw new InvalidDataException($"Content definition '{path}' is invalid.", exception);
        }
        catch (OverflowException exception)
        {
            throw new InvalidDataException($"Content definition '{path}' is invalid.", exception);
        }
    }

    private static MonsterDefinition ParseMonster(string json)
    {
        var root = ParseRoot(json);
        return new MonsterDefinition(
            RequiredString(root, "id"),
            RequiredString(root, "displayName"),
            RequiredEnum<MonsterFamily>(root, "family"),
            new MonsterStats(IntMap(root, "baseStats")),
            StringArray(root, "traits"),
            StringArray(root, "resistances"),
            StringArray(root, "vulnerabilities"),
            StringArray(root, "actions"),
            StringArray(root, "behaviors"),
            new MonsterSpawnRules(StringMap(root, "spawnRules")),
            OptionalString(root, "lootTable"));
    }

    private static ItemDefinition ParseItem(string json)
    {
        var root = ParseRoot(json);
        return new ItemDefinition(
            RequiredString(root, "id"),
            RequiredString(root, "displayName"),
            RequiredString(root, "category"),
            new ItemProperties(StringMap(root, "baseProperties")),
            StringArray(root, "affixPool"),
            StringArray(root, "cursePool"),
            new ItemRarityRules(StringMap(root, "rarityRules")),
            new ItemDepthRules(StringMap(root, "depthRules")),
            OptionalString(root, "unidentifiedName"));
    }

    private static SpellDefinition ParseSpell(string json)
    {
        var root = ParseRoot(json);
        return new SpellDefinition(
            RequiredString(root, "id"),
            RequiredString(root, "name"),
            RequiredString(root, "initialDescription"),
            StringArray(root, "discoveredDescriptions"),
            RequiredInt(root, "cost"),
            RequiredString(root, "targetingRule"),
            StringArray(root, "effects"),
            StringArray(root, "interactions"));
    }

    private static FeatureDefinition ParseFeature(string json)
    {
        var root = ParseRoot(json);
        return new FeatureDefinition(
            RequiredString(root, "id"),
            RequiredEnum<FeatureType>(root, "type"),
            RequiredString(root, "presentationKey"),
            StringArray(root, "interactionOptions"),
            FeatureOutcomes(root, "outcomeTable"),
            StringMap(root, "hintRules"),
            OptionalString(root, "knowledgeCategory"));
    }

    private static TalentDefinition ParseTalent(string json)
    {
        var root = ParseRoot(json);
        return new TalentDefinition(
            RequiredString(root, "id"),
            RequiredString(root, "constellation"),
            StringArray(root, "prerequisites"),
            StringArray(root, "effects"),
            RequiredInt(root, "cost"));
    }

    private static LootTable ParseLootTable(string json)
    {
        var root = ParseRoot(json);
        var entries = RequiredArray(root, "entries")
            .EnumerateArray()
            .Select(entry => new LootTableEntry(
                RequiredString(entry, "itemId"),
                RequiredLong(entry, "weight")))
            .ToArray();
        return new LootTable(RequiredString(root, "id"), entries);
    }

    private static DungeonBandDefinition ParseDungeonBand(string json)
    {
        var root = ParseRoot(json);
        return new DungeonBandDefinition(
            RequiredString(root, "id"),
            RequiredString(root, "displayName"),
            RequiredInt(root, "floorMin"),
            RequiredInt(root, "floorMax"),
            RequiredString(root, "generationProfile"),
            StringArray(root, "monsterFamilies"),
            IntMap(root, "featureWeights"),
            StringArray(root, "hazards"),
            StringArray(root, "ambientRules"),
            OptionalString(root, "encounterEcologyId"),
            OptionalString(root, "lootProfile"),
            RequiredString(root, "visualTheme"),
            RequiredString(root, "audioTheme"));
    }

    private static EncounterTable ParseEncounterTable(string json)
    {
        var root = ParseRoot(json);
        var entries = RequiredArray(root, "entries")
            .EnumerateArray()
            .Select(entry => new EncounterTableEntry(
                RequiredString(entry, "monsterId"),
                RequiredLong(entry, "weight"),
                RequiredInt(entry, "level"),
                RequiredInt(entry, "currentHitPoints")))
            .ToArray();
        return new EncounterTable(
            RequiredString(root, "id"),
            RequiredInt(root, "floorMin"),
            RequiredInt(root, "floorMax"),
            entries);
    }

    private static IReadOnlyList<FeatureOutcome> FeatureOutcomes(JsonElement root, string propertyName)
    {
        if (!TryGetProperty(root, propertyName, out var value) || value.ValueKind == JsonValueKind.Null)
        {
            return [];
        }

        if (value.ValueKind != JsonValueKind.Array)
        {
            throw new JsonException($"'{propertyName}' must be an array.");
        }

        return value.EnumerateArray()
            .Select(ParseFeatureOutcome)
            .ToArray();
    }

    private static FeatureOutcome ParseFeatureOutcome(JsonElement root)
    {
        return new FeatureOutcome(
            StringArray(root, "conditions"),
            OptionalInt(root, "weight"),
            StringArray(root, "effects"),
            StringArray(root, "observations"));
    }

    private static JsonElement ParseRoot(string json)
    {
        using var document = JsonDocument.Parse(
            json,
            new JsonDocumentOptions
            {
                AllowTrailingCommas = true,
                CommentHandling = JsonCommentHandling.Skip
            });
        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            throw new JsonException("A content definition must be a JSON object.");
        }

        return document.RootElement.Clone();
    }

    private static string RequiredString(JsonElement root, string propertyName)
    {
        if (!TryGetProperty(root, propertyName, out var value) || value.ValueKind != JsonValueKind.String)
        {
            throw new JsonException($"Content definition requires string property '{propertyName}'.");
        }

        var result = value.GetString();
        return string.IsNullOrWhiteSpace(result)
            ? throw new JsonException($"Content definition property '{propertyName}' must not be blank.")
            : result;
    }

    private static string? OptionalString(JsonElement root, string propertyName)
    {
        if (!TryGetProperty(root, propertyName, out var value) || value.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        return value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : throw new JsonException($"Content definition property '{propertyName}' must be a string.");
    }

    private static int RequiredInt(JsonElement root, string propertyName) =>
        RequiredNumber(root, propertyName, value => value.GetInt32());

    private static long RequiredLong(JsonElement root, string propertyName) =>
        RequiredNumber(root, propertyName, value => value.GetInt64());

    private static int OptionalInt(JsonElement root, string propertyName) =>
        !TryGetProperty(root, propertyName, out var value) || value.ValueKind == JsonValueKind.Null
            ? 0
            : value.ValueKind == JsonValueKind.Number
                ? value.GetInt32()
                : throw new JsonException($"Content definition property '{propertyName}' must be a number.");

    private static T RequiredNumber<T>(
        JsonElement root,
        string propertyName,
        Func<JsonElement, T> read)
    {
        if (!TryGetProperty(root, propertyName, out var value) || value.ValueKind != JsonValueKind.Number)
        {
            throw new JsonException($"Content definition requires numeric property '{propertyName}'.");
        }

        return read(value);
    }

    private static T RequiredEnum<T>(JsonElement root, string propertyName)
        where T : struct, Enum
    {
        var value = RequiredString(root, propertyName);
        var name = Enum.GetNames<T>().FirstOrDefault(
            candidate => string.Equals(candidate, value, StringComparison.OrdinalIgnoreCase));
        return name is not null
            ? Enum.Parse<T>(name)
            : throw new JsonException($"Content definition property '{propertyName}' has an unknown value '{value}'.");
    }

    private static IReadOnlyList<string> StringArray(JsonElement root, string propertyName)
    {
        if (!TryGetProperty(root, propertyName, out var value) || value.ValueKind == JsonValueKind.Null)
        {
            return [];
        }

        if (value.ValueKind != JsonValueKind.Array)
        {
            throw new JsonException($"Content definition property '{propertyName}' must be an array.");
        }

        return value.EnumerateArray()
            .Select(item => item.ValueKind == JsonValueKind.String
                ? item.GetString()!
                : throw new JsonException($"Content definition property '{propertyName}' must contain strings."))
            .ToArray();
    }

    private static IReadOnlyDictionary<string, string> StringMap(JsonElement root, string propertyName)
    {
        if (!TryGetProperty(root, propertyName, out var value) || value.ValueKind == JsonValueKind.Null)
        {
            return new Dictionary<string, string>(StringComparer.Ordinal);
        }

        if (value.ValueKind != JsonValueKind.Object)
        {
            throw new JsonException($"Content definition property '{propertyName}' must be an object.");
        }

        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var property in value.EnumerateObject())
        {
            if (property.Value.ValueKind != JsonValueKind.String)
            {
                throw new JsonException($"Content definition map '{propertyName}' must contain strings.");
            }

            result.Add(property.Name, property.Value.GetString()!);
        }

        return new ReadOnlyDictionary<string, string>(result);
    }

    private static IReadOnlyDictionary<string, int> IntMap(JsonElement root, string propertyName)
    {
        if (!TryGetProperty(root, propertyName, out var value) || value.ValueKind == JsonValueKind.Null)
        {
            return new Dictionary<string, int>(StringComparer.Ordinal);
        }

        if (value.ValueKind != JsonValueKind.Object)
        {
            throw new JsonException($"Content definition property '{propertyName}' must be an object.");
        }

        var result = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var property in value.EnumerateObject())
        {
            if (property.Value.ValueKind != JsonValueKind.Number)
            {
                throw new JsonException($"Content definition map '{propertyName}' must contain numbers.");
            }

            result.Add(property.Name, property.Value.GetInt32());
        }

        return new ReadOnlyDictionary<string, int>(result);
    }

    private static JsonElement RequiredArray(JsonElement root, string propertyName)
    {
        if (!TryGetProperty(root, propertyName, out var value) || value.ValueKind != JsonValueKind.Array)
        {
            throw new JsonException($"Content definition requires array property '{propertyName}'.");
        }

        return value;
    }

    private static bool TryGetProperty(JsonElement root, string propertyName, out JsonElement value)
    {
        var normalizedName = NormalizePropertyName(propertyName);
        var found = false;
        value = default;
        foreach (var property in root.EnumerateObject())
        {
            if (string.Equals(NormalizePropertyName(property.Name), normalizedName, StringComparison.OrdinalIgnoreCase))
            {
                if (found)
                {
                    throw new JsonException($"Content definition contains duplicate property '{propertyName}'.");
                }

                value = property.Value;
                found = true;
            }
        }

        return found;
    }

    private static string NormalizePropertyName(string propertyName) =>
        propertyName.Replace("_", string.Empty, StringComparison.Ordinal);

    private static void ValidateReferences(ContentPack pack, bool hasEncounterTableCatalog)
    {
        ValidateBandRanges(pack);
        ValidateEncounterTableRanges(pack);

        foreach (var table in pack.EncounterTables.Definitions.Values)
        {
            foreach (var entry in table.Entries)
            {
                if (!pack.Monsters.TryGet(entry.MonsterId, out _))
                {
                    throw new InvalidDataException(
                        $"Encounter table '{table.Id}' references missing monster '{entry.MonsterId}'.");
                }
            }
        }

        foreach (var band in pack.Bands.Definitions.Values)
        {
            if (hasEncounterTableCatalog
                && band.EncounterEcologyId is not null
                && !pack.EncounterTables.TryGet(band.EncounterEcologyId, out _))
            {
                throw new InvalidDataException(
                    $"Dungeon band '{band.Id}' references missing encounter table '{band.EncounterEcologyId}'.");
            }

            if (hasEncounterTableCatalog
                && band.EncounterEcologyId is not null
                && pack.EncounterTables.TryGet(band.EncounterEcologyId, out var resolvedTable)
                && (resolvedTable.FloorMin > band.FloorMin || resolvedTable.FloorMax < band.FloorMax))
            {
                throw new InvalidDataException(
                    $"Encounter table '{resolvedTable.Id}' does not cover dungeon band '{band.Id}'.");
            }
        }

        foreach (var table in pack.LootTables.Definitions.Values)
        {
            foreach (var entry in table.Entries)
            {
                if (!pack.Items.TryGet(entry.ItemId, out _))
                {
                    throw new InvalidDataException(
                        $"Loot table '{table.Id}' references missing item '{entry.ItemId}'.");
                }
            }
        }

        foreach (var monster in pack.Monsters.Definitions.Values)
        {
            if (monster.LootTable is not null && !pack.LootTables.TryGet(monster.LootTable, out _))
            {
                throw new InvalidDataException(
                    $"Monster '{monster.Id}' references missing loot table '{monster.LootTable}'.");
            }
        }
    }

    private static void ValidateBandRanges(ContentPack pack)
    {
        var bands = pack.Bands.Definitions.Values
            .OrderBy(band => band.FloorMin)
            .ThenBy(band => band.FloorMax)
            .ThenBy(band => band.Id, StringComparer.Ordinal)
            .ToArray();

        for (var index = 1; index < bands.Length; index++)
        {
            if (bands[index - 1].FloorMax >= bands[index].FloorMin)
            {
                throw new InvalidDataException(
                    $"Dungeon bands '{bands[index - 1].Id}' and '{bands[index].Id}' have overlapping floor ranges.");
            }
        }
    }

    private static void ValidateEncounterTableRanges(ContentPack pack)
    {
        var tables = pack.EncounterTables.Definitions.Values
            .OrderBy(table => table.FloorMin)
            .ThenBy(table => table.FloorMax)
            .ThenBy(table => table.Id, StringComparer.Ordinal)
            .ToArray();

        if (tables.Length == 0)
        {
            return;
        }

        var nextFloor = 1;

        for (var index = 0; index < tables.Length; index++)
        {
            if (tables[index].FloorMin != nextFloor)
            {
                throw new InvalidDataException(
                    $"Encounter tables do not provide contiguous floor coverage at floor {nextFloor}.");
            }

            nextFloor = tables[index].FloorMax + 1;
        }

        if (nextFloor != 6)
        {
            throw new InvalidDataException("Encounter tables must provide contiguous coverage for floors 1 through 5.");
        }
    }

}
