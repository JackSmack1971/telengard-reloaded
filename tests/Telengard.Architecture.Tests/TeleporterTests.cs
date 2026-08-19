using Telengard.Content;
using Telengard.Core.Knowledge;
using Telengard.Core.Simulation;
using Telengard.Core.World.Features;
using Telengard.Save;
using Telengard.Save.Dto;
using Xunit;

namespace Telengard.Architecture.Tests;

public sealed class TeleporterTests
{
    [Fact]
    public void Teleporter_resolution_moves_the_player_and_updates_expedition_depth()
    {
        var feature = CreateFeature(new DungeonPosition(3, 4, 5));
        var state = ActiveState(feature);
        var destination = new DungeonPosition(7, 9, 11);
        var definition = Definition(new FeatureOutcome(
            weight: 1,
            effects: ["spatial_shift"],
            observations: ["the_air_folds"]));

        var result = TeleporterResolver.Resolve(
            state,
            new ActivateFeatureCommand(feature.InstanceId),
            definition,
            destination);

        Assert.Equal(destination, result.State.Player.Position);
        Assert.Equal(7, result.State.Expedition.DeepestFloorReached);
        Assert.Equal([3, 7], result.State.Expedition.FloorsVisited);
        Assert.Equal(1, Assert.Single(result.State.Dungeon.Features).ActivationCount);
        Assert.Collection(
            result.Events,
            discovered => Assert.Equal(new FeatureDiscoveredEvent(feature.InstanceId, feature.Position), discovered),
            activated => Assert.Equal(new FeatureActivatedEvent(feature.InstanceId, feature.Position, 1), activated),
            outcome =>
            {
                var resolved = Assert.IsType<TeleporterOutcomeResolvedEvent>(outcome);
                Assert.Equal(feature.InstanceId, resolved.FeatureId);
                Assert.Equal(feature.Position, resolved.From);
                Assert.Equal(destination, resolved.To);
                Assert.Equal(1, resolved.ActivationCount);
                Assert.Equal(["spatial_shift"], resolved.Effects);
                Assert.Equal(["the_air_folds"], resolved.Observations);
            });
    }

    [Fact]
    public void Teleporter_selection_replays_from_stable_inputs_and_conditions()
    {
        var feature = CreateFeature(new DungeonPosition(3, 4, 5));
        var state = ActiveState(feature);
        var destination = new DungeonPosition(7, 9, 11);
        var definition = Definition(
            new FeatureOutcome(["has:map"], 1, ["mapped_route"], ["the_route_is_known"]),
            new FeatureOutcome(weight: 0, effects: ["unknown_route"], observations: ["the_world_shifts"]));
        var command = new ActivateFeatureCommand(feature.InstanceId);
        var context = new FeatureOutcomeSelectionContext(["has:map"]);

        var first = TeleporterResolver.Resolve(state, command, definition, destination, context);
        var second = TeleporterResolver.Resolve(state, command, definition, destination, context);

        Assert.Equal(SaveGameSerializer.Serialize(first.State), SaveGameSerializer.Serialize(second.State));
        var firstOutcome = Assert.IsType<TeleporterOutcomeResolvedEvent>(first.Events[^1]);
        var secondOutcome = Assert.IsType<TeleporterOutcomeResolvedEvent>(second.Events[^1]);
        Assert.Equal(firstOutcome.FeatureId, secondOutcome.FeatureId);
        Assert.Equal(firstOutcome.From, secondOutcome.From);
        Assert.Equal(firstOutcome.To, secondOutcome.To);
        Assert.Equal(firstOutcome.ActivationCount, secondOutcome.ActivationCount);
        Assert.Equal(firstOutcome.Effects, secondOutcome.Effects);
        Assert.Equal(firstOutcome.Observations, secondOutcome.Observations);
        Assert.Equal(["mapped_route"], firstOutcome.Effects);
        Assert.Equal(["the_route_is_known"], firstOutcome.Observations);
    }

    [Fact]
    public void Teleporter_resolution_rejects_wrong_definitions_and_missing_outcomes_before_mutation()
    {
        var feature = CreateFeature(new DungeonPosition(3, 4, 5));
        var state = ActiveState(feature);
        var command = new ActivateFeatureCommand(feature.InstanceId);
        var destination = new DungeonPosition(7, 9, 11);

        Assert.Throws<ArgumentException>(() => TeleporterResolver.Resolve(
            state,
            command,
            new FeatureDefinition("stone-pit", FeatureType.Pit, "pit", outcomeTable: [new FeatureOutcome(weight: 1)]),
            destination));
        Assert.Throws<InvalidOperationException>(() => TeleporterResolver.Resolve(
            state,
            command,
            new FeatureDefinition("different-teleporter", FeatureType.Teleporter, "teleporter", outcomeTable: [new FeatureOutcome(weight: 1)]),
            destination));
        Assert.Throws<InvalidOperationException>(() => TeleporterResolver.Resolve(
            state,
            command,
            Definition(new FeatureOutcome(["requires:unknown"], 1, ["never"])),
            destination));

        Assert.Equal(0, Assert.Single(state.Dungeon.Features).ActivationCount);
        Assert.Equal(feature.Position, state.Player.Position);
    }

    [Fact]
    public void Teleporter_state_round_trips_through_the_existing_explicit_save_contract()
    {
        var feature = CreateFeature(new DungeonPosition(3, 4, 5));
        var state = TeleporterResolver.Resolve(
            ActiveState(feature),
            new ActivateFeatureCommand(feature.InstanceId),
            Definition(new FeatureOutcome(weight: 1, observations: ["arrival"])),
            new DungeonPosition(7, 9, 11)).State;

        var restored = SaveGameSerializer.Deserialize(SaveGameSerializer.Serialize(state));

        Assert.Equal(state.Player.Position, restored.Player.Position);
        Assert.Equal(state.Expedition.DeepestFloorReached, restored.Expedition.DeepestFloorReached);
        Assert.Equal(state.Expedition.FloorsVisited, restored.Expedition.FloorsVisited);
        Assert.Equal(state.Dungeon.Features, restored.Dungeon.Features);
        Assert.Equal(GameState.CurrentSaveVersion, restored.SaveVersion);
    }

    [Fact]
    public void Teleporter_node_observation_progresses_from_observed_to_mapped()
    {
        var feature = CreateFeature(new DungeonPosition(3, 4, 5));
        var destination = new DungeonPosition(7, 9, 11);
        var node = new TeleporterNode("node-a", feature.Position, "network-a", "configured-rule");
        var state = ActiveState(feature);
        var definition = Definition(new FeatureOutcome(weight: 1, observations: ["arrival"]));

        var first = TeleporterResolver.Resolve(
            state,
            new ActivateFeatureCommand(feature.InstanceId),
            definition,
            node,
            destination);

        var firstMapping = Assert.Single(first.State.Knowledge.TeleporterMappings);
        Assert.Equal(TeleporterMappingStatus.Observed, firstMapping.Status);
        var firstEvent = Assert.IsType<TeleporterMappingObservedEvent>(first.Events[^1]);
        Assert.Equal(TeleporterMappingStatus.Observed, firstEvent.Status);
        Assert.Equal(
            TeleporterMappingStatus.Observed,
            TeleporterMappingResolver.GetStatus(
                first.State.Knowledge,
                "network-a",
                "node-a",
                feature.Position,
                destination));

        var secondState = first.State with
        {
            Player = first.State.Player with { Position = feature.Position }
        };
        var second = TeleporterResolver.Resolve(
            secondState,
            new ActivateFeatureCommand(feature.InstanceId),
            definition,
            node,
            destination);

        var secondMapping = Assert.Single(second.State.Knowledge.TeleporterMappings);
        Assert.Equal(TeleporterMappingStatus.Mapped, secondMapping.Status);
        var secondEvent = Assert.IsType<TeleporterMappingObservedEvent>(second.Events[^1]);
        Assert.Equal(TeleporterMappingStatus.Mapped, secondEvent.Status);
        Assert.Equal(
            TeleporterMappingStatus.Mapped,
            TeleporterMappingResolver.GetStatus(
                second.State.Knowledge,
                "network-a",
                "node-a",
                feature.Position,
                destination));
    }

    [Fact]
    public void Teleporter_mapping_validates_before_mutation_and_round_trips()
    {
        var feature = CreateFeature(new DungeonPosition(3, 4, 5));
        var state = ActiveState(feature);
        var destination = new DungeonPosition(7, 9, 11);
        var command = new AddTeleporterMappingCommand(
            "network-a",
            "node-a",
            feature.Position,
            destination);

        Assert.Throws<InvalidOperationException>(() => TeleporterMappingResolver.Add(
            state with { Expedition = new ExpeditionState() },
            command));
        Assert.Throws<InvalidOperationException>(() => TeleporterMappingResolver.Add(
            state with { Player = new PlayerState { Alive = false } },
            command));

        var result = TeleporterMappingResolver.Add(state, command);
        var restored = SaveGameSerializer.Deserialize(SaveGameSerializer.Serialize(result.State));

        Assert.Equal(result.State.Knowledge.TeleporterMappings, restored.Knowledge.TeleporterMappings);
        Assert.Equal(SaveGameSerializer.Serialize(result.State), SaveGameSerializer.Serialize(restored));
        Assert.Equal(TeleporterMappingStatus.Unknown, TeleporterMappingResolver.GetStatus(
            state.Knowledge,
            "network-a",
            "node-a",
            feature.Position,
            destination));
    }

    [Fact]
    public void Version_eight_saves_migrate_with_empty_teleporter_mappings()
    {
        var save = GameStateSaveDto.FromState(GameState.Create(1234)) with
        {
            SaveVersion = 8,
            Knowledge = new KnowledgeStateDto { Entries = [] }
        };

        var migrated = SaveMigrations.Migrate(save);

        Assert.Equal(GameState.CurrentSaveVersion, migrated.SaveVersion);
        Assert.Empty(migrated.Knowledge.TeleporterMappings!);
    }

    private static FeatureDefinition Definition(params FeatureOutcome[] outcomes) => new(
        "network-teleporter",
        FeatureType.Teleporter,
        "feature.teleporter.network",
        interactionOptions: ["enter"],
        outcomeTable: outcomes,
        knowledgeCategory: "teleporter");

    private static FeatureInstance CreateFeature(DungeonPosition position) => new(
        Guid.Parse("00000000-0000-0000-0000-000000000045"),
        "network-teleporter",
        position);

    private static GameState ActiveState(FeatureInstance feature) => GameState.Create(1234) with
    {
        Player = new PlayerState { Position = feature.Position },
        Expedition = new ExpeditionState
        {
            Active = true,
            ExpeditionId = Guid.Parse("00000000-0000-0000-0000-000000000046"),
            DeepestFloorReached = feature.Position.Floor,
            FloorsVisited = [feature.Position.Floor]
        },
        Inn = new InnState { IsAtInn = false },
        Dungeon = new DungeonState { Features = [feature] }
    };
}
