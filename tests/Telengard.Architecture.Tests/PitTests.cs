using Telengard.Content;
using Telengard.Core.Simulation;
using Telengard.Core.World.Features;
using Telengard.Save;
using Xunit;

namespace Telengard.Architecture.Tests;

public sealed class PitTests
{
    [Fact]
    public void Pit_resolution_drops_the_player_two_floors_and_updates_expedition_depth()
    {
        var feature = CreateFeature(new DungeonPosition(3, 4, 5));
        var state = ActiveState(feature);
        var definition = Definition(new FeatureOutcome(
            weight: 1,
            effects: [PitEffectIds.DropTwoFloors],
            observations: ["the_floor_gives_way"]));

        var result = PitResolver.Resolve(state, new ActivateFeatureCommand(feature.InstanceId), definition);

        Assert.Equal(new DungeonPosition(5, 4, 5), result.State.Player.Position);
        Assert.Equal(5, result.State.Expedition.DeepestFloorReached);
        Assert.Equal([3, 5], result.State.Expedition.FloorsVisited);
        Assert.Equal(1, Assert.Single(result.State.Dungeon.Features).ActivationCount);
        Assert.Collection(
            result.Events,
            discovered => Assert.Equal(new FeatureDiscoveredEvent(feature.InstanceId, feature.Position), discovered),
            activated => Assert.Equal(new FeatureActivatedEvent(feature.InstanceId, feature.Position, 1), activated),
            outcome =>
            {
                var resolved = Assert.IsType<PitOutcomeResolvedEvent>(outcome);
                Assert.Equal(feature.InstanceId, resolved.FeatureId);
                Assert.Equal(feature.Position, resolved.Position);
                Assert.Equal(1, resolved.ActivationCount);
                Assert.Equal([PitEffectIds.DropTwoFloors], resolved.Effects);
                Assert.Equal(["the_floor_gives_way"], resolved.Observations);
            });
    }

    [Fact]
    public void Pit_selection_replays_from_stable_inputs_and_conditions()
    {
        var feature = CreateFeature(new DungeonPosition(3, 4, 5));
        var state = ActiveState(feature);
        var definition = Definition(
            new FeatureOutcome(["has:rope"], 1, [PitEffectIds.DropTwoFloors], ["rope_slows_the_fall"]),
            new FeatureOutcome(weight: 3, observations: ["the_floor_gives_way"]));
        var command = new ActivateFeatureCommand(feature.InstanceId);
        var context = new FeatureOutcomeSelectionContext(["has:rope"]);

        var first = PitResolver.Resolve(state, command, definition, context);
        var second = PitResolver.Resolve(state, command, definition, context);

        Assert.Equal(SaveGameSerializer.Serialize(first.State), SaveGameSerializer.Serialize(second.State));
        var firstOutcome = Assert.IsType<PitOutcomeResolvedEvent>(first.Events[^1]);
        var secondOutcome = Assert.IsType<PitOutcomeResolvedEvent>(second.Events[^1]);
        Assert.Equal(firstOutcome.FeatureId, secondOutcome.FeatureId);
        Assert.Equal(firstOutcome.Position, secondOutcome.Position);
        Assert.Equal(firstOutcome.ActivationCount, secondOutcome.ActivationCount);
        Assert.Equal(firstOutcome.Effects, secondOutcome.Effects);
        Assert.Equal(firstOutcome.Observations, secondOutcome.Observations);
        Assert.Empty(firstOutcome.Effects);
        Assert.Equal(["the_floor_gives_way"], firstOutcome.Observations);
    }

    [Fact]
    public void Pit_resolution_rejects_wrong_definitions_and_unsupported_effects_before_mutation()
    {
        var feature = CreateFeature(new DungeonPosition(3, 4, 5));
        var state = ActiveState(feature);
        var command = new ActivateFeatureCommand(feature.InstanceId);

        Assert.Throws<ArgumentException>(() => PitResolver.Resolve(
            state,
            command,
            new FeatureDefinition("stone-altar", FeatureType.Altar, "altar", outcomeTable: [new FeatureOutcome(weight: 1)])));
        Assert.Throws<InvalidOperationException>(() => PitResolver.Resolve(
            state,
            command,
            new FeatureDefinition("different-pit", FeatureType.Pit, "pit", outcomeTable: [new FeatureOutcome(weight: 1)])));
        Assert.Throws<InvalidOperationException>(() => PitResolver.Resolve(
            state,
            command,
            Definition(new FeatureOutcome(weight: 1, effects: ["unsupported"]))));
        Assert.Equal(0, Assert.Single(state.Dungeon.Features).ActivationCount);
        Assert.Equal(feature.Position, state.Player.Position);
    }

    [Fact]
    public void Pit_resolution_rejects_a_drop_without_a_valid_destination_before_mutation()
    {
        var feature = CreateFeature(new DungeonPosition(49, 4, 5));
        var state = ActiveState(feature);

        Assert.Throws<InvalidOperationException>(() => PitResolver.Resolve(
            state,
            new ActivateFeatureCommand(feature.InstanceId),
            Definition(new FeatureOutcome(weight: 1, effects: [PitEffectIds.DropTwoFloors]))));

        Assert.Equal(0, Assert.Single(state.Dungeon.Features).ActivationCount);
        Assert.Equal(feature.Position, state.Player.Position);
    }

    [Fact]
    public void Pit_state_round_trips_through_the_existing_explicit_save_contract()
    {
        var feature = CreateFeature(new DungeonPosition(3, 4, 5));
        var state = PitResolver.Resolve(
            ActiveState(feature),
            new ActivateFeatureCommand(feature.InstanceId),
            Definition(new FeatureOutcome(weight: 1, effects: [PitEffectIds.DropTwoFloors]))).State;

        var restored = SaveGameSerializer.Deserialize(SaveGameSerializer.Serialize(state));

        Assert.Equal(state.Player.Position, restored.Player.Position);
        Assert.Equal(state.Expedition.DeepestFloorReached, restored.Expedition.DeepestFloorReached);
        Assert.Equal(state.Expedition.FloorsVisited, restored.Expedition.FloorsVisited);
        Assert.Equal(state.Dungeon.Features, restored.Dungeon.Features);
        Assert.Equal(GameState.CurrentSaveVersion, restored.SaveVersion);
    }

    private static FeatureDefinition Definition(params FeatureOutcome[] outcomes) => new(
        "floor-pit",
        FeatureType.Pit,
        "feature.pit.floor",
        interactionOptions: ["fall"],
        outcomeTable: outcomes,
        knowledgeCategory: "hazard");

    private static FeatureInstance CreateFeature(DungeonPosition position) => new(
        Guid.Parse("00000000-0000-0000-0000-000000000044"),
        "floor-pit",
        position);

    private static GameState ActiveState(FeatureInstance feature) => GameState.Create(1234) with
    {
        Player = new PlayerState { Position = feature.Position },
        Expedition = new ExpeditionState
        {
            Active = true,
            ExpeditionId = Guid.Parse("00000000-0000-0000-0000-000000000045"),
            DeepestFloorReached = feature.Position.Floor,
            FloorsVisited = [feature.Position.Floor]
        },
        Dungeon = new DungeonState { Features = [feature] }
    };
}
