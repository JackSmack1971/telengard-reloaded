using Telengard.Content;
using Telengard.Core.Simulation;
using Telengard.Core.World.Features;
using Xunit;

namespace Telengard.Architecture.Tests;

public sealed class FirstSliceFeatureTests
{
    [Fact]
    public void Production_pack_contains_exactly_the_four_first_slice_features()
    {
        var pack = ContentPackLoader.Load(RepositoryContentRoot());

        Assert.Equal(4, pack.Features.Count);
        Assert.Equal(
            [FeatureType.Fountain, FeatureType.Altar, FeatureType.Teleporter, FeatureType.Pit],
            pack.Features.Definitions.Values.Select(feature => feature.Type).OrderBy(type => type));
        Assert.All(pack.Features.Definitions.Values, feature =>
        {
            Assert.NotEmpty(feature.InteractionOptions);
            Assert.False(string.IsNullOrWhiteSpace(feature.PresentationKey));
            Assert.False(string.IsNullOrWhiteSpace(feature.KnowledgeCategory));
            Assert.Contains(feature.OutcomeTable, outcome => outcome.Weight > 0);
        });
    }

    [Fact]
    public void Production_feature_definitions_load_canonically_and_activate_through_existing_resolvers()
    {
        var first = ContentPackLoader.Load(RepositoryContentRoot());
        var second = ContentPackLoader.Load(RepositoryContentRoot());

        Assert.Equal(first.Features.Definitions.Keys, second.Features.Definitions.Keys);
        Assert.Equal(first.Features.Definitions.Values.Select(Fingerprint), second.Features.Definitions.Values.Select(Fingerprint));

        var position = new DungeonPosition(1, 0, 0);
        var definitions = new[] { "azure-fountain", "stone-altar", "bottomless-pit", "network-teleporter" }
            .Select(first.Features.GetRequired)
            .ToArray();
        var features = definitions
            .Select((definition, index) => new FeatureInstance(
                Guid.Parse($"00000000-0000-0000-0000-0000000000{index + 51:00}"),
                definition.Id,
                position))
            .ToArray();
        var state = GameState.Create(1234) with
        {
            Player = new PlayerState { Position = position, MaxSpellPower = 10 },
            Expedition = new ExpeditionState { Active = true, FloorsVisited = [1] },
            Inn = new InnState { IsAtInn = false },
            Dungeon = new DungeonState { Features = features }
        };

        var fountain = FountainResolver.Resolve(state, new ActivateFeatureCommand(features[0].InstanceId), definitions[0]);
        var altar = AltarResolver.Resolve(state, new ActivateFeatureCommand(features[1].InstanceId), definitions[1]);
        var pit = PitResolver.Resolve(state, new ActivateFeatureCommand(features[2].InstanceId), definitions[2]);
        var teleporter = TeleporterResolver.Resolve(
            state,
            new ActivateFeatureCommand(features[3].InstanceId),
            definitions[3],
            new DungeonPosition(2, 1, 1));

        Assert.IsType<FountainOutcomeResolvedEvent>(fountain.Events[^1]);
        Assert.IsType<AltarOutcomeResolvedEvent>(altar.Events[^1]);
        Assert.IsType<PitOutcomeResolvedEvent>(pit.Events[^1]);
        Assert.IsType<TeleporterOutcomeResolvedEvent>(teleporter.Events[^1]);
        Assert.Equal(new DungeonPosition(2, 1, 1), teleporter.State.Player.Position);
        Assert.Empty(state.Knowledge.Entries);
    }

    private static string Fingerprint(FeatureDefinition feature) => string.Join(
        "|",
        feature.Id,
        feature.Type,
        feature.PresentationKey,
        string.Join(",", feature.InteractionOptions),
        string.Join(",", feature.HintRules.OrderBy(pair => pair.Key).Select(pair => $"{pair.Key}={pair.Value}")),
        feature.KnowledgeCategory,
        string.Join(";", feature.OutcomeTable.Select(outcome => string.Join(",", outcome.Conditions) + ":" + outcome.Weight + ":" + string.Join(",", outcome.Effects) + ":" + string.Join(",", outcome.Observations))));

    private static string RepositoryContentRoot() =>
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "content");
}
