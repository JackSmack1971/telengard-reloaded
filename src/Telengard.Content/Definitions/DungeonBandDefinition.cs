using System.Collections.ObjectModel;

namespace Telengard.Content;

public sealed record DungeonBandDefinition
{
    public DungeonBandDefinition(
        string id,
        string displayName,
        int floorMin,
        int floorMax,
        string generationProfile,
        IEnumerable<string>? monsterFamilies = null,
        IReadOnlyDictionary<string, int>? featureWeights = null,
        IEnumerable<string>? hazards = null,
        IEnumerable<string>? ambientRules = null,
        string? encounterEcologyId = null,
        string? lootProfile = null,
        string? visualTheme = null,
        string? audioTheme = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentOutOfRangeException.ThrowIfLessThan(floorMin, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(floorMax, 5);
        if (floorMin > floorMax)
        {
            throw new ArgumentException("A dungeon band minimum floor cannot exceed its maximum floor.", nameof(floorMin));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(generationProfile);
        ArgumentException.ThrowIfNullOrWhiteSpace(visualTheme);
        ArgumentException.ThrowIfNullOrWhiteSpace(audioTheme);

        Id = id;
        DisplayName = displayName;
        FloorMin = floorMin;
        FloorMax = floorMax;
        GenerationProfile = generationProfile;
        MonsterFamilies = CopyTags(monsterFamilies, nameof(monsterFamilies));
        FeatureWeights = CopyWeights(featureWeights);
        Hazards = CopyTags(hazards, nameof(hazards));
        AmbientRules = CopyTags(ambientRules, nameof(ambientRules));
        EncounterEcologyId = NormalizeOptional(encounterEcologyId);
        LootProfile = NormalizeOptional(lootProfile);
        VisualTheme = visualTheme;
        AudioTheme = audioTheme;
    }

    public string Id { get; }
    public string DisplayName { get; }
    public int FloorMin { get; }
    public int FloorMax { get; }
    public string GenerationProfile { get; }
    public IReadOnlyList<string> MonsterFamilies { get; }
    public IReadOnlyDictionary<string, int> FeatureWeights { get; }
    public IReadOnlyList<string> Hazards { get; }
    public IReadOnlyList<string> AmbientRules { get; }
    public string? EncounterEcologyId { get; }
    public string? LootProfile { get; }
    public string VisualTheme { get; }
    public string AudioTheme { get; }

    public bool CoversFloor(int floor) => floor >= FloorMin && floor <= FloorMax;

    private static IReadOnlyList<string> CopyTags(IEnumerable<string>? values, string parameterName)
    {
        if (values is null)
        {
            return Array.Empty<string>();
        }

        var copy = new List<string>();
        foreach (var value in values)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
            if (copy.Contains(value, StringComparer.Ordinal))
            {
                throw new ArgumentException("Values must be unique.", parameterName);
            }

            copy.Add(value);
        }

        return Array.AsReadOnly(copy.ToArray());
    }

    private static IReadOnlyDictionary<string, int> CopyWeights(IReadOnlyDictionary<string, int>? values)
    {
        if (values is null)
        {
            return new ReadOnlyDictionary<string, int>(new Dictionary<string, int>(StringComparer.Ordinal));
        }

        var copy = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var pair in values)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(pair.Key);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pair.Value);
            if (!copy.TryAdd(pair.Key, pair.Value))
            {
                throw new ArgumentException("Feature weight names must be unique.", nameof(values));
            }
        }

        return new ReadOnlyDictionary<string, int>(copy);
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;
}
