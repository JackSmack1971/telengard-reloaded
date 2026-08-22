using Telengard.Core.Combat;
using Telengard.Core.Events;
using Telengard.Core.Knowledge;
using Telengard.Core.Simulation;
using Telengard.Save;
using Xunit;

namespace Telengard.Architecture.Tests;

public sealed class LegacyCharacterReplacementTests
{
    [Fact]
    public void Replacement_preserves_knowledge_and_legacy_history_while_resetting_character_run_state()
    {
        var dead = LegacyDeadState();
        var result = Resolve(dead);

        Assert.Equal(NewCharacter().Id, result.State.Player.Id);
        Assert.Equal(NewCharacter().Attributes, result.State.Player.Attributes);
        Assert.True(result.State.Player.Alive);
        Assert.Equal(dead.Knowledge, result.State.Knowledge);
        Assert.Equal(dead.Legacy, result.State.Legacy);
        Assert.Equal(dead.Dungeon, result.State.Dungeon);
        Assert.Equal(dead.SecuredProgress, result.State.SecuredProgress);
        Assert.Equal(dead.ExpeditionSequence, result.State.ExpeditionSequence);
        Assert.False(result.State.Expedition.Active);
        Assert.Empty(result.State.Expedition.AcquiredItems);
        Assert.True(result.State.Inn.IsAtInn);
        Assert.Null(result.State.Combat);

        Assert.Collection(
            result.Events,
            domainEvent =>
            {
                var created = Assert.IsType<CharacterCreatedEvent>(domainEvent);
                Assert.Equal(NewCharacter().Id, created.PlayerId);
                Assert.Equal(CharacterCreationMode.PointAllocation, created.Mode);
            },
            domainEvent =>
            {
                var replaced = Assert.IsType<LegacyCharacterReplacedEvent>(domainEvent);
                Assert.Equal(dead.Player.Id, replaced.PreviousPlayerId);
                Assert.Equal(NewCharacter().Id, replaced.NewPlayerId);
            });
    }

    [Fact]
    public void Replacement_survives_the_existing_explicit_save_contract_without_a_schema_change()
    {
        var replacement = Resolve(LegacyDeadState()).State;
        var restored = SaveGameSerializer.Deserialize(SaveGameSerializer.Serialize(replacement));

        Assert.Equal(SaveGameSerializer.Serialize(replacement), SaveGameSerializer.Serialize(restored));
        Assert.Equal(replacement.Knowledge.Entries.Select(entry => entry.SubjectId),
            restored.Knowledge.Entries.Select(entry => entry.SubjectId));
        Assert.Equal(replacement.Knowledge.Entries.Single().Observations,
            restored.Knowledge.Entries.Single().Observations);
        Assert.Equal(replacement.Legacy.PersistentMap, restored.Legacy.PersistentMap);
        Assert.Equal(replacement.Legacy.PreviousHeroes, restored.Legacy.PreviousHeroes);
        Assert.Equal(replacement.Player.Id, restored.Player.Id);
        Assert.Equal(replacement.Player.Attributes, restored.Player.Attributes);
        Assert.False(restored.Expedition.Active);
        Assert.Empty(restored.Expedition.AcquiredItems);
    }

    [Fact]
    public void Equal_replacement_inputs_replay_to_equal_state_and_events()
    {
        var first = Resolve(LegacyDeadState());
        var second = Resolve(LegacyDeadState());

        Assert.Equal(SaveGameSerializer.Serialize(first.State), SaveGameSerializer.Serialize(second.State));
        Assert.Equal(first.Events, second.Events);
    }

    [Fact]
    public void Replacement_rejects_invalid_handoff_state_or_character_before_mutation()
    {
        var dead = LegacyDeadState();

        Assert.Throws<InvalidOperationException>(() => Resolve(
            dead with { CurrentMode = GameMode.Classic }));
        Assert.Throws<InvalidOperationException>(() => Resolve(
            dead with { Player = dead.Player with { Alive = true } }));
        Assert.Throws<InvalidOperationException>(() => Resolve(
            dead with { Player = dead.Player with { HitPoints = 1 } }));
        Assert.Throws<InvalidOperationException>(() => Resolve(
            dead with { Player = dead.Player with { Inventory = ["lost-item"] } }));
        Assert.Throws<InvalidOperationException>(() => Resolve(
            dead with { Expedition = dead.Expedition with { Active = true } }));
        Assert.Throws<InvalidOperationException>(() => Resolve(
            dead with { Expedition = dead.Expedition with { CarriedGold = 1 } }));
        Assert.Throws<InvalidOperationException>(() => Resolve(
            dead with { Inn = new InnState { IsAtInn = false } }));
        Assert.Throws<InvalidOperationException>(() => Resolve(
            dead with
            {
                Combat = new CombatState(
                    new MonsterInstance(
                        Guid.Parse("00000000-0000-0000-0000-000000000105"),
                        "rat",
                        1,
                        1,
                        new DungeonPosition(1, 0, 0)),
                    CombatPhase.StateCheck,
                    1)
            }));
        Assert.Throws<InvalidOperationException>(() => Resolve(
            GameState.Create(1234, mode: GameMode.Legacy, playerId: dead.Player.Id)));
        Assert.Throws<ArgumentException>(() => ResolveWithProvider(
            dead,
            new FixedProvider(NewCharacter() with { Inventory = ["item"] })));
        Assert.Throws<ArgumentException>(() => ResolveWithProvider(
            dead,
            new FixedProvider(NewCharacter() with { Id = dead.Player.Id })));
        Assert.Throws<ArgumentException>(() => Resolve(
            dead with
            {
                Legacy = dead.Legacy with
                {
                    PreviousHeroes = dead.Legacy.PreviousHeroes.Append(
                        dead.Legacy.PreviousHeroes[0] with
                        {
                            HeroId = Guid.Parse("00000000-0000-0000-0000-000000000104")
                        }).ToArray()
                }
            },
            Guid.Parse("00000000-0000-0000-0000-000000000104")));
    }

    [Fact]
    public void Dispatcher_publishes_replacement_events_after_committing_the_new_character()
    {
        var dispatcher = new CommandDispatcher(LegacyDeadState(), new DomainEventBus());
        GameState? observed = null;
        dispatcher.EventBus!.Subscribe<LegacyCharacterReplacedEvent>(_ => observed = dispatcher.CurrentState);
        dispatcher.Register<ReplaceLegacyCharacterCommand>((state, command) =>
            LegacyCharacterReplacementResolver.Resolve(state, command, ReplacementProvider()));

        dispatcher.Dispatch(ReplacementCommand());

        Assert.NotNull(observed);
        Assert.Equal(NewCharacter().Id, observed!.Player.Id);
        Assert.False(observed.Expedition.Active);
    }

    [Fact]
    public void Dispatcher_rejects_invalid_handoff_without_committing_or_publishing()
    {
        var dispatcher = new CommandDispatcher(
            LegacyDeadState() with
            {
                Expedition = LegacyDeadState().Expedition with { Active = true }
            },
            new DomainEventBus());
        var published = 0;
        dispatcher.EventBus!.Subscribe<LegacyCharacterReplacedEvent>(_ => published++);
        dispatcher.Register<ReplaceLegacyCharacterCommand>((state, command) =>
            LegacyCharacterReplacementResolver.Resolve(state, command, ReplacementProvider()));
        var before = SaveGameSerializer.Serialize(dispatcher.CurrentState);

        Assert.Throws<InvalidOperationException>(() => dispatcher.Dispatch(ReplacementCommand()));

        Assert.Equal(before, SaveGameSerializer.Serialize(dispatcher.CurrentState));
        Assert.Equal(0, published);
    }

    private static CommandResult Resolve(GameState state, Guid? newPlayerId = null) =>
        ResolveWithProvider(state, ReplacementProvider(), newPlayerId);

    private static CommandResult ResolveWithProvider(
        GameState state,
        ICharacterCreationProvider provider,
        Guid? newPlayerId = null) =>
        LegacyCharacterReplacementResolver.Resolve(
            state,
            ReplacementCommand(newPlayerId),
            provider);

    private static ReplaceLegacyCharacterCommand ReplacementCommand(Guid? newPlayerId = null) =>
        new(
            newPlayerId ?? NewCharacter().Id,
            new CharacterCreationRequest(
                CharacterCreationMode.PointAllocation,
                new PointAllocationCharacterCreationInput(NewCharacter().Attributes)));

    private static ICharacterCreationProvider ReplacementProvider() =>
        new PointAllocationCharacterCreationProvider(
            new PointAllocationCharacterCreationConfiguration(33, 0, 20));

    private static GameState LegacyDeadState()
    {
        var playerId = Guid.Parse("00000000-0000-0000-0000-000000000101");
        var initial = GameState.Create(1234, mode: GameMode.Legacy, playerId: playerId) with
        {
            Player = new PlayerState
            {
                Id = playerId,
                Attributes = new PlayerAttributes(7, 8, 9, 10, 11, 12),
                Position = new DungeonPosition(2, 3, 4),
                HitPoints = 0,
                MaxHitPoints = 10,
                Inventory = ["old-item"]
            },
            Expedition = new ExpeditionState
            {
                ExpeditionId = Guid.Parse("00000000-0000-0000-0000-000000000102"),
                DeepestFloorReached = 2,
                Active = true
            },
            Inn = new InnState { IsAtInn = false },
            Knowledge = new KnowledgeState([
                new KnowledgeEntry(
                    "monster:rat",
                    observations: ["observed-small"],
                    sampleCount: 1,
                    confidence: 1)]),
            Legacy = new LegacyState
            {
                PersistentMap = new PersistentMapState(
                    observedPositions: [new DungeonPosition(1, 0, 0)],
                    visitedPositions: [new DungeonPosition(1, 0, 0)])
            }
        };

        return PlayerDeathResolver.Resolve(initial, new PlayerDeathCommand()).State;
    }

    private static PlayerState NewCharacter() => new()
    {
        Id = Guid.Parse("00000000-0000-0000-0000-000000000103"),
        Attributes = new PlayerAttributes(3, 4, 5, 6, 7, 8)
    };

    private sealed class FixedProvider(PlayerState player) : ICharacterCreationProvider
    {
        public CharacterCreationMode Mode => CharacterCreationMode.PointAllocation;

        public CharacterCreationResult Create(GameState state, CharacterCreationRequest request) =>
            new(player);
    }
}
