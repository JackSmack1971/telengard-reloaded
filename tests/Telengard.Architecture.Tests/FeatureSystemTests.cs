using System.Text.Json;
using Telengard.Content;
using Telengard.Core.Simulation;
using Telengard.Core.World.Features;
using Telengard.Save;
using Xunit;

namespace Telengard.Architecture.Tests;

public sealed class FeatureSystemTests
{
    [Fact]
    public void Definition_preserves_feature_schema_without_owning_runtime_state()
    {
        var definition = new FeatureDefinition(
            "azure-fountain",
            FeatureType.Fountain,
            "feature.fountain.azure",
            ["drink", "inspect"],
            [new FeatureOutcome(["intelligence>=10"], 3, ["restore_spell_power"], ["cold_water"])],
            new Dictionary<string, string> { ["color"] = "azure", ["smell"] = "ozone" },
            "fountain");

        Assert.Equal("azure-fountain", definition.Id);
        Assert.Equal(FeatureType.Fountain, definition.Type);
        Assert.Equal("feature.fountain.azure", definition.PresentationKey);
        Assert.Equal(["drink", "inspect"], definition.InteractionOptions);
        Assert.Equal(3, definition.OutcomeTable[0].Weight);
        Assert.Equal("restore_spell_power", definition.OutcomeTable[0].Effects[0]);
        Assert.Equal("azure", definition.HintRules["color"]);
        Assert.Equal("fountain", definition.KnowledgeCategory);

        var json = JsonSerializer.Serialize(definition);
        Assert.Contains("\"Id\":\"azure-fountain\"", json);
        Assert.DoesNotContain("ActivationCount", json);
    }

    [Fact]
    public void Definition_copies_collections_and_rejects_invalid_schema_values()
    {
        var options = new List<string> { "inspect" };
        var rules = new Dictionary<string, string> { ["color"] = "azure" };
        var definition = new FeatureDefinition("fountain", FeatureType.Fountain, "fountain", options, hintRules: rules);

        options.Add("drink");
        rules["color"] = "red";

        Assert.Equal(["inspect"], definition.InteractionOptions);
        Assert.Equal("azure", definition.HintRules["color"]);
        Assert.Throws<ArgumentException>(() => new FeatureDefinition("", FeatureType.Fountain, "fountain"));
        Assert.Throws<ArgumentException>(() => new FeatureDefinition("fountain", FeatureType.Fountain, ""));
        Assert.Throws<ArgumentOutOfRangeException>(() => new FeatureDefinition("fountain", (FeatureType)999, "fountain"));
        Assert.Throws<ArgumentException>(() => new FeatureDefinition("fountain", FeatureType.Fountain, "fountain", ["inspect", "inspect"]));
        Assert.Throws<ArgumentException>(() => new FeatureDefinition("fountain", FeatureType.Fountain, "fountain", hintRules: new Dictionary<string, string> { [" "] = "hint" }));
        Assert.Throws<ArgumentOutOfRangeException>(() => new FeatureOutcome(weight: -1));
    }

    [Fact]
    public void Activation_validates_position_and_commits_discovery_and_activation_events()
    {
        var feature = CreateFeature();
        var state = ActiveState(feature);

        var first = FeatureActivationResolver.Activate(state, new ActivateFeatureCommand(feature.InstanceId));

        var activated = Assert.Single(first.State.Dungeon.Features);
        Assert.True(activated.Discovered);
        Assert.Equal(1, activated.ActivationCount);
        Assert.Collection(
            first.Events,
            discovered => Assert.Equal(new FeatureDiscoveredEvent(feature.InstanceId, feature.Position), discovered),
            activatedEvent => Assert.Equal(new FeatureActivatedEvent(feature.InstanceId, feature.Position, 1), activatedEvent));

        var second = FeatureActivationResolver.Activate(first.State, new ActivateFeatureCommand(feature.InstanceId));
        Assert.DoesNotContain(second.Events, domainEvent => domainEvent is FeatureDiscoveredEvent);
        Assert.Equal(2, Assert.Single(second.State.Dungeon.Features).ActivationCount);
    }

    [Fact]
    public void Generic_outcome_activation_commits_opaque_outcome_without_content_specific_state_changes()
    {
        var feature = CreateFeature();
        var state = ActiveState(feature) with
        {
            Player = new PlayerState
            {
                Position = feature.Position,
                SpellPower = 2,
                MaxSpellPower = 9
            }
        };

        var result = FeatureActivationResolver.Activate(
            state,
            new ActivateFeatureCommand(feature.InstanceId),
            new FeatureOutcomeResolution(["configured_effect"], ["observed_result"]));

        Assert.Equal(2, result.State.Player.SpellPower);
        Assert.Equal(1, Assert.Single(result.State.Dungeon.Features).ActivationCount);
        var outcome = Assert.IsType<FeatureOutcomeResolvedEvent>(result.Events[^1]);
        Assert.Equal(["configured_effect"], outcome.Effects);
        Assert.Equal(["observed_result"], outcome.Observations);
    }

    [Fact]
    public void Equal_activation_inputs_replay_to_equal_feature_state_and_events()
    {
        var feature = CreateFeature();
        var command = new ActivateFeatureCommand(feature.InstanceId);

        var first = FeatureActivationResolver.Activate(ActiveState(feature), command);
        var second = FeatureActivationResolver.Activate(ActiveState(feature), command);

        Assert.Equal(first.Events, second.Events);
        Assert.Equal(first.State.Dungeon.Features, second.State.Dungeon.Features);
    }

    [Fact]
    public void Activation_rejects_inactive_dead_combat_wrong_position_and_unknown_features()
    {
        var feature = CreateFeature();
        var state = ActiveState(feature);
        var command = new ActivateFeatureCommand(feature.InstanceId);

        Assert.Throws<InvalidOperationException>(() => FeatureActivationResolver.Activate(state, new ActivateFeatureCommand(Guid.NewGuid())));
        Assert.Throws<InvalidOperationException>(() => FeatureActivationResolver.Activate(state with { Expedition = new ExpeditionState() }, command));
        Assert.Throws<InvalidOperationException>(() => FeatureActivationResolver.Activate(state with { Player = new PlayerState { Alive = false, Position = feature.Position } }, command));
        Assert.Throws<InvalidOperationException>(() => FeatureActivationResolver.Activate(
            state with
            {
                Combat = new Telengard.Core.Combat.CombatState(
                    new Telengard.Core.Combat.MonsterInstance(Guid.NewGuid(), "rat", 1, 1, feature.Position),
                    Telengard.Core.Combat.CombatPhase.PlayerAction)
            },
            command));
        Assert.Throws<InvalidOperationException>(() => FeatureActivationResolver.Activate(state with { Player = new PlayerState { Position = new DungeonPosition(1, 1, 1) } }, command));
        Assert.Throws<ArgumentException>(() => new ActivateFeatureCommand(Guid.Empty));
    }

    [Fact]
    public void Activation_state_round_trips_through_explicit_save_dto()
    {
        var feature = CreateFeature();
        var state = FeatureActivationResolver.Activate(ActiveState(feature), new ActivateFeatureCommand(feature.InstanceId)).State;
        var restored = SaveGameSerializer.Deserialize(SaveGameSerializer.Serialize(state));

        Assert.Equal(state.Dungeon.Features, restored.Dungeon.Features);
        Assert.Equal(GameState.CurrentSaveVersion, restored.SaveVersion);
    }

    private static FeatureInstance CreateFeature() => new(
        Guid.Parse("00000000-0000-0000-0000-000000000040"),
        "azure-fountain",
        new DungeonPosition(1, 0, 0));

    private static GameState ActiveState(FeatureInstance feature) => GameState.Create(1234) with
    {
        Player = new PlayerState { Position = feature.Position },
        Expedition = new ExpeditionState { Active = true, ExpeditionId = Guid.Parse("00000000-0000-0000-0000-000000000041") },
        Dungeon = new DungeonState { Features = [feature] }
    };
}
