using Telengard.Core.Presentation;
using Xunit;

namespace Telengard.Architecture.Tests;

public sealed class PresentationAssetRegistryTests
{
    [Fact]
    public void Resolve_is_deterministic_and_orders_entries_by_stable_key()
    {
        var registry = new PresentationAssetRegistry(
        [
            new PresentationAssetEntry("feature/azure_fountain", "res://features/fountain.tscn"),
            new PresentationAssetEntry("monster/crypt_stalker")
        ]);

        Assert.Equal("res://features/fountain.tscn", registry.Resolve("feature/azure_fountain"));
        Assert.Equal("res://presentation/placeholders/missing_asset.tres", registry.Resolve("monster/crypt_stalker"));
        Assert.Equal(
            ["feature/azure_fountain", "monster/crypt_stalker"],
            registry.Entries.Select(entry => entry.Key));
    }

    [Fact]
    public void Registry_rejects_duplicate_keys_and_missing_required_assets_when_placeholders_are_disallowed()
    {
        Assert.Throws<ArgumentException>(() => new PresentationAssetRegistry(
        [
            new PresentationAssetEntry("feature/fountain"),
            new PresentationAssetEntry("feature/fountain", "res://features/fountain.tscn")
        ]));

        var registry = new PresentationAssetRegistry([new PresentationAssetEntry("feature/fountain", required: true)]);
        Assert.Throws<InvalidOperationException>(() => registry.Validate(allowPlaceholders: false));
        Assert.Equal("res://presentation/placeholders/missing_asset.tres", registry.Resolve("feature/fountain"));
        Assert.Throws<InvalidOperationException>(() => registry.Resolve("feature/fountain", allowPlaceholder: false));
    }

    [Fact]
    public void Missing_unknown_identity_is_explicitly_placeholder_or_validation_failure()
    {
        var registry = new PresentationAssetRegistry([]);

        Assert.Equal("res://presentation/placeholders/missing_asset.tres", registry.Resolve("item/unknown"));
        Assert.Throws<KeyNotFoundException>(() => registry.Resolve("item/unknown", allowPlaceholder: false));
    }
}
