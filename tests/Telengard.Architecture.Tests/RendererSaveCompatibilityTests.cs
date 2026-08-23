using Telengard.Core.Combat;
using Telengard.Core.Knowledge;
using Telengard.Core.Presentation;
using Telengard.Core.Simulation;
using Telengard.Core.World.Features;
using Telengard.Core.World.Generation;
using Telengard.Save;
using Telengard.Terminal;
using Xunit;

namespace Telengard.Architecture.Tests;

public sealed class RendererSaveCompatibilityTests
{
    [Fact]
    public void Save_reload_preserves_modern_and_terminal_projections_without_exposing_hidden_state()
    {
        var layout = new FloorLayoutGenerator().Generate(1234, "generator-1", 1);
        var enterDispatcher = new CommandDispatcher(GameState.Create(1234, mode: GameMode.Legacy));
        enterDispatcher.Register<EnterDungeonCommand>((current, command) =>
            DungeonWalkingResolver.Enter(current, command, layout));
        var entered = enterDispatcher.Dispatch(new EnterDungeonCommand());
        var encounterConfiguration = new EncounterTriggerConfiguration(
            1,
            [new EncounterSpawnOption("deep-watcher", level: 4, currentHitPoints: 7)]);
        var destination = FindWalkableNeighbor(layout, entered.State.Player.Position);
        var moveDispatcher = new CommandDispatcher(entered.State);
        moveDispatcher.Register<MoveCommand>((current, command) =>
            DungeonWalkingResolver.Move(current, command, layout, encounterConfiguration));
        var encounter = moveDispatcher.Dispatch(new MoveCommand(
            DirectionBetween(entered.State.Player.Position, destination)));
        var position = encounter.State.Player.Position;
        var observed = new DungeonPosition(1, 0, 0);
        var visited = new DungeonPosition(1, 0, 0);
        var discoveredFeatureId = Guid.Parse("00000000-0000-0000-0000-000000000010");
        var hiddenFeatureId = Guid.Parse("00000000-0000-0000-0000-000000000011");
        var state = encounter.State with
        {
            SimulationTick = 42,
            Inn = new InnState { IsAtInn = false },
            Player = new PlayerState
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                Attributes = new(1, 2, 3, 4, 5, 6),
                Position = position,
                Level = 3,
                HitPoints = 8,
                MaxHitPoints = 10,
                SpellPower = 4,
                MaxSpellPower = 6,
                Inventory = ["potion"],
                CarriedGold = 17
            },
            Expedition = entered.State.Expedition with
            {
                DeepestFloorReached = 1,
                CarriedGold = 17,
                FloorsVisited = [1]
            },
            SecuredProgress = new SecuredProgressState { SecuredGold = 23 },
            Legacy = new LegacyState
            {
                PersistentMap = new PersistentMapState([observed, position], [visited])
            },
            Dungeon = new DungeonState
            {
                Features =
                [
                    new FeatureInstance(discoveredFeatureId, "fountain", observed, discovered: true),
                    new FeatureInstance(hiddenFeatureId, "secret-altar", position)
                ]
            },
            Knowledge = new KnowledgeState(
                [new KnowledgeEntry(
                    "feature:fountain",
                    observations: ["restores something"],
                    sampleCount: 1,
                    confidence: 50)]),
            Combat = encounter.State.Combat! with
            {
                Monster = new MonsterInstance(
                    encounter.State.Combat.Monster.InstanceId,
                    encounter.State.Combat.Monster.DefinitionId,
                    encounter.State.Combat.Monster.Level,
                    encounter.State.Combat.Monster.CurrentHitPoints,
                    encounter.State.Combat.Monster.Position,
                    temporaryEffects: ["hidden-effect"],
                    currentBehaviorState: "ambush"),
                Phase = CombatPhase.EnemyAction,
                Round = 2,
                SelectedAction = CombatAction.Defend,
                ThreatLevel = ThreatLevel.Deadly
            }
        };
        var events = entered.Events.Concat(encounter.Events).ToArray();
        var originalSave = SaveGameSerializer.Serialize(state);

        var expectedPresentation = PresentationStateAdapter.Create(state);
        var expectedModern = ModernRenderer.Create(expectedPresentation, events);
        var expectedTerminal = TerminalRenderer.Render(expectedPresentation, events);
        var reloaded = SaveGameSerializer.Deserialize(originalSave);
        var actualPresentation = PresentationStateAdapter.Create(reloaded);
        var actualModern = ModernRenderer.Create(actualPresentation, events);
        var actualTerminal = TerminalRenderer.Render(actualPresentation, events);
        var originalContinuation = ContinueCombat(state);
        var reloadedContinuation = ContinueCombat(reloaded);

        Assert.Collection(
            events,
            domainEvent => Assert.IsType<DungeonEnteredEvent>(domainEvent),
            domainEvent => Assert.IsType<ExpeditionStartedEvent>(domainEvent),
            domainEvent => Assert.IsType<PlayerMovedEvent>(domainEvent),
            domainEvent => Assert.IsType<EncounterStartedEvent>(domainEvent));
        var movedEvent = Assert.IsType<PlayerMovedEvent>(events[2]);
        Assert.Equal(entered.State.Player.Position, movedEvent.From);
        Assert.Equal(state.Player.Position, movedEvent.To);
        var encounterEvent = Assert.IsType<EncounterStartedEvent>(events[3]);
        var committedMonster = encounter.State.Combat!.Monster;
        Assert.Equal(committedMonster, encounterEvent.Monster);
        Assert.Equal(committedMonster.InstanceId, encounterEvent.Monster.InstanceId);
        Assert.Equal(committedMonster.DefinitionId, encounterEvent.Monster.DefinitionId);
        Assert.Equal(committedMonster.Level, encounterEvent.Monster.Level);
        Assert.Equal(committedMonster.CurrentHitPoints, encounterEvent.Monster.CurrentHitPoints);
        Assert.Equal(committedMonster.Position, encounterEvent.Monster.Position);
        Assert.Equal(originalSave, SaveGameSerializer.Serialize(reloaded));
        Assert.Equal(
            SaveGameSerializer.Serialize(originalContinuation.State),
            SaveGameSerializer.Serialize(reloadedContinuation.State));
        Assert.Equal(originalContinuation.Events, reloadedContinuation.Events);
        Assert.Equal(expectedTerminal, actualTerminal);
        Assert.Equal(expectedModern.Scene, actualModern.Scene);
        Assert.Equal(expectedModern.PlayerPosition, actualModern.PlayerPosition);
        Assert.Equal(expectedModern.Environment, actualModern.Environment);
        Assert.Equal(expectedModern.Tiles, actualModern.Tiles);
        Assert.Equal(expectedModern.Features, actualModern.Features);
        Assert.Equal(expectedModern.Hud, actualModern.Hud);
        Assert.Equal(expectedModern.Combat, actualModern.Combat);
        Assert.Equal(expectedModern.Cues, actualModern.Cues);
        Assert.Equal(state.Combat!.Monster.DefinitionId, actualModern.Combat!.Monster.DefinitionId);
        Assert.Equal(state.Combat.Monster.CurrentHitPoints, actualModern.Combat.Monster.CurrentHitPoints);
        Assert.Equal(state.Combat.Monster.Position, actualModern.Combat.Monster.Position);
        Assert.Equal(
            [ModernCueKind.DungeonEntered, ModernCueKind.PlayerMoved, ModernCueKind.CombatStarted],
            actualModern.Cues.Select(cue => cue.Kind));
        Assert.Equal(new ModernCue(ModernCueKind.DungeonEntered, entered.State.Player.Position), actualModern.Cues[0]);
        Assert.Equal(new ModernCue(ModernCueKind.PlayerMoved, position), actualModern.Cues[1]);
        Assert.Equal(new ModernCue(ModernCueKind.CombatStarted), actualModern.Cues[2]);
        Assert.DoesNotContain(typeof(ModernMonsterMarker).GetProperties(), property =>
            property.Name is nameof(MonsterInstance.Level) or
                nameof(MonsterInstance.TemporaryEffects) or
                nameof(MonsterInstance.CurrentBehaviorState));
        Assert.Contains("EVENT dungeon_entered", actualTerminal);
        Assert.Contains("EVENT player_moved", actualTerminal);
        Assert.Contains("EVENT encounter_started", actualTerminal);
        Assert.True(actualTerminal.IndexOf("EVENT dungeon_entered", StringComparison.Ordinal) <
            actualTerminal.IndexOf("EVENT player_moved", StringComparison.Ordinal));
        Assert.True(actualTerminal.IndexOf("EVENT player_moved", StringComparison.Ordinal) <
            actualTerminal.IndexOf("EVENT encounter_started", StringComparison.Ordinal));
        Assert.DoesNotContain(actualPresentation.DiscoveredFeatures, feature => feature.InstanceId == hiddenFeatureId);
        Assert.DoesNotContain(hiddenFeatureId.ToString("N"), actualTerminal, StringComparison.Ordinal);
        Assert.DoesNotContain("hidden-effect", actualTerminal, StringComparison.Ordinal);
        Assert.DoesNotContain("ambush", actualTerminal, StringComparison.Ordinal);
        Assert.DoesNotContain("level=4", actualTerminal, StringComparison.Ordinal);
        Assert.Equal(originalSave, SaveGameSerializer.Serialize(state));
    }

    private static CommandDispatcher CreateCombatDispatcher(GameState state)
    {
        var dispatcher = new CommandDispatcher(state);
        dispatcher.Register<AdvanceCombatCommand>(CombatStateResolver.Advance);
        dispatcher.Register<SelectCombatActionCommand>(CombatStateResolver.SelectAction);
        dispatcher.Register<FleeCommand>((current, command) =>
            FleeResolver.Resolve(current, command, new FleeConfiguration(1)));
        return dispatcher;
    }

    private static (GameState State, IReadOnlyList<IDomainEvent> Events) ContinueCombat(GameState state)
    {
        var dispatcher = CreateCombatDispatcher(state);
        var events = new List<IDomainEvent>();

        foreach (var command in new ICommand[]
        {
            new AdvanceCombatCommand(),
            new AdvanceCombatCommand(),
            new SelectCombatActionCommand(CombatAction.Flee),
            new FleeCommand()
        })
        {
            var result = command switch
            {
                AdvanceCombatCommand advance => dispatcher.Dispatch(advance),
                SelectCombatActionCommand select => dispatcher.Dispatch(select),
                FleeCommand flee => dispatcher.Dispatch(flee),
                _ => throw new ArgumentOutOfRangeException(nameof(command))
            };
            events.AddRange(result.Events);
        }

        return (dispatcher.CurrentState, events);
    }

    private static DungeonPosition FindWalkableNeighbor(FloorLayout layout, DungeonPosition origin)
    {
        foreach (var direction in Enum.GetValues<MovementDirection>())
        {
            var candidate = Offset(origin, direction);
            if (candidate.X >= 0 && candidate.X < layout.Width &&
                candidate.Y >= 0 && candidate.Y < layout.Height && layout.IsWalkable(candidate))
            {
                return candidate;
            }
        }

        throw new InvalidOperationException("The generated layout has no walkable neighbor.");
    }

    private static DungeonPosition Offset(DungeonPosition position, MovementDirection direction) => direction switch
    {
        MovementDirection.North => new(position.Floor, position.X, position.Y - 1),
        MovementDirection.South => new(position.Floor, position.X, position.Y + 1),
        MovementDirection.East => new(position.Floor, position.X + 1, position.Y),
        MovementDirection.West => new(position.Floor, position.X - 1, position.Y),
        _ => throw new ArgumentOutOfRangeException(nameof(direction))
    };

    private static MovementDirection DirectionBetween(DungeonPosition from, DungeonPosition to) =>
        (to.X - from.X, to.Y - from.Y) switch
        {
            (0, -1) => MovementDirection.North,
            (0, 1) => MovementDirection.South,
            (1, 0) => MovementDirection.East,
            (-1, 0) => MovementDirection.West,
            _ => throw new ArgumentException("Positions must be adjacent.", nameof(to))
        };
}
