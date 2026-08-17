using Telengard.Core.Combat;
using Telengard.Core.Simulation;
using Telengard.Save;
using Xunit;

namespace Telengard.Architecture.Tests;

public sealed class ThreatAssessmentTests
{
    [Fact]
    public void Classification_exposes_only_configured_approximate_levels()
    {
        var player = new PlayerState { Level = 1 };
        var configuration = new ThreatClassificationConfiguration(0, 2, ["rat"]);

        Assert.Equal(ThreatLevel.Trivial, ThreatAssessmentResolver.Classify(Monster("rat", 1), player, configuration));
        Assert.Equal(ThreatLevel.Dangerous, ThreatAssessmentResolver.Classify(Monster("rat", 2), player, configuration));
        Assert.Equal(ThreatLevel.Deadly, ThreatAssessmentResolver.Classify(Monster("rat", 3), player, configuration));
        Assert.Equal(ThreatLevel.Unknown, ThreatAssessmentResolver.Classify(Monster("unknown", 3), player, configuration));
    }

    [Fact]
    public void Assessment_commits_player_action_state_before_emitting_the_category()
    {
        var state = ActiveCombat();
        state = CombatStateResolver.Advance(state, new AdvanceCombatCommand()).State;

        var result = ThreatAssessmentResolver.Resolve(
            state,
            new AssessThreatCommand(),
            new ThreatClassificationConfiguration(0, 2, ["rat"]));

        Assert.Equal(CombatPhase.PlayerAction, result.State.Combat!.Phase);
        Assert.Equal(ThreatLevel.Trivial, result.State.Combat.ThreatLevel);
        var assessed = Assert.IsType<ThreatAssessedEvent>(Assert.Single(result.Events));
        Assert.Equal(result.State.Combat.EncounterId, assessed.EncounterId);
        Assert.Equal(ThreatLevel.Trivial, assessed.Level);
    }

    [Fact]
    public void Threat_assessment_validates_active_living_and_phase_boundaries()
    {
        var command = new AssessThreatCommand();
        var configuration = new ThreatClassificationConfiguration(0, 2, ["rat"]);

        Assert.Throws<ArgumentNullException>(() => ThreatAssessmentResolver.Resolve(null!, command, configuration));
        Assert.Throws<ArgumentNullException>(() => ThreatAssessmentResolver.Resolve(ActiveCombat(), null!, configuration));
        Assert.Throws<ArgumentNullException>(() => ThreatAssessmentResolver.Resolve(ActiveCombat(), command, null!));
        Assert.Throws<InvalidOperationException>(() => ThreatAssessmentResolver.Resolve(
            ActiveCombat() with
            {
                Expedition = new ExpeditionState { Active = false },
                Combat = ActiveCombat().Combat! with { Phase = CombatPhase.ThreatAssessment }
            }, command, configuration));
        Assert.Throws<InvalidOperationException>(() => ThreatAssessmentResolver.Resolve(
            ActiveCombat() with
            {
                Player = ActiveCombat().Player with { Alive = false },
                Combat = ActiveCombat().Combat! with { Phase = CombatPhase.ThreatAssessment }
            }, command, configuration));
        Assert.Throws<InvalidOperationException>(() => ThreatAssessmentResolver.Resolve(
            ActiveCombat(), command, configuration));
        Assert.Throws<InvalidOperationException>(() => ThreatAssessmentResolver.Resolve(
            ActiveCombat() with { Combat = null }, command, configuration));
    }

    [Fact]
    public void Equal_assessment_inputs_replay_to_equal_state_and_events()
    {
        var first = Assess(ActiveCombat());
        var second = Assess(ActiveCombat());

        Assert.Equal(first.State.Combat, second.State.Combat);
        Assert.Equal(first.Events, second.Events);
    }

    [Fact]
    public void Classification_rejects_null_boundaries()
    {
        var monster = Monster("rat", 1);
        var player = new PlayerState { Level = 1 };
        var configuration = new ThreatClassificationConfiguration(0, 2, ["rat"]);

        Assert.Throws<ArgumentNullException>(() => ThreatAssessmentResolver.Classify(null!, player, configuration));
        Assert.Throws<ArgumentNullException>(() => ThreatAssessmentResolver.Classify(monster, null!, configuration));
        Assert.Throws<ArgumentNullException>(() => ThreatAssessmentResolver.Classify(monster, player, null!));
    }

    [Fact]
    public void Configuration_validates_thresholds_and_copies_known_definitions()
    {
        Assert.Throws<ArgumentException>(() => new ThreatClassificationConfiguration(2, 2));
        Assert.Throws<ArgumentException>(() => new ThreatClassificationConfiguration(2, 1));
        Assert.Throws<ArgumentException>(() => new ThreatClassificationConfiguration(0, 2, [""]));

        var known = new List<string> { "rat" };
        var configuration = new ThreatClassificationConfiguration(0, 2, known);
        known.Add("dragon");

        Assert.Equal(["rat"], configuration.KnownMonsterDefinitionIds);
        Assert.Empty(new ThreatClassificationConfiguration(0, 2).KnownMonsterDefinitionIds);
    }

    [Fact]
    public void Classified_combat_round_trips_through_the_explicit_save_contract()
    {
        var state = Assess(ActiveCombat()).State;
        var restored = SaveGameSerializer.Deserialize(SaveGameSerializer.Serialize(state));

        Assert.Equal(SaveGameSerializer.Serialize(state), SaveGameSerializer.Serialize(restored));
        Assert.Equal(ThreatLevel.Trivial, restored.Combat!.ThreatLevel);
    }

    private static CommandResult Assess(GameState state) => ThreatAssessmentResolver.Resolve(
        CombatStateResolver.Advance(state, new AdvanceCombatCommand()).State,
        new AssessThreatCommand(),
        new ThreatClassificationConfiguration(0, 2, ["rat"]));

    private static GameState ActiveCombat()
    {
        var state = GameState.Create(1234) with
        {
            Expedition = new ExpeditionState { Active = true },
            Player = new PlayerState { Position = new DungeonPosition(1, 0, 0) }
        };
        return state with
        {
            Combat = CombatStateResolver.Begin(Monster("rat", 1))
        };
    }

    private static MonsterInstance Monster(string definitionId, int level) => new(
        Guid.Parse("00000000-0000-0000-0000-000000000001"),
        definitionId,
        level,
        3,
        new DungeonPosition(1, 0, 0));
}
