using Telengard.Core.Combat;
using Telengard.Core.Events;
using Telengard.Core.Simulation;
using Telengard.Core.World.Generation;
using Xunit;

namespace Telengard.Architecture.Tests;

public sealed class Phase3AcceptanceTests
{
    [Fact]
    public void Exploration_command_commits_encounter_state_before_publishing_start()
    {
        var layout = new FloorLayoutGenerator().Generate(1234, "generator-1", 1);
        var bus = new DomainEventBus();
        CommandDispatcher? dispatcher = null;
        var observedAfterCommit = false;
        bus.Subscribe<EncounterStartedEvent>(encounter =>
            observedAfterCommit = dispatcher!.CurrentState.Combat?.EncounterId == encounter.Monster.InstanceId);

        dispatcher = new CommandDispatcher(GameState.Create(1234), bus);
        dispatcher.Register<EnterDungeonCommand>((state, command) =>
            DungeonWalkingResolver.Enter(state, command, layout));
        dispatcher.Register<MoveCommand>((state, command) =>
            DungeonWalkingResolver.Move(
                state,
                command,
                layout,
                new EncounterTriggerConfiguration(1, [new EncounterSpawnOption("rat", 1, 3)])));

        dispatcher.Dispatch(new EnterDungeonCommand());
        var origin = dispatcher.CurrentState.Player.Position;
        var destination = FindWalkableNeighbor(layout, origin);

        var result = dispatcher.Dispatch(new MoveCommand(DirectionBetween(origin, destination)));

        var encounter = Assert.Single(result.Events.OfType<EncounterStartedEvent>());
        Assert.True(observedAfterCommit);
        Assert.Equal(destination, result.State.Player.Position);
        Assert.Equal(CombatPhase.Contact, result.State.Combat!.Phase);
        Assert.Equal(encounter.Monster.InstanceId, result.State.Combat.EncounterId);
        Assert.Equal("rat", result.State.Combat.Monster.DefinitionId);
        Assert.Equal(1, result.State.Combat.Monster.Level);
        Assert.Equal(3, result.State.Combat.Monster.CurrentHitPoints);
        Assert.Equal(destination, result.State.Combat.Monster.Position);
    }

    [Fact]
    public void Public_combat_commands_cover_defend_attack_flee_and_termination()
    {
        var dispatcher = new CommandDispatcher(ActiveCombat());
        dispatcher.Register<AdvanceCombatCommand>(CombatStateResolver.Advance);
        dispatcher.Register<AssessThreatCommand>((state, command) => ThreatAssessmentResolver.Resolve(
            state,
            command,
            new ThreatClassificationConfiguration(0, 2, ["rat"])));
        dispatcher.Register<SelectCombatActionCommand>(CombatStateResolver.SelectAction);
        dispatcher.Register<DefendCommand>(DefendResolver.Resolve);
        dispatcher.Register<AttackCommand>((state, command) => AttackResolver.Resolve(
            state,
            command,
            new AttackConfiguration(2)));
        dispatcher.Register<FleeCommand>((state, command) => FleeResolver.Resolve(
            state,
            command,
            new FleeConfiguration(1)));

        dispatcher.Dispatch(new AdvanceCombatCommand());
        dispatcher.Dispatch(new AssessThreatCommand());
        dispatcher.Dispatch(new SelectCombatActionCommand(CombatAction.Defend));
        dispatcher.Dispatch(new DefendCommand());
        dispatcher.Dispatch(new AdvanceCombatCommand());
        dispatcher.Dispatch(new AdvanceCombatCommand());
        dispatcher.Dispatch(new SelectCombatActionCommand(CombatAction.Attack));
        var attack = dispatcher.Dispatch(new AttackCommand());

        Assert.Equal(CombatPhase.EnemyAction, attack.State.Combat!.Phase);
        Assert.Equal(1, attack.State.Combat.Monster.CurrentHitPoints);

        dispatcher.Dispatch(new AdvanceCombatCommand());
        dispatcher.Dispatch(new AdvanceCombatCommand());
        dispatcher.Dispatch(new SelectCombatActionCommand(CombatAction.Flee));
        var flee = dispatcher.Dispatch(new FleeCommand());

        Assert.Null(flee.State.Combat);
        Assert.Equal(
            ActiveCombat().Combat!.EncounterId,
            Assert.IsType<EncounterEndedEvent>(Assert.Single(flee.Events)).EncounterId);
    }

    [Fact]
    public void Lethal_state_check_command_publishes_death_and_closes_encounter()
    {
        var dispatcher = new CommandDispatcher(ActiveCombat() with
        {
            Player = new PlayerState
            {
                Position = new DungeonPosition(1, 0, 0),
                HitPoints = 0,
                MaxHitPoints = 10,
                Alive = true
            },
            Combat = new CombatState(
                ActiveCombat().Combat!.Monster,
                CombatPhase.StateCheck,
                1)
        });
        dispatcher.Register<AdvanceCombatCommand>(CombatStateResolver.Advance);

        var result = dispatcher.Dispatch(new AdvanceCombatCommand());

        Assert.False(result.State.Player.Alive);
        Assert.False(result.State.Expedition.Active);
        Assert.Null(result.State.Combat);
        Assert.IsType<PlayerDiedEvent>(result.Events[0]);
        Assert.IsType<ExpeditionFailedEvent>(result.Events[1]);
    }

    private static GameState ActiveCombat()
    {
        var state = GameState.Create(1234) with
        {
            Inn = new InnState { IsAtInn = false },
            Expedition = new ExpeditionState { Active = true },
            Player = new PlayerState
            {
                Position = new DungeonPosition(1, 0, 0),
                Level = 1,
                HitPoints = 10,
                MaxHitPoints = 10,
                Alive = true
            }
        };

        return state with
        {
            Combat = new CombatState(
                new MonsterInstance(
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
            if (candidate.X >= 0 && candidate.X < layout.Width &&
                candidate.Y >= 0 && candidate.Y < layout.Height && layout.IsWalkable(candidate))
            {
                return candidate;
            }
        }

        throw new InvalidOperationException("The generated layout has no walkable neighbor.");
    }

    private static MovementDirection DirectionBetween(DungeonPosition from, DungeonPosition to) =>
        (to.X - from.X, to.Y - from.Y) switch
        {
            (0, -1) => MovementDirection.North,
            (0, 1) => MovementDirection.South,
            (1, 0) => MovementDirection.East,
            (-1, 0) => MovementDirection.West,
            _ => throw new ArgumentOutOfRangeException(nameof(to))
        };
}
