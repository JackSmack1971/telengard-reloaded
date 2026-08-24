using System.Text.Json;
using Telengard.Content;
using Xunit;

namespace Telengard.Architecture.Tests;

public sealed class ContentPackLoaderTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"telengard-content-{Guid.NewGuid():N}");

    public ContentPackLoaderTests() => Directory.CreateDirectory(_root);

    [Fact]
    public void Load_reads_external_definitions_into_versioned_catalogs()
    {
        WriteManifest("slice-1");
        Write("items", "z-tooth.json", "{\"id\":\"tooth\",\"display_name\":\"Rat Tooth\",\"category\":\"material\"}");
        Write("items", "a-claw.json", "{\"id\":\"claw\",\"display_name\":\"Rat Claw\",\"category\":\"material\",\"base_properties\":{\"values\":\"keeps-map-keys\"}}");
        Write("loot_tables", "common.json", "{\"id\":\"common\",\"entries\":[{\"item_id\":\"tooth\",\"weight\":1}]}");
        Write("monsters", "rat.json", "{\"id\":\"rat\",\"display_name\":\"Cave Rat\",\"family\":\"Beasts\",\"base_stats\":{\"hit_points\":3},\"loot_table\":\"common\"}");
        Write("spells", "spark.json", "{\"id\":\"spark\",\"name\":\"Spark\",\"initial_description\":\"A spark.\",\"discovered_descriptions\":[],\"cost\":1,\"targeting_rule\":\"single_target\"}");
        Write("features", "fountain.json", "{\"id\":\"fountain\",\"type\":\"Fountain\",\"presentation_key\":\"feature.fountain\",\"interaction_options\":[\"drink\"],\"knowledge_category\":\"fountain\",\"outcome_table\":[{\"weight\":1}]}");
        Write("talents", "first-step.json", "{\"id\":\"first-step\",\"constellation\":\"Survival\",\"prerequisites\":[],\"effects\":[],\"cost\":0}");

        var pack = ContentPackLoader.Load(_root);

        Assert.Equal("slice-1", pack.ContentVersion);
        Assert.Equal(["claw", "tooth"], pack.Items.Definitions.Keys);
        Assert.Equal("keeps-map-keys", pack.Items.GetRequired("claw").BaseProperties["values"]);
        Assert.Equal("common", pack.Monsters.GetRequired("rat").LootTable);
        Assert.Equal(3, pack.Monsters.GetRequired("rat").BaseStats["hit_points"]);
        Assert.Equal("Spark", pack.Spells.GetRequired("spark").Name);
        Assert.Equal(FeatureType.Fountain, pack.Features.GetRequired("fountain").Type);
        Assert.Equal("Survival", pack.Talents.GetRequired("first-step").Constellation);

        var second = ContentPackLoader.Load(_root);
        Assert.Equal(pack.ContentVersion, second.ContentVersion);
        Assert.Equal(pack.Items.Definitions.Keys, second.Items.Definitions.Keys);
        Assert.Throws<NotSupportedException>(() =>
        {
            ((IDictionary<string, ItemDefinition>)pack.Items.Definitions)["new"] = pack.Items.GetRequired("claw");
        });
    }

    [Fact]
    public void Load_rejects_duplicate_ids_and_unresolved_references()
    {
        WriteManifest("slice-1");
        Write("items", "one.json", "{\"id\":\"same\",\"display_name\":\"One\",\"category\":\"material\"}");
        Write("items", "two.json", "{\"id\":\"same\",\"display_name\":\"Two\",\"category\":\"material\"}");

        var duplicate = Assert.Throws<InvalidDataException>(() => ContentPackLoader.Load(_root));
        Assert.Contains("Duplicate content id 'same'", duplicate.Message);

        Directory.Delete(Path.Combine(_root, "items"), recursive: true);
        Write("loot_tables", "common.json", "{\"id\":\"common\",\"entries\":[{\"itemId\":\"missing\",\"weight\":1}]}");

        var missing = Assert.Throws<InvalidDataException>(() => ContentPackLoader.Load(_root));
        Assert.Contains("references missing item 'missing'", missing.Message);

        Directory.Delete(Path.Combine(_root, "loot_tables"), recursive: true);
        Write("monsters", "rat.json", "{\"id\":\"rat\",\"display_name\":\"Cave Rat\",\"family\":\"Beasts\",\"loot_table\":\"missing\"}");

        var missingTable = Assert.Throws<InvalidDataException>(() => ContentPackLoader.Load(_root));
        Assert.Contains("references missing loot table 'missing'", missingTable.Message);
    }

    [Fact]
    public void Load_rejects_missing_or_invalid_manifest_and_definition_json()
    {
        var missing = Assert.Throws<InvalidDataException>(() => ContentPackLoader.Load(_root));
        Assert.Contains("manifest", missing.Message, StringComparison.OrdinalIgnoreCase);

        Write("manifest.json", "{\"contentVersion\":\" \"}");
        var blank = Assert.Throws<InvalidDataException>(() => ContentPackLoader.Load(_root));
        Assert.Contains("contentVersion", blank.Message);

        WriteManifest("slice-1");
        Write("items", "broken.json", "not-json");
        var invalid = Assert.Throws<InvalidDataException>(() => ContentPackLoader.Load(_root));
        Assert.Contains("broken.json", invalid.Message);

        File.Delete(Path.Combine(_root, "items", "broken.json"));
        Write("monsters", "broken-number.json", "{\"id\":\"broken-number\",\"display_name\":\"Broken\",\"family\":\"Beasts\",\"base_stats\":{\"hit_points\":2147483648}}");
        var invalidNumber = Assert.Throws<InvalidDataException>(() => ContentPackLoader.Load(_root));
        Assert.Contains("broken-number.json", invalidNumber.Message);
    }

    [Fact]
    public void Load_rejects_numeric_enum_values_and_duplicate_casing_aliases()
    {
        WriteManifest("slice-1");
        Write("features", "numeric-type.json", "{\"id\":\"numeric\",\"type\":\"1\",\"presentation_key\":\"feature.numeric\"}");

        var numericEnum = Assert.Throws<InvalidDataException>(() => ContentPackLoader.Load(_root));
        Assert.Contains("numeric-type.json", numericEnum.Message);

        Directory.Delete(Path.Combine(_root, "features"), recursive: true);
        Write("items", "duplicate-alias.json", "{\"id\":\"duplicate\",\"displayName\":\"One\",\"display_name\":\"Two\",\"category\":\"material\"}");

        var duplicateAlias = Assert.Throws<InvalidDataException>(() => ContentPackLoader.Load(_root));
        Assert.Contains("duplicate-alias.json", duplicateAlias.Message);
    }

    [Fact]
    public void Load_rejects_malformed_feature_outcomes_effects_and_teleporter_references()
    {
        WriteManifest("slice-1");
        Write("features", "missing-outcome.json", "{\"id\":\"fountain\",\"type\":\"Fountain\",\"presentation_key\":\"feature.fountain\",\"interaction_options\":[\"drink\"],\"knowledge_category\":\"fountain\",\"outcome_table\":[{\"weight\":0}]}");

        var missingOutcome = Assert.Throws<InvalidDataException>(() => ContentPackLoader.Load(_root));
        Assert.Contains("positive-weight outcome", missingOutcome.Message);

        File.Delete(Path.Combine(_root, "features", "missing-outcome.json"));
        Write("features", "unsupported-effect.json", "{\"id\":\"fountain\",\"type\":\"Fountain\",\"presentation_key\":\"feature.fountain\",\"interaction_options\":[\"drink\"],\"knowledge_category\":\"fountain\",\"outcome_table\":[{\"weight\":1,\"effects\":[\"unknown\"]}]}");

        var unsupportedEffect = Assert.Throws<InvalidDataException>(() => ContentPackLoader.Load(_root));
        Assert.Contains("unsupported effect 'unknown'", unsupportedEffect.Message);

        File.Delete(Path.Combine(_root, "features", "unsupported-effect.json"));
        Write("features", "teleporter.json", "{\"id\":\"teleporter\",\"type\":\"Teleporter\",\"presentation_key\":\"feature.teleporter\",\"interaction_options\":[\"enter\"],\"knowledge_category\":\"teleporter\",\"outcome_table\":[{\"weight\":1}]}");

        var missingReference = Assert.Throws<InvalidDataException>(() => ContentPackLoader.Load(_root));
        Assert.Contains("hint rule 'network_id'", missingReference.Message);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private void WriteManifest(string contentVersion) =>
        Write("manifest.json", $"{{\"content_version\":\"{contentVersion}\"}}");

    private void Write(string fileName, string json) =>
        File.WriteAllText(Path.Combine(_root, fileName), json);

    private void Write(string relativeDirectory, string fileName, string json)
    {
        var directory = relativeDirectory == "manifest.json"
            ? _root
            : Directory.CreateDirectory(Path.Combine(_root, relativeDirectory)).FullName;
        File.WriteAllText(Path.Combine(directory, fileName == "" ? relativeDirectory : fileName), json);
    }
}
