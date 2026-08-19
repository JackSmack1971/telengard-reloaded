using Telengard.Content;
using Telengard.Core.Simulation;
using Telengard.Core.World.Features;
using Telengard.Save;
using Xunit;

namespace Telengard.Architecture.Tests;

public sealed class FountainTests
{
    [Fact]
    public void Fountain_resolution_applies_observable_effects_after_activation_commits()
    {
        var feature = CreateFeature();
        var state = ActiveState(feature) with
        {
            Player = new PlayerState
            {
                Position = feature.Position,
                SpellPower = 2,
                MaxSpellPower = 9,
                TemporaryEffects = ["poison", "slow"]
            }
        };
        var definition = Definition(
            new FeatureOutcome(
                weight: 1,
                effects: [FountainEffectIds.RestoreSpellPower, FountainEffectIds.CleansePoison],
                observations: ["cold_water"]));

        var result = FountainResolver.Resolve(state, new ActivateFeatureCommand(feature.InstanceId), definition);

        Assert.Equal(9, result.State.Player.SpellPower);
        Assert.Equal(["slow"], result.State.Player.TemporaryEffects);
        Assert.Equal(1, Assert.Single(result.State.Dungeon.Features).ActivationCount);
        Assert.Equal(3, result.Events.Count);
        var outcome = Assert.IsType<FountainOutcomeResolvedEvent>(result.Events[^1]);
        Assert.Equal(feature.InstanceId, outcome.FeatureId);
        Assert.Equal(feature.Position, outcome.Position);
        Assert.Equal(1, outcome.ActivationCount);
        Assert.Equal([FountainEffectIds.RestoreSpellPower, FountainEffectIds.CleansePoison], outcome.Effects);
        Assert.Equal(["cold_water"], outcome.Observations);
    }

    [Fact]
    public void Fountain_resolution_retains_unknown_transformation_as_an_observed_noop()
    {
        var feature = CreateFeature();
        var state = ActiveState(feature) with
        {
            Player = new PlayerState { Position = feature.Position, SpellPower = 3, MaxSpellPower = 9 }
        };
        var definition = Definition(new FeatureOutcome(
            weight: 1,
            effects: [FountainEffectIds.Blindness, FountainEffectIds.UnknownTransformation]));

        var result = FountainResolver.Resolve(state, new ActivateFeatureCommand(feature.InstanceId), definition);

        Assert.Equal(3, result.State.Player.SpellPower);
        Assert.Equal([FountainEffectIds.Blindness], result.State.Player.TemporaryEffects);
        var outcome = Assert.IsType<FountainOutcomeResolvedEvent>(result.Events[^1]);
        Assert.Equal([FountainEffectIds.Blindness, FountainEffectIds.UnknownTransformation], outcome.Effects);
    }

    [Fact]
    public void Fountain_selection_replays_from_stable_inputs_and_respects_conditions()
    {
        var feature = CreateFeature();
        var state = ActiveState(feature);
        var definition = Definition(
            new FeatureOutcome(["has:silver-vial"], 1, [FountainEffectIds.CleansePoison]),
            new FeatureOutcome(weight: 3, effects: [FountainEffectIds.RestoreSpellPower]));
        var command = new ActivateFeatureCommand(feature.InstanceId);
        var context = new FeatureOutcomeSelectionContext(["has:silver-vial"]);

        var first = FountainResolver.Resolve(state, command, definition, context);
        var second = FountainResolver.Resolve(state, command, definition, context);

        Assert.Equal(
            SaveGameSerializer.Serialize(first.State),
            SaveGameSerializer.Serialize(second.State));
        var firstOutcome = Assert.IsType<FountainOutcomeResolvedEvent>(first.Events[^1]);
        var secondOutcome = Assert.IsType<FountainOutcomeResolvedEvent>(second.Events[^1]);
        Assert.Equal(firstOutcome.FeatureId, secondOutcome.FeatureId);
        Assert.Equal(firstOutcome.Position, secondOutcome.Position);
        Assert.Equal(firstOutcome.ActivationCount, secondOutcome.ActivationCount);
        Assert.Equal(firstOutcome.Effects, secondOutcome.Effects);
        Assert.Equal(firstOutcome.Observations, secondOutcome.Observations);
        Assert.Equal([FountainEffectIds.CleansePoison], firstOutcome.Effects);
    }

    [Fact]
    public void Fountain_resolution_rejects_non_fountains_mismatched_definitions_and_unsupported_effects_before_mutation()
    {
        var feature = CreateFeature();
        var state = ActiveState(feature);
        var command = new ActivateFeatureCommand(feature.InstanceId);

        Assert.Throws<ArgumentException>(() => FountainResolver.Resolve(
            state,
            command,
            new FeatureDefinition("azure-altar", FeatureType.Altar, "altar", outcomeTable: [new FeatureOutcome(weight: 1)])));
        Assert.Throws<InvalidOperationException>(() => FountainResolver.Resolve(
            state,
            command,
            new FeatureDefinition("different-fountain", FeatureType.Fountain, "fountain", outcomeTable: [new FeatureOutcome(weight: 1)])));
        Assert.Throws<InvalidOperationException>(() => FountainResolver.Resolve(
            state,
            command,
            Definition(new FeatureOutcome(weight: 1, effects: ["unsupported"]))));
        Assert.Equal(0, Assert.Single(state.Dungeon.Features).ActivationCount);
    }

    [Fact]
    public void Fountain_state_round_trips_through_the_existing_explicit_save_contract()
    {
        var feature = CreateFeature();
        var state = FountainResolver.Resolve(
            ActiveState(feature),
            new ActivateFeatureCommand(feature.InstanceId),
            Definition(new FeatureOutcome(weight: 1, effects: [FountainEffectIds.Blindness]))).State;

        var restored = SaveGameSerializer.Deserialize(SaveGameSerializer.Serialize(state));

        Assert.Equal(state.Player.SpellPower, restored.Player.SpellPower);
        Assert.Equal(state.Player.MaxSpellPower, restored.Player.MaxSpellPower);
        Assert.Equal(state.Player.TemporaryEffects, restored.Player.TemporaryEffects);
        Assert.Equal(state.Dungeon.Features, restored.Dungeon.Features);
        Assert.Equal(GameState.CurrentSaveVersion, restored.SaveVersion);
    }

    private static FeatureDefinition Definition(params FeatureOutcome[] outcomes) => new(
        "azure-fountain",
        FeatureType.Fountain,
        "feature.fountain.azure",
        interactionOptions: ["drink"],
        outcomeTable: outcomes);

    private static FeatureInstance CreateFeature() => new(
        Guid.Parse("00000000-0000-0000-0000-000000000042"),
        "azure-fountain",
        new DungeonPosition(1, 0, 0));

    private static GameState ActiveState(FeatureInstance feature) => GameState.Create(1234) with
    {
        Player = new PlayerState { Position = feature.Position, MaxSpellPower = 10 },
        Expedition = new ExpeditionState
        {
            Active = true,
            FloorsVisited = [1],
            ExpeditionId = Guid.Parse("00000000-0000-0000-0000-000000000043")
        },
        Inn = new InnState { IsAtInn = false },
        Dungeon = new DungeonState { Features = [feature] }
    };
}
