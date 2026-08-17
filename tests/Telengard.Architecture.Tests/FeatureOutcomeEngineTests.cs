using Telengard.Content;
using Telengard.Core.Rng;
using Telengard.Core.Simulation;
using Xunit;

namespace Telengard.Architecture.Tests;

public sealed class FeatureOutcomeEngineTests
{
    [Fact]
    public void Select_filters_by_conditions_and_ignores_zero_weight_outcomes()
    {
        var definition = new FeatureDefinition(
            "fountain",
            FeatureType.Fountain,
            "fountain",
            outcomeTable:
            [
                new FeatureOutcome(weight: 0, effects: ["never"]),
                new FeatureOutcome(["intelligence>=10"], 2, ["restore"]),
                new FeatureOutcome(["strength>=10"], 2, ["poison"])
            ]);

        var outcome = FeatureOutcomeEngine.Select(
            definition,
            new FeatureOutcomeSelectionContext(["intelligence>=10"]),
            new DeterministicRng(1234, "0.1").CreateStream("feature-outcome"));

        Assert.Equal(["restore"], outcome.Effects);
    }

    [Fact]
    public void Select_replays_from_stable_feature_inputs()
    {
        var definition = new FeatureDefinition(
            "azure-fountain",
            FeatureType.Fountain,
            "fountain",
            outcomeTable:
            [
                new FeatureOutcome(weight: 1, effects: ["rare"]),
                new FeatureOutcome(weight: 3, effects: ["common"])
            ]);
        var position = new DungeonPosition(2, 4, 5);
        var featureId = Guid.Parse("00000000-0000-0000-0000-000000000041");
        var context = new FeatureOutcomeSelectionContext();

        var first = FeatureOutcomeEngine.Select(definition, context, 1234, "0.1", featureId, position, 2);
        var second = FeatureOutcomeEngine.Select(definition, context, 1234, "0.1", featureId, position, 2);

        Assert.Same(first, second);
    }

    [Fact]
    public void Select_uses_each_outcomes_weight_as_its_share_of_the_roll_range()
    {
        var definition = new FeatureDefinition(
            "altar",
            FeatureType.Altar,
            "altar",
            outcomeTable:
            [
                new FeatureOutcome(weight: 1, effects: ["rare"]),
                new FeatureOutcome(weight: 3, effects: ["common"])
            ]);
        var expectedRoll = new DeterministicRng(9876, "0.1")
            .CreateStream("feature-outcome")
            .NextLong(0, 4);

        var selected = FeatureOutcomeEngine.Select(
            definition,
            new FeatureOutcomeSelectionContext(),
            new DeterministicRng(9876, "0.1").CreateStream("feature-outcome"));

        Assert.Same(definition.OutcomeTable[expectedRoll == 0 ? 0 : 1], selected);
    }

    [Fact]
    public void Select_rejects_missing_eligible_outcomes()
    {
        var definition = new FeatureDefinition(
            "altar",
            FeatureType.Altar,
            "altar",
            outcomeTable:
            [
                new FeatureOutcome(["wisdom>=10"], 0, ["unknown"])
            ]);

        Assert.Throws<InvalidOperationException>(() => FeatureOutcomeEngine.Select(
            definition,
            new FeatureOutcomeSelectionContext(),
            new DeterministicRng(1234, "0.1").CreateStream("feature-outcome")));
    }

    [Fact]
    public void Selection_context_copies_and_validates_conditions()
    {
        var conditions = new List<string> { "has:key" };
        var context = new FeatureOutcomeSelectionContext(conditions);
        conditions.Add("depth:5");

        Assert.Equal(["has:key"], context.Conditions);
        Assert.Throws<ArgumentException>(() => new FeatureOutcomeSelectionContext(["has:key", "has:key"]));
        Assert.Throws<ArgumentException>(() => new FeatureOutcomeSelectionContext([" "]));
    }
}
