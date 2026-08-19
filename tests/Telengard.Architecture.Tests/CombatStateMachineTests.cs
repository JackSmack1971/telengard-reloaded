using Telengard.Core.Combat;
using Telengard.Core.Simulation;
using Telengard.Core.World.Generation;
using Telengard.Save;
using Xunit;

namespace Telengard.Architecture.Tests;

public sealed class CombatStateMachineTests
{
    [Fact]
    public void Encounter_trigger_commits_contact_combat_state_before_event_delivery()
    {
        var layout = new FloorLayoutGenerator().Generate(1234, "generator-1", 1);
        var entered = DungeonWalkingResolver.Enter(GameState.Create(1234), new EnterDungeonCommand(), layout);
        var position = FindWalkableNeighbor(layout, entered.State.Player.Position);
        var state = entered.State with { Player = entered.State.Player with { Position = position } };

        var result = EncounterTriggerResolver.Evaluate(
            state,
            position,
            new EncounterTriggerConfiguration(1, [new EncounterSpawnOption("rat", 1, 3)]));

        var encounter = Assert.Single(result.Events.OfType<EncounterStartedEvent>());
        Assert.NotNull(result.State.Combat);
        Assert.Equal(CombatPhase.Contact, result.State.Combat!.Phase);
        Assert.Equal(encounter.Monster.InstanceId, result.State.Combat.EncounterId);
    }

    [Fact]
    public void Combat_advances_through_the_defined_lifecycle_and_repeats_rounds()
    {
        var state = ActiveCombat();

        state = CombatStateResolver.Advance(state, new AdvanceCombatCommand()).State;
        Assert.Equal(CombatPhase.ThreatAssessment, state.Combat!.Phase);
        var assessment = ThreatAssessmentResolver.Resolve(
            state,
            new AssessThreatCommand(),
            new ThreatClassificationConfiguration(0, 2, ["rat"]));
        state = assessment.State;
        Assert.Equal(CombatPhase.PlayerAction, state.Combat!.Phase);
        Assert.Equal(ThreatLevel.Trivial, state.Combat.ThreatLevel);
        Assert.Equal(ThreatLevel.Trivial, Assert.IsType<ThreatAssessedEvent>(Assert.Single(assessment.Events)).Level);

        var selected = CombatStateResolver.SelectAction(
            state,
            new SelectCombatActionCommand(CombatAction.Defend));
        Assert.Equal(CombatPhase.Resolution, selected.State.Combat!.Phase);
        Assert.Equal(CombatAction.Defend, selected.State.Combat.SelectedAction);
        var selectedEvent = Assert.IsType<CombatPhaseChangedEvent>(Assert.Single(selected.Events));
        Assert.Equal(CombatAction.Defend, selectedEvent.Action);
        Assert.Equal(state.Combat.EncounterId, selectedEvent.EncounterId);
        Assert.Equal(state.Combat.Round, selectedEvent.Round);

        state = DefendResolver.Resolve(selected.State, new DefendCommand()).State;
        Assert.Equal(CombatPhase.EnemyAction, state.Combat!.Phase);
        Assert.Equal(CombatAction.Defend, state.Combat.SelectedAction);
        var stateCheck = CombatStateResolver.Advance(state, new AdvanceCombatCommand());
        state = stateCheck.State;
        Assert.Equal(CombatPhase.StateCheck, state.Combat!.Phase);
        Assert.Equal(CombatAction.Defend, state.Combat.SelectedAction);
        Assert.Equal(ThreatLevel.Trivial, state.Combat.ThreatLevel);
        var stateCheckEvent = Assert.IsType<CombatPhaseChangedEvent>(Assert.Single(stateCheck.Events));
        Assert.Equal(CombatPhase.EnemyAction, stateCheckEvent.From);
        Assert.Equal(CombatPhase.StateCheck, stateCheckEvent.To);
        state = CombatStateResolver.Advance(state, new AdvanceCombatCommand()).State;
        Assert.Equal(CombatPhase.PlayerAction, state.Combat!.Phase);
        Assert.Equal(2, state.Combat.Round);
        Assert.Null(state.Combat.SelectedAction);
    }

    [Fact]
    public void Combat_commands_validate_active_state_and_phase()
    {
        var initial = GameState.Create(1234);
        Assert.Throws<InvalidOperationException>(() => CombatStateResolver.Advance(initial, new AdvanceCombatCommand()));
        Assert.Throws<InvalidOperationException>(() => CombatStateResolver.SelectAction(
            initial,
            new SelectCombatActionCommand(CombatAction.Attack)));

        var state = ActiveCombat();
        Assert.Throws<InvalidOperationException>(() => CombatStateResolver.SelectAction(
            state,
            new SelectCombatActionCommand(CombatAction.Attack)));
        Assert.Throws<InvalidOperationException>(() => CombatStateResolver.Advance(
            state with { Combat = state.Combat! with { Phase = CombatPhase.ThreatAssessment } },
            new AdvanceCombatCommand()));
        Assert.Throws<ArgumentOutOfRangeException>(() => CombatStateResolver.SelectAction(
            state with { Combat = state.Combat! with { Phase = CombatPhase.PlayerAction } },
            new SelectCombatActionCommand((CombatAction)999)));
        Assert.Throws<InvalidOperationException>(() => CombatStateResolver.Advance(
            state with { Combat = state.Combat! with { Phase = CombatPhase.PlayerAction } },
            new AdvanceCombatCommand()));
        Assert.Throws<InvalidOperationException>(() => CombatStateResolver.Advance(
            state with { Player = state.Player with { Alive = false } },
            new AdvanceCombatCommand()));
        Assert.Throws<InvalidOperationException>(() => CombatStateResolver.Advance(
            state with { Expedition = new ExpeditionState { Active = true }, Combat = null },
            new AdvanceCombatCommand()));
        Assert.Throws<InvalidOperationException>(() => CombatStateResolver.Advance(
            state with { Expedition = new ExpeditionState { Active = false } },
            new AdvanceCombatCommand()));
    }

    [Fact]
    public void Combat_constructor_and_advance_cover_metadata_and_phase_boundaries()
    {
        var monster = ActiveCombat().Combat!.Monster;

        Assert.Throws<ArgumentNullException>(() => new CombatState(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => new CombatState(monster, (CombatPhase)999));
        Assert.Throws<ArgumentOutOfRangeException>(() => new CombatState(monster, round: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new CombatState(monster, selectedAction: (CombatAction)999));
        Assert.Throws<ArgumentException>(() => new CombatState(monster, CombatPhase.PlayerAction, selectedAction: CombatAction.Attack));
        Assert.Throws<ArgumentOutOfRangeException>(() => new CombatState(monster, threatLevel: (ThreatLevel)999));
        Assert.Throws<ArgumentException>(() => new CombatState(monster, CombatPhase.ThreatAssessment, threatLevel: ThreatLevel.Trivial));
        Assert.All(new[] { CombatPhase.Searching, CombatPhase.Contact, CombatPhase.ThreatAssessment }, phase =>
            Assert.Throws<ArgumentException>(() => new CombatState(monster, phase, selectedAction: CombatAction.Attack)));
        Assert.All(new[] { CombatPhase.Searching, CombatPhase.Contact }, phase =>
            Assert.Throws<ArgumentException>(() => new CombatState(monster, phase, threatLevel: ThreatLevel.Trivial)));

        var searching = ActiveCombat() with
        {
            Combat = new CombatState(monster, CombatPhase.Searching)
        };
        Assert.Equal(CombatPhase.Contact, CombatStateResolver.Advance(searching, new AdvanceCombatCommand()).State.Combat!.Phase);

        var resolution = ActiveCombat() with
        {
            Combat = new CombatState(monster, CombatPhase.Resolution)
        };
        Assert.Equal(CombatPhase.EnemyAction, CombatStateResolver.Advance(resolution, new AdvanceCombatCommand()).State.Combat!.Phase);

        Assert.Throws<ArgumentNullException>(() => CombatStateResolver.Begin(null!));
        Assert.Throws<ArgumentNullException>(() => CombatStateResolver.Advance(null!, new AdvanceCombatCommand()));
        Assert.Throws<ArgumentNullException>(() => CombatStateResolver.Advance(ActiveCombat(), null!));
        Assert.Throws<ArgumentNullException>(() => CombatStateResolver.SelectAction(null!, new SelectCombatActionCommand(CombatAction.Attack)));
        Assert.Throws<ArgumentNullException>(() => CombatStateResolver.SelectAction(ActiveCombat(), null!));
    }

    [Fact]
    public void Combat_preserves_threat_and_clears_action_at_the_round_boundary()
    {
        var state = ActiveCombat() with
        {
            Combat = new CombatState(
                ActiveCombat().Combat!.Monster,
                CombatPhase.StateCheck,
                1,
                CombatAction.Defend,
                ThreatLevel.Dangerous)
        };

        var result = CombatStateResolver.Advance(state, new AdvanceCombatCommand());

        Assert.Equal(CombatPhase.PlayerAction, result.State.Combat!.Phase);
        Assert.Null(result.State.Combat.SelectedAction);
        Assert.Equal(ThreatLevel.Dangerous, result.State.Combat.ThreatLevel);

        Assert.Throws<OverflowException>(() => CombatStateResolver.Advance(
            state with { Combat = state.Combat! with { Round = int.MaxValue } },
            new AdvanceCombatCommand()));
    }

    [Fact]
    public void Equal_combat_inputs_replay_to_equal_state_and_events()
    {
        var first = CombatStateResolver.Advance(ActiveCombat(), new AdvanceCombatCommand());
        var second = CombatStateResolver.Advance(ActiveCombat(), new AdvanceCombatCommand());

        Assert.Equal(first.State.Combat!.Phase, second.State.Combat!.Phase);
        Assert.Equal(first.State.Combat.Round, second.State.Combat.Round);
        Assert.Equal(first.Events, second.Events);
    }

    [Fact]
    public void Active_combat_round_trips_through_explicit_save_state()
    {
        var state = ActiveCombat() with
        {
            Combat = new CombatState(
                ActiveCombat().Combat!.Monster,
                CombatPhase.Resolution,
                2,
                CombatAction.Defend)
        };

        var roundTrip = SaveGameSerializer.Deserialize(SaveGameSerializer.Serialize(state));

        Assert.Equal(SaveGameSerializer.Serialize(state), SaveGameSerializer.Serialize(roundTrip));
        Assert.Equal(state.Combat!.EncounterId, roundTrip.Combat!.EncounterId);
        Assert.Equal(state.Combat.Phase, roundTrip.Combat.Phase);
        Assert.Equal(state.Combat.Round, roundTrip.Combat.Round);
        Assert.Equal(state.Combat.SelectedAction, roundTrip.Combat.SelectedAction);
        Assert.Equal(state.Combat.Monster.DefinitionId, roundTrip.Combat.Monster.DefinitionId);
    }

    private static GameState ActiveCombat()
    {
        var state = GameState.Create(1234) with
        {
            Expedition = new ExpeditionState { Active = true, FloorsVisited = [1] },
            Inn = new InnState { IsAtInn = false },
            Player = new PlayerState
            {
                Position = new DungeonPosition(1, 0, 0),
                HitPoints = 1,
                MaxHitPoints = 1
            }
        };
        return state with
        {
            Combat = CombatStateResolver.Begin(new MonsterInstance(
                Guid.Parse("00000000-0000-0000-0000-000000000001"),
                "rat",
                1,
                3,
                state.Player.Position))
        };
    }

    private static DungeonPosition FindWalkableNeighbor(FloorLayout layout, DungeonPosition origin)
    {
        foreach (var direction in Enum.GetValues<MovementDirection>())
        {
            var candidate = direction switch
            {
                MovementDirection.North => new DungeonPosition(origin.Floor, origin.X, origin.Y - 1),
                MovementDirection.South => new DungeonPosition(origin.Floor, origin.X, origin.Y + 1),
                MovementDirection.East => new DungeonPosition(origin.Floor, origin.X + 1, origin.Y),
                MovementDirection.West => new DungeonPosition(origin.Floor, origin.X - 1, origin.Y),
                _ => throw new ArgumentOutOfRangeException(nameof(direction))
            };
            if (candidate.X >= 0 && candidate.X < layout.Width && candidate.Y >= 0 && candidate.Y < layout.Height && layout.IsWalkable(candidate))
            {
                return candidate;
            }
        }

        throw new InvalidOperationException("The generated layout has no walkable neighbor.");
    }
}
