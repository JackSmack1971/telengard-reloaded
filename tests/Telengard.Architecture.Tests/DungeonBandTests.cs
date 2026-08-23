using Telengard.Content;
using Xunit;

namespace Telengard.Architecture.Tests;

public sealed class DungeonBandTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"telengard-band-{Guid.NewGuid():N}");

    public DungeonBandTests() => Directory.CreateDirectory(_root);

    [Fact]
    public void Load_reads_upper_ruins_band_and_resolves_only_its_declared_floors()
    {
        WriteManifest();
        WriteBand("upper-ruins.json", "upper-ruins", 1, 5);

        var pack = ContentPackLoader.Load(_root);

        var band = Assert.Single(pack.Bands.Definitions.Values);
        Assert.Equal("upper-ruins", band.Id);
        Assert.Equal("Upper Ruins", band.DisplayName);
        Assert.Equal("upper-ruins", band.GenerationProfile);
        Assert.Equal(["ruin-dweller"], band.MonsterFamilies);
        Assert.Equal(["darkness"], band.Hazards);
        Assert.Equal(["distant-drips"], band.AmbientRules);
        Assert.Equal("upper-ruins-encounters", band.EncounterEcologyId);
        Assert.Equal("upper-ruins-loot", band.LootProfile);
        Assert.Equal("upper-ruins", band.VisualTheme);
        Assert.Equal("upper-ruins", band.AudioTheme);

        Assert.True(pack.TryGetBandForFloor(1, out var first));
        Assert.Same(band, first);
        Assert.True(pack.TryGetBandForFloor(3, out _));
        Assert.True(pack.TryGetBandForFloor(5, out _));
        Assert.False(pack.TryGetBandForFloor(0, out _));
        Assert.False(pack.TryGetBandForFloor(6, out _));

        var second = ContentPackLoader.Load(_root);
        Assert.Equal(pack.Bands.Definitions.Keys, second.Bands.Definitions.Keys);
        var secondBand = second.Bands.GetRequired("upper-ruins");
        Assert.Equal(band.Id, secondBand.Id);
        Assert.Equal(band.FloorMin, secondBand.FloorMin);
        Assert.Equal(band.FloorMax, secondBand.FloorMax);
        Assert.Equal(band.GenerationProfile, secondBand.GenerationProfile);
        Assert.Equal(band.EncounterEcologyId, secondBand.EncounterEcologyId);
        Assert.Equal(band.LootProfile, secondBand.LootProfile);
        Assert.Equal(band.VisualTheme, secondBand.VisualTheme);
        Assert.Equal(band.AudioTheme, secondBand.AudioTheme);
        Assert.Throws<NotSupportedException>(() =>
        {
            ((IDictionary<string, DungeonBandDefinition>)pack.Bands.Definitions)["new"] = band;
        });
    }

    [Theory]
    [InlineData(0, 5)]
    [InlineData(1, 6)]
    [InlineData(4, 3)]
    public void Load_rejects_band_ranges_outside_or_reversed(int floorMin, int floorMax)
    {
        WriteManifest();
        WriteBand("invalid.json", "invalid", floorMin, floorMax);

        var exception = Assert.Throws<InvalidDataException>(() => ContentPackLoader.Load(_root));

        Assert.Contains("invalid.json", exception.Message);
    }

    [Fact]
    public void Load_rejects_overlapping_band_ranges()
    {
        WriteManifest();
        WriteBand("first.json", "first", 1, 3);
        WriteBand("second.json", "second", 3, 5);

        var exception = Assert.Throws<InvalidDataException>(() => ContentPackLoader.Load(_root));

        Assert.Contains("overlapping floor ranges", exception.Message);
    }

    [Fact]
    public void Load_rejects_duplicate_band_ids()
    {
        WriteManifest();
        WriteBand("first.json", "same", 1, 2);
        WriteBand("second.json", "same", 3, 5);

        var exception = Assert.Throws<InvalidDataException>(() => ContentPackLoader.Load(_root));

        Assert.Contains("Duplicate content id 'same'", exception.Message);
    }

    [Fact]
    public void Definition_copies_nested_collections_into_read_only_views()
    {
        var band = new DungeonBandDefinition(
            "upper-ruins",
            "Upper Ruins",
            1,
            5,
            "upper-ruins",
            ["ruin-dweller"],
            new Dictionary<string, int> { ["fountain"] = 1 },
            ["darkness"],
            ["distant-drips"],
            "upper-ruins-encounters",
            "upper-ruins-loot",
            "upper-ruins",
            "upper-ruins");

        Assert.Throws<NotSupportedException>(() => ((IList<string>)band.MonsterFamilies).Add("other"));
        Assert.Throws<NotSupportedException>(() => ((IList<string>)band.Hazards).Add("other"));
        Assert.Throws<NotSupportedException>(() => ((IList<string>)band.AmbientRules).Add("other"));
        Assert.Throws<NotSupportedException>(() => ((IDictionary<string, int>)band.FeatureWeights)["other"] = 1);
    }

    [Fact]
    public void Repository_pack_contains_the_authored_upper_ruins_collections()
    {
        var repositoryRoot = FindRepositoryRoot();
        var pack = ContentPackLoader.Load(Path.Combine(repositoryRoot, "content"));

        var band = pack.Bands.GetRequired("upper-ruins");
        Assert.Equal("Upper Ruins", band.DisplayName);
        Assert.Equal(1, band.FloorMin);
        Assert.Equal(5, band.FloorMax);
        Assert.Equal("upper-ruins", band.GenerationProfile);
        Assert.Equal(["ruin-dweller"], band.MonsterFamilies);
        Assert.Empty(band.FeatureWeights);
        Assert.Equal(["darkness"], band.Hazards);
        Assert.Equal(["distant-drips"], band.AmbientRules);
        Assert.Equal("upper-ruins-encounters", band.EncounterEcologyId);
        Assert.Equal("upper-ruins-loot", band.LootProfile);
        Assert.Equal("upper-ruins", band.VisualTheme);
        Assert.Equal("upper-ruins", band.AudioTheme);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private void WriteManifest() =>
        File.WriteAllText(Path.Combine(_root, "manifest.json"), "{\"content_version\":\"slice-1\"}");

    private void WriteBand(string fileName, string id, int floorMin, int floorMax)
    {
        var directory = Directory.CreateDirectory(Path.Combine(_root, "bands"));
        var displayName = id == "upper-ruins" ? "Upper Ruins" : id;
        File.WriteAllText(
            Path.Combine(directory.FullName, fileName),
            $$"""
            {
              "id": "{{id}}",
              "display_name": "{{displayName}}",
              "floor_min": {{floorMin}},
              "floor_max": {{floorMax}},
              "generation_profile": "{{id}}",
              "monster_families": ["ruin-dweller"],
              "feature_weights": {},
              "hazards": ["darkness"],
              "ambient_rules": ["distant-drips"],
              "encounter_ecology_id": "{{id}}-encounters",
              "loot_profile": "{{id}}-loot",
              "visual_theme": "{{id}}",
              "audio_theme": "{{id}}"
            }
            """);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Telengard.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new InvalidOperationException("The repository root could not be located from the test output directory.");
    }
}
