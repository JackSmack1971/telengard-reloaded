using Telengard.Content;
using Telengard.Core.Simulation;
using Telengard.Core.World.Features;
using Telengard.Save;
using Xunit;

namespace Telengard.Architecture.Tests;

public sealed class AltarTests
{
    [Fact]
    public void Altar_resolution_selects_configured_outcome_and_emits_observed_result_after_activation()
    {
        var feature = CreateFeature();
        var state = ActiveState(feature) with
        {
            Player = new PlayerState { Position = feature.Position, SpellPower = 4 }
        };
        var definition = Definition(new FeatureOutcome(
            weight: 1,
            effects: ["blessing"],
            observations: ["warm_runes"]));

        var result = AltarResolver.Resolve(state, new ActivateFeatureCommand(feature.InstanceId), definition);

        Assert.Equal(4, result.State.Player.SpellPower);
        Assert.Equal(1, Assert.Single(result.State.Dungeon.Features).ActivationCount);
        Assert.Collection(
            result.Events,
            discovered => Assert.Equal(new FeatureDiscoveredEvent(feature.InstanceId, feature.Position), discovered),
            activated => Assert.Equal(new FeatureActivatedEvent(feature.InstanceId, feature.Position, 1), activated),
            outcome =>
            {
                var resolved = Assert.IsType<AltarOutcomeResolvedEvent>(outcome);
                Assert.Equal(feature.InstanceId, resolved.FeatureId);
                Assert.Equal(feature.Position, resolved.Position);
                Assert.Equal(1, resolved.ActivationCount);
                Assert.Equal(["blessing"], resolved.Effects);
                Assert.Equal(["warm_runes"], resolved.Observations);
            });
    }

    [Fact]
    public void Altar_outcomes_do_not_apply_fountain_effects()
    {
        var feature = CreateFeature();
        var state = ActiveState(feature) with
        {
            Player = new PlayerState
            {
                Position = feature.Position,
                SpellPower = 4,
                MaxSpellPower = 9
            }
        };
        var definition = Definition(new FeatureOutcome(
            weight: 1,
            effects: [FountainEffectIds.RestoreSpellPower],
            observations: ["the altar answers"]));

        var result = AltarResolver.Resolve(state, new ActivateFeatureCommand(feature.InstanceId), definition);

        Assert.Equal(4, result.State.Player.SpellPower);
        Assert.IsType<AltarOutcomeResolvedEvent>(result.Events[^1]);
    }

    [Fact]
    public void Altar_selection_replays_from_stable_inputs_and_conditions()
    {
        var feature = CreateFeature();
        var state = ActiveState(feature);
        var definition = Definition(
            new FeatureOutcome(["has:relic"], 1, ["rare"], ["relic_resonates"]),
            new FeatureOutcome(weight: 3, effects: ["common"], observations: ["dust_stirs"]));
        var command = new ActivateFeatureCommand(feature.InstanceId);
        var context = new FeatureOutcomeSelectionContext(["has:relic"]);

        var first = AltarResolver.Resolve(state, command, definition, context);
        var second = AltarResolver.Resolve(state, command, definition, context);

        Assert.Equal(SaveGameSerializer.Serialize(first.State), SaveGameSerializer.Serialize(second.State));
        Assert.Equal(first.Events.Take(2), second.Events.Take(2));
        var outcome = Assert.IsType<AltarOutcomeResolvedEvent>(first.Events[^1]);
        var replayedOutcome = Assert.IsType<AltarOutcomeResolvedEvent>(second.Events[^1]);
        Assert.Equal(["common"], outcome.Effects);
        Assert.Equal(["dust_stirs"], outcome.Observations);
        Assert.Equal(outcome.FeatureId, replayedOutcome.FeatureId);
        Assert.Equal(outcome.Position, replayedOutcome.Position);
        Assert.Equal(outcome.ActivationCount, replayedOutcome.ActivationCount);
        Assert.Equal(outcome.Effects, replayedOutcome.Effects);
        Assert.Equal(outcome.Observations, replayedOutcome.Observations);
    }

    [Fact]
    public void Altar_resolution_rejects_wrong_definitions_and_missing_outcomes_before_mutation()
    {
        var feature = CreateFeature();
        var state = ActiveState(feature);
        var command = new ActivateFeatureCommand(feature.InstanceId);

        Assert.Throws<ArgumentException>(() => AltarResolver.Resolve(
            state,
            command,
            new FeatureDefinition("fountain", FeatureType.Fountain, "fountain", outcomeTable: [new FeatureOutcome(weight: 1)])));
        Assert.Throws<InvalidOperationException>(() => AltarResolver.Resolve(
            state,
            command,
            new FeatureDefinition("different-altar", FeatureType.Altar, "altar", outcomeTable: [new FeatureOutcome(weight: 1)])));
        Assert.Throws<InvalidOperationException>(() => AltarResolver.Resolve(
            state,
            command,
            Definition(new FeatureOutcome(["requires:unknown"], 1, ["never"]))));
        Assert.Equal(0, Assert.Single(state.Dungeon.Features).ActivationCount);
    }

    [Fact]
    public void Altar_activation_uses_the_existing_explicit_save_contract()
    {
        var feature = CreateFeature();
        var state = AltarResolver.Resolve(
            ActiveState(feature),
            new ActivateFeatureCommand(feature.InstanceId),
            Definition(new FeatureOutcome(weight: 1, effects: ["unknown_effect"]))).State;

        var restored = SaveGameSerializer.Deserialize(SaveGameSerializer.Serialize(state));

        Assert.Equal(state.Dungeon.Features, restored.Dungeon.Features);
        Assert.Equal(GameState.CurrentSaveVersion, restored.SaveVersion);
    }

    private static FeatureDefinition Definition(params FeatureOutcome[] outcomes) => new(
        "stone-altar",
        FeatureType.Altar,
        "feature.altar.stone",
        interactionOptions: ["pray"],
        outcomeTable: outcomes,
        knowledgeCategory: "altar");

    private static FeatureInstance CreateFeature() => new(
        Guid.Parse("00000000-0000-0000-0000-000000000043"),
        "stone-altar",
        new DungeonPosition(1, 0, 0));

    private static GameState ActiveState(FeatureInstance feature) => GameState.Create(1234) with
    {
        Player = new PlayerState { Position = feature.Position },
        Expedition = new ExpeditionState
        {
            Active = true,
            FloorsVisited = [1],
            ExpeditionId = Guid.Parse("00000000-0000-0000-0000-000000000044")
        },
        Inn = new InnState { IsAtInn = false },
        Dungeon = new DungeonState { Features = [feature] }
    };
}
