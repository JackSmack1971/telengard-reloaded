using System.Text.Json;
using Telengard.Core.Combat;
using Telengard.Core.Economy;
using Telengard.Core.Knowledge;
using Telengard.Core.Meta;
using Telengard.Core.Simulation;
using Telengard.Core.World.Features;
using Telengard.Core.World.Generation;
using Telengard.Save;
using Telengard.TestHarness;
using Xunit;

namespace Telengard.Architecture.Tests;

public sealed class CoreAlphaIntegrationTests
{
    [Fact]
    public void Success_path_replays_feature_encounter_treasure_knowledge_suspend_reload_and_banking()
    {
        const long seed = 1234;
        var layout = new FloorLayoutGenerator().Generate(seed, "generator-1", 1);
        var feature = new FeatureInstance(
            Guid.Parse("00000000-0000-0000-0000-000000000107"),
            "azure-fountain",
            layout.StairsUp);
        var initial = CreateReadyState(seed, GameMode.Classic) with
        {
            Dungeon = new DungeonState { Features = [feature] }
        };
        var neighbor = FindWalkableNeighbor(layout, layout.StairsUp);
        var commands = BuildSuccessCommands(layout, feature, neighbor);

        SimulationTestHarness.AssertDeterministic(
            initial,
            dispatcher => RegisterSuccessHandlers(dispatcher, layout, feature),
            commands,
            [12]);

        var checkpointed = SimulationTestHarness.Run(
            initial,
            dispatcher => RegisterSuccessHandlers(dispatcher, layout, feature),
            commands,
            [12]);
        var uninterrupted = SimulationTestHarness.Run(
            initial,
            dispatcher => RegisterSuccessHandlers(dispatcher, layout, feature),
            commands);

        Assert.Equal(uninterrupted.FinalSave, checkpointed.FinalSave);
        Assert.Equal(EventSignatures(uninterrupted.Events), EventSignatures(checkpointed.Events));

        var state = checkpointed.FinalState;
        Assert.True(state.Inn.IsAtInn);
        Assert.False(state.Expedition.Active);
        Assert.Equal(0, state.Player.CarriedGold);
        Assert.Equal(17, state.SecuredProgress.SecuredGold);
        Assert.Contains(layout.StairsUp, state.Legacy.PersistentMap.VisitedPositions);
        Assert.Contains(neighbor, state.Legacy.PersistentMap.VisitedPositions);
        var unknownWalkable = Enumerable.Range(0, layout.Width)
            .SelectMany(x => Enumerable.Range(0, layout.Height)
                .Select(y => new DungeonPosition(layout.Floor, x, y)))
            .FirstOrDefault(position => layout.IsWalkable(position) &&
                !state.Legacy.PersistentMap.ObservedPositions.Contains(position));
        Assert.NotNull(unknownWalkable);
        Assert.DoesNotContain(unknownWalkable, state.Legacy.PersistentMap.ObservedPositions);
        Assert.Equal(["amulet"], state.Player.Inventory);
        Assert.Equal(1, state.Expedition.MonstersDefeated);
        Assert.Equal(1, state.Dungeon.Features.Single().ActivationCount);
        Assert.Equal(
            ["fountain-observed"],
            state.Knowledge.Entries.Single(entry => entry.SubjectId == "feature:azure-fountain").Observations);
        Assert.Equal(
            ["observed-small"],
            state.Knowledge.Entries.Single(entry => entry.SubjectId == "monster:crypt-stalker").Observations);

        var encounterIndex = IndexOf<EncounterStartedEvent>(checkpointed.Events);
        var threatIndex = IndexOf<ThreatAssessedEvent>(checkpointed.Events);
        var featureKnowledgeIndex = IndexOfKnowledge(checkpointed.Events, "feature:azure-fountain");
        var monsterKnowledgeIndex = IndexOfKnowledge(checkpointed.Events, "monster:crypt-stalker");
        Assert.True(encounterIndex >= 0);
        Assert.True(threatIndex > encounterIndex);
        Assert.True(featureKnowledgeIndex >= 0 && featureKnowledgeIndex < encounterIndex);
        Assert.True(monsterKnowledgeIndex > threatIndex);
        Assert.DoesNotContain(
            checkpointed.Events.Skip(encounterIndex + 1).Take(monsterKnowledgeIndex - encounterIndex - 1),
            domainEvent => domainEvent is KnowledgeObservationAddedEvent);
        var encounter = Assert.IsType<EncounterStartedEvent>(
            checkpointed.Events.Single(domainEvent => domainEvent is EncounterStartedEvent));
        Assert.Equal("crypt-stalker", encounter.Monster.DefinitionId);
        Assert.Equal(ThreatLevel.Unknown, Assert.IsType<ThreatAssessedEvent>(
            checkpointed.Events.Single(domainEvent => domainEvent is ThreatAssessedEvent)).Level);
        var treasureIndex = IndexOf<TreasureAcquiredEvent>(checkpointed.Events);
        var securedIndex = IndexOf<GoldSecuredEvent>(checkpointed.Events);
        Assert.True(treasureIndex >= 0);
        Assert.True(securedIndex > treasureIndex);
        Assert.Contains(checkpointed.Events, domainEvent => domainEvent is GameSuspendedEvent);
        var fountain = Assert.IsType<FountainOutcomeResolvedEvent>(
            checkpointed.Events.Single(domainEvent => domainEvent is FountainOutcomeResolvedEvent));
        Assert.Equal(["fountain-observed"], fountain.Observations);
    }

    [Fact]
    public void Legacy_failure_path_replays_death_save_reload_and_restart_with_knowledge_retained()
    {
        const long seed = 4321;
        var layout = new FloorLayoutGenerator().Generate(seed, "generator-1", 1);
        var initial = CreateReadyState(seed, GameMode.Legacy) with
        {
            Player = CreateReadyState(seed, GameMode.Legacy).Player with
            {
                HitPoints = 0,
                MaxHitPoints = 10
            }
        };
        var newPlayerId = Guid.Parse("00000000-0000-0000-0000-000000000108");
        var replacementCommand = new ReplaceLegacyCharacterCommand(
            newPlayerId,
            new CharacterCreationRequest(
                CharacterCreationMode.PointAllocation,
                new PointAllocationCharacterCreationInput(new PlayerAttributes(3, 4, 5, 6, 7, 8))));
        var commands = new Func<CommandDispatcher, CommandResult>[]
        {
            dispatcher => dispatcher.Dispatch(new EnterDungeonCommand()),
            dispatcher => dispatcher.Dispatch(new AddMonsterKnowledgeCommand(
                "crypt-stalker",
                ["observed-small"])),
            dispatcher => dispatcher.Dispatch(new AcquireTreasureCommand(19, ["lost-item"])),
            dispatcher => dispatcher.Dispatch(new SuspendExpeditionCommand()),
            dispatcher => dispatcher.Dispatch(new PlayerDeathCommand()),
            dispatcher => dispatcher.Dispatch(replacementCommand)
        };

        SimulationTestHarness.AssertDeterministic(
            initial,
            dispatcher => RegisterFailureHandlers(dispatcher, layout),
            commands,
            [5]);
        var result = SimulationTestHarness.Run(
            initial,
            dispatcher => RegisterFailureHandlers(dispatcher, layout),
            commands,
            [5]);
        var uninterrupted = SimulationTestHarness.Run(
            initial,
            dispatcher => RegisterFailureHandlers(dispatcher, layout),
            commands);

        Assert.Equal(uninterrupted.FinalSave, result.FinalSave);
        Assert.Equal(EventSignatures(uninterrupted.Events), EventSignatures(result.Events));

        var state = result.FinalState;
        Assert.Equal(newPlayerId, state.Player.Id);
        Assert.True(state.Player.Alive);
        Assert.True(state.Inn.IsAtInn);
        Assert.False(state.Expedition.Active);
        Assert.Equal(0, state.Player.CarriedGold);
        Assert.Equal(0, state.Expedition.CarriedGold);
        Assert.Empty(state.Expedition.AcquiredItems);
        Assert.Empty(state.Player.Inventory);
        Assert.Equal(0, state.SecuredProgress.SecuredGold);
        var deadHero = Assert.Single(state.Legacy.PreviousHeroes);
        Assert.Equal(initial.Player.Id, deadHero.HeroId);
        var retainedKnowledge = Assert.Single(state.Knowledge.Entries);
        Assert.Equal("monster:crypt-stalker", retainedKnowledge.SubjectId);
        Assert.Equal(["observed-small"], retainedKnowledge.Observations);
        Assert.Contains(result.Events, domainEvent => domainEvent is PlayerDiedEvent);
        Assert.Contains(result.Events, domainEvent => domainEvent is LegacyCharacterReplacedEvent);
        Assert.Equal(
            GameState.CurrentSaveVersion,
            SaveGameSerializer.Deserialize(result.FinalSave).SaveVersion);
    }

    private static GameState CreateReadyState(long seed, GameMode mode)
    {
        var playerId = mode == GameMode.Legacy
            ? Guid.Parse("00000000-0000-0000-0000-000000000109")
            : Guid.Parse("00000000-0000-0000-0000-000000000110");
        var state = GameState.Create(seed, mode: mode, playerId: playerId);
        var provider = new PointAllocationCharacterCreationProvider(
            new PointAllocationCharacterCreationConfiguration(33, 0, 20));
        var dispatcher = new CommandDispatcher(state);
        dispatcher.Register<CreateCharacterCommand>((current, command) =>
            CharacterCreationResolver.Resolve(current, command, provider));
        var creation = dispatcher.Dispatch(new CreateCharacterCommand(new CharacterCreationRequest(
            CharacterCreationMode.PointAllocation,
            new PointAllocationCharacterCreationInput(new PlayerAttributes(3, 4, 5, 6, 7, 8)))));
        var setup = NewGameSetupResolver.Create(new NewGameSetupRequest(
            seed,
            mode,
            new CharacterCreationResult(creation.State.Player)));

        Assert.IsType<CharacterCreatedEvent>(Assert.Single(creation.Events));
        Assert.IsType<NewGameCreatedEvent>(Assert.Single(setup.Events));
        return setup.State;
    }

    private static Func<CommandDispatcher, CommandResult>[] BuildSuccessCommands(
        FloorLayout layout,
        FeatureInstance feature,
        DungeonPosition neighbor)
    {
        var toNeighbor = DirectionBetween(layout.StairsUp, neighbor);
        var commands = new List<Func<CommandDispatcher, CommandResult>>
        {
            dispatcher => dispatcher.Dispatch(new EnterDungeonCommand()),
            dispatcher => dispatcher.Dispatch(new ActivateFeatureCommand(feature.InstanceId)),
            dispatcher => dispatcher.Dispatch(new AddFeatureKnowledgeCommand(
                "azure-fountain",
                ["fountain-observed"])),
            dispatcher => dispatcher.Dispatch(new MoveCommand(toNeighbor)),
            dispatcher => dispatcher.Dispatch(new AdvanceCombatCommand()),
            dispatcher => dispatcher.Dispatch(new AssessThreatCommand()),
            dispatcher => dispatcher.Dispatch(new SelectCombatActionCommand(CombatAction.Attack)),
            dispatcher => dispatcher.Dispatch(new AttackCommand()),
            dispatcher => dispatcher.Dispatch(new AddMonsterKnowledgeCommand(
                "crypt-stalker",
                ["observed-small"])),
            dispatcher => dispatcher.Dispatch(new AcquireTreasureCommand(17, ["amulet"])),
            dispatcher => AssertUnsecuredTreasure(dispatcher),
            dispatcher => dispatcher.Dispatch(new SuspendExpeditionCommand())
        };
        commands.AddRange(FindPath(layout, neighbor, layout.StairsDown)
            .Select(direction => (Func<CommandDispatcher, CommandResult>)(dispatcher =>
                dispatcher.Dispatch(new MoveCommand(direction)))));
        commands.Add(dispatcher => dispatcher.Dispatch(new LeaveDungeonCommand()));
        return commands.ToArray();
    }

    private static void RegisterSuccessHandlers(
        CommandDispatcher dispatcher,
        FloorLayout layout,
        FeatureInstance feature)
    {
        dispatcher.Register<EnterDungeonCommand>((state, command) =>
            DungeonWalkingResolver.Enter(state, command, layout));
        dispatcher.Register<ActivateFeatureCommand>((state, command) =>
            FeatureActivationResolver.ActivateFountain(
                state,
                command,
                new FeatureOutcomeResolution(
                    [FountainEffectIds.RestoreSpellPower],
                    ["fountain-observed"])));
        dispatcher.Register<AddFeatureKnowledgeCommand>((state, command) =>
            FeatureKnowledgeResolver.Add(state, command));
        dispatcher.Register<MoveCommand>((state, command) =>
            DungeonWalkingResolver.Move(
                state,
                command,
                layout,
                state.Expedition.MonstersDefeated == 0
                    ? new EncounterTriggerConfiguration(
                        1,
                        [new EncounterSpawnOption("crypt-stalker", 2, 3)])
                    : new EncounterTriggerConfiguration(0)));
        dispatcher.Register<AdvanceCombatCommand>(CombatStateResolver.Advance);
        dispatcher.Register<AssessThreatCommand>((state, command) =>
            ThreatAssessmentResolver.Resolve(
                state,
                command,
                new ThreatClassificationConfiguration(0, 3)));
        dispatcher.Register<SelectCombatActionCommand>(CombatStateResolver.SelectAction);
        dispatcher.Register<AttackCommand>((state, command) =>
            AttackResolver.Resolve(state, command, new AttackConfiguration(3)));
        dispatcher.Register<AddMonsterKnowledgeCommand>((state, command) =>
            MonsterKnowledgeResolver.Add(state, command));
        dispatcher.Register<AcquireTreasureCommand>(TreasureAcquisitionResolver.Resolve);
        dispatcher.Register<SuspendExpeditionCommand>(ExpeditionSuspensionResolver.Suspend);
        dispatcher.Register<LeaveDungeonCommand>((state, command) =>
            DungeonWalkingResolver.Leave(state, command, layout));

        Assert.Equal(feature.InstanceId, dispatcher.CurrentState.Dungeon.Features.Single().InstanceId);
    }

    private static void RegisterFailureHandlers(CommandDispatcher dispatcher, FloorLayout layout)
    {
        dispatcher.Register<EnterDungeonCommand>((state, command) =>
            DungeonWalkingResolver.Enter(state, command, layout));
        dispatcher.Register<AddFeatureKnowledgeCommand>((state, command) =>
            FeatureKnowledgeResolver.Add(state, command));
        dispatcher.Register<AddMonsterKnowledgeCommand>((state, command) =>
            MonsterKnowledgeResolver.Add(state, command));
        dispatcher.Register<AcquireTreasureCommand>(TreasureAcquisitionResolver.Resolve);
        dispatcher.Register<SuspendExpeditionCommand>(ExpeditionSuspensionResolver.Suspend);
        dispatcher.Register<PlayerDeathCommand>(PlayerDeathResolver.Resolve);
        dispatcher.Register<ReplaceLegacyCharacterCommand>((state, command) =>
            LegacyCharacterReplacementResolver.Resolve(
                state,
                command,
                new PointAllocationCharacterCreationProvider(
                    new PointAllocationCharacterCreationConfiguration(33, 0, 20))));
    }

    private static int IndexOf<TEvent>(IReadOnlyList<IDomainEvent> events)
        where TEvent : IDomainEvent
        => Array.FindIndex(events.ToArray(), domainEvent => domainEvent is TEvent);

    private static int IndexOfKnowledge(
        IReadOnlyList<IDomainEvent> events,
        string subjectId)
        => Array.FindIndex(
            events.ToArray(),
            domainEvent => domainEvent is KnowledgeObservationAddedEvent observation &&
                observation.SubjectId == subjectId);

    private static IReadOnlyList<string> EventSignatures(IEnumerable<IDomainEvent> events) =>
        events.Select(domainEvent =>
            $"{domainEvent.GetType().AssemblyQualifiedName}:{JsonSerializer.Serialize(domainEvent, domainEvent.GetType())}")
            .ToArray();

    private static CommandResult AssertUnsecuredTreasure(CommandDispatcher dispatcher)
    {
        Assert.Equal(17, dispatcher.CurrentState.Player.CarriedGold);
        Assert.Equal(17, dispatcher.CurrentState.Expedition.CarriedGold);
        Assert.Equal(0, dispatcher.CurrentState.SecuredProgress.SecuredGold);
        return new CommandResult(dispatcher.CurrentState);
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

    private static IReadOnlyList<MovementDirection> FindPath(
        FloorLayout layout,
        DungeonPosition start,
        DungeonPosition target)
    {
        var pending = new Queue<DungeonPosition>([start]);
        var previous = new Dictionary<DungeonPosition, (DungeonPosition Position, MovementDirection Direction)>
        {
            [start] = default
        };

        while (pending.Count > 0)
        {
            var current = pending.Dequeue();
            if (current == target)
            {
                var path = new List<MovementDirection>();
                while (current != start)
                {
                    var step = previous[current];
                    path.Add(step.Direction);
                    current = step.Position;
                }

                path.Reverse();
                return path;
            }

            foreach (var direction in Enum.GetValues<MovementDirection>())
            {
                var next = Offset(current, direction);
                if (next.X < 0 || next.X >= layout.Width || next.Y < 0 ||
                    next.Y >= layout.Height || !layout.IsWalkable(next) || previous.ContainsKey(next))
                {
                    continue;
                }

                previous[next] = (current, direction);
                pending.Enqueue(next);
            }
        }

        throw new InvalidOperationException("The generated layout has no path to the floor entrance.");
    }
}
