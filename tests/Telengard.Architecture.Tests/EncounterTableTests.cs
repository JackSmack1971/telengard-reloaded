using Telengard.Content;
using Telengard.Core.World.Generation;
using Xunit;

namespace Telengard.Architecture.Tests;

public sealed class EncounterTableTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"telengard-encounters-{Guid.NewGuid():N}");

    public EncounterTableTests() => Directory.CreateDirectory(_root);

    [Fact]
    public void First_slice_table_covers_every_band_floor_and_resolves_the_roster()
    {
        var pack = ContentPackLoader.Load(RepositoryContentRoot());

        var table = Assert.IsType<EncounterTable>(pack.EncounterTables.GetRequired("upper-ruins-encounters"));
        Assert.Equal(1, table.FloorMin);
        Assert.Equal(5, table.FloorMax);
        Assert.Equal(8, table.Entries.Count);
        Assert.All(table.Entries, entry => Assert.True(pack.Monsters.TryGet(entry.MonsterId, out _)));
        Assert.All(Enumerable.Range(1, 5), floor =>
            Assert.Same(table, pack.TryGetEncounterTableForFloor(floor, out var resolved) ? resolved : null));

        var configuration = table.ToTriggerConfiguration(1);
        Assert.Equal(table.Entries.Select(entry => entry.MonsterId), configuration.SpawnOptions.Select(option => option.DefinitionId));
        Assert.Equal(table.Entries.Select(entry => entry.Weight), configuration.SpawnOptions.Select(option => option.Weight));
        Assert.Equal(configuration.SpawnOptions.Select(option => option.DefinitionId),
            pack.CreateEncounterTriggerConfiguration(1, 1).SpawnOptions.Select(option => option.DefinitionId));
    }

    [Fact]
    public void Loader_rejects_unknown_monsters_duplicate_entries_and_overlapping_ranges()
    {
        WriteManifest();
        Write("monsters/rat.json", "{\"id\":\"rat\",\"display_name\":\"Rat\",\"family\":\"Beasts\"}");
        Write("bands/upper.json", "{\"id\":\"upper\",\"display_name\":\"Upper\",\"floor_min\":1,\"floor_max\":5,\"generation_profile\":\"upper\",\"visual_theme\":\"upper\",\"audio_theme\":\"upper\",\"encounter_ecology_id\":\"encounters\"}");
        Write("encounter_tables/encounters.json", "{\"id\":\"encounters\",\"floor_min\":1,\"floor_max\":5,\"entries\":[{\"monster_id\":\"missing\",\"weight\":1,\"level\":1,\"current_hit_points\":1}]}");

        var missing = Assert.Throws<InvalidDataException>(() => ContentPackLoader.Load(_root));
        Assert.Contains("missing monster 'missing'", missing.Message);

        Write("encounter_tables/encounters.json", "{\"id\":\"encounters\",\"floor_min\":1,\"floor_max\":5,\"entries\":[{\"monster_id\":\"rat\",\"weight\":1,\"level\":1,\"current_hit_points\":1},{\"monster_id\":\"rat\",\"weight\":1,\"level\":1,\"current_hit_points\":1}]}");
        Assert.Throws<InvalidDataException>(() => ContentPackLoader.Load(_root));

        Write("encounter_tables/encounters.json", "{\"id\":\"encounters\",\"floor_min\":1,\"floor_max\":5,\"entries\":[{\"monster_id\":\"rat\",\"weight\":1,\"level\":1,\"current_hit_points\":1}]}");
        Write("bands/upper.json", "{\"id\":\"upper\",\"display_name\":\"Upper\",\"floor_min\":1,\"floor_max\":5,\"generation_profile\":\"upper\",\"visual_theme\":\"upper\",\"audio_theme\":\"upper\",\"encounter_ecology_id\":\"missing\"}");
        Assert.Throws<InvalidDataException>(() => ContentPackLoader.Load(_root));
    }

    [Fact]
    public void Table_adapter_preserves_deterministic_trigger_replay()
    {
        var pack = ContentPackLoader.Load(RepositoryContentRoot());
        var table = pack.EncounterTables.GetRequired("upper-ruins-encounters");
        var layout = new FloorLayoutGenerator().Generate(1234, "generator-1", 1);
        var entered = DungeonWalkingResolver.Enter(
            Telengard.Core.Simulation.GameState.Create(1234),
            new EnterDungeonCommand(),
            layout);

        var first = Telengard.Core.Combat.EncounterTriggerResolver.Evaluate(
            entered.State,
            entered.State.Player.Position,
            table.ToTriggerConfiguration(1));
        var second = Telengard.Core.Combat.EncounterTriggerResolver.Evaluate(
            entered.State,
            entered.State.Player.Position,
            table.ToTriggerConfiguration(1));

        Assert.Equal(first.State, second.State);
        Assert.Equal(first.Events, second.Events);
    }

    [Fact]
    public void Weighted_trigger_selection_reaches_each_cumulative_interval()
    {
        var position = new Telengard.Core.Simulation.DungeonPosition(1, 0, 0);
        var state = Telengard.Core.Simulation.GameState.Create(1234) with
        {
            Player = new Telengard.Core.Simulation.PlayerState { Position = position },
            Expedition = new Telengard.Core.Simulation.ExpeditionState { Active = true }
        };
        var configuration = new Telengard.Core.Combat.EncounterTriggerConfiguration(1,
        [
            new Telengard.Core.Combat.EncounterSpawnOption("light", 1, 1, 1),
            new Telengard.Core.Combat.EncounterSpawnOption("heavy", 1, 1, 3)
        ]);

        var selected = Enumerable.Range(0, 100)
            .Select(tick => Telengard.Core.Combat.EncounterTriggerResolver.Evaluate(
                state with { SimulationTick = tick }, position, configuration))
            .Select(result => Assert.IsType<Telengard.Core.Combat.EncounterStartedEvent>(Assert.Single(result.Events)).Monster.DefinitionId)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Equal(["heavy", "light"], selected.OrderBy(value => value, StringComparer.Ordinal));
    }

    [Fact]
    public void Explicit_unit_weights_preserve_the_legacy_encounter_stream()
    {
        var position = new Telengard.Core.Simulation.DungeonPosition(1, 0, 0);
        var state = Telengard.Core.Simulation.GameState.Create(1234) with
        {
            Player = new Telengard.Core.Simulation.PlayerState { Position = position },
            Expedition = new Telengard.Core.Simulation.ExpeditionState { Active = true }
        };
        var implicitWeights = new Telengard.Core.Combat.EncounterTriggerConfiguration(1,
        [new Telengard.Core.Combat.EncounterSpawnOption("one", 1, 1), new Telengard.Core.Combat.EncounterSpawnOption("two", 1, 1)]);
        var explicitWeights = new Telengard.Core.Combat.EncounterTriggerConfiguration(1,
        [new Telengard.Core.Combat.EncounterSpawnOption("one", 1, 1, 1), new Telengard.Core.Combat.EncounterSpawnOption("two", 1, 1, 1)]);

        var implicitResults = Enumerable.Range(0, 20).Select(tick =>
            Telengard.Core.Combat.EncounterTriggerResolver.Evaluate(state with { SimulationTick = tick }, position, implicitWeights).State);
        var explicitResults = Enumerable.Range(0, 20).Select(tick =>
            Telengard.Core.Combat.EncounterTriggerResolver.Evaluate(state with { SimulationTick = tick }, position, explicitWeights).State);

        Assert.Equal(implicitResults, explicitResults);
    }

    [Fact]
    public void Encounter_table_constructor_rejects_invalid_ranges_weights_and_overflow()
    {
        var entry = new EncounterTableEntry("rat", 1, 1, 1);
        Assert.Throws<ArgumentOutOfRangeException>(() => new EncounterTable("bad", 0, 1, [entry]));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EncounterTableEntry("rat", 0, 1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EncounterTableEntry("rat", 1, 0, 1));
        Assert.Throws<OverflowException>(() => new EncounterTable(
            "overflow", 1, 5,
            [new EncounterTableEntry("one", long.MaxValue, 1, 1), new EncounterTableEntry("two", 1, 1, 1)]));
    }

    [Fact]
    public void Loader_rejects_gaps_and_overlapping_floor_ranges()
    {
        WriteManifest();
        Write("monsters/rat.json", "{\"id\":\"rat\",\"display_name\":\"Rat\",\"family\":\"Beasts\"}");
        Write("encounter_tables/first.json", "{\"id\":\"first\",\"floor_min\":1,\"floor_max\":2,\"entries\":[{\"monster_id\":\"rat\",\"weight\":1,\"level\":1,\"current_hit_points\":1}]}");
        Write("encounter_tables/second.json", "{\"id\":\"second\",\"floor_min\":4,\"floor_max\":5,\"entries\":[{\"monster_id\":\"rat\",\"weight\":1,\"level\":1,\"current_hit_points\":1}]}");

        Assert.Throws<InvalidDataException>(() => ContentPackLoader.Load(_root));

        File.WriteAllText(Path.Combine(_root, "encounter_tables/first.json"),
            "{\"id\":\"first\",\"floor_min\":1,\"floor_max\":5,\"entries\":[{\"monster_id\":\"rat\",\"weight\":1,\"level\":1,\"current_hit_points\":1}]}");
        File.WriteAllText(Path.Combine(_root, "encounter_tables/second.json"),
            "{\"id\":\"second\",\"floor_min\":5,\"floor_max\":5,\"entries\":[{\"monster_id\":\"rat\",\"weight\":1,\"level\":1,\"current_hit_points\":1}]}");

        Assert.Throws<InvalidDataException>(() => ContentPackLoader.Load(_root));
    }

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }

    private static string RepositoryContentRoot() =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "content"));

    private void WriteManifest() => Write("manifest.json", "{\"content_version\":\"slice-1\"}");

    private void Write(string relativePath, string json)
    {
        var path = Path.Combine(_root, relativePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, json);
    }
}
