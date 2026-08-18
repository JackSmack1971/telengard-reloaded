using Telengard.Core.Combat;
using Telengard.Core.Items;
using Telengard.Core.Simulation;
using Telengard.Core.World.Generation;
using Telengard.Save;
using Xunit;

namespace Telengard.Architecture.Tests;

public sealed class DeathTests
{
    [Fact]
    public void Death_marks_the_player_and_fails_the_expedition_after_commit()
    {
        var state = ActiveState();

        var result = PlayerDeathResolver.Resolve(state, new PlayerDeathCommand());

        Assert.False(result.State.Player.Alive);
        Assert.Equal(0, result.State.Player.HitPoints);
        Assert.False(result.State.Expedition.Active);
        Assert.True(result.State.Inn.IsAtInn);
        Assert.Null(result.State.Combat);
        Assert.Equal(Guid.Empty, result.State.Player.Id);
        Assert.Equal(0, result.State.Player.Level);
        Assert.Equal(0, result.State.Player.Experience);
        Assert.Empty(result.State.Player.Inventory);
        Assert.Empty(result.State.Player.EquipmentSlots);
        Assert.Empty(result.State.Player.Talents);
        Assert.Empty(result.State.Player.Spells);
        Assert.Equal(0, result.State.Player.CarriedGold);
        Assert.Equal(0, result.State.Expedition.CarriedGold);
        Assert.Empty(result.State.Expedition.AcquiredItems);
        Assert.Equal(state.SecuredProgress, result.State.SecuredProgress);
        Assert.Equal(state.Legacy, result.State.Legacy);
        Assert.Equal(state.Knowledge, result.State.Knowledge);

        var died = Assert.IsType<PlayerDiedEvent>(result.Events[0]);
        Assert.Equal(state.Expedition.ExpeditionId, died.ExpeditionId);
        Assert.Equal(state.Player.Position, died.Position);
        var failed = Assert.IsType<ExpeditionFailedEvent>(result.Events[1]);
        Assert.Equal(state.Expedition.ExpeditionId, failed.ExpeditionId);
        Assert.Equal(state.Expedition.DeepestFloorReached, failed.DeepestFloorReached);
    }

    [Fact]
    public void Combat_state_check_resolves_lethal_player_state()
    {
        var state = ActiveState() with
        {
            Combat = new CombatState(
                new MonsterInstance(
                    Guid.Parse("00000000-0000-0000-0000-000000000001"),
                    "rat",
                    1,
                    3,
                    new DungeonPosition(1, 0, 0)),
                CombatPhase.StateCheck,
                2)
        };

        var result = CombatStateResolver.Advance(state, new AdvanceCombatCommand());

        Assert.False(result.State.Player.Alive);
        Assert.Contains(result.Events, domainEvent => domainEvent is PlayerDiedEvent);
        Assert.Contains(result.Events, domainEvent => domainEvent is ExpeditionFailedEvent);
    }

    [Fact]
    public void Legacy_death_preserves_the_dead_hero_and_persistent_knowledge_but_loses_unsecured_assets()
    {
        var state = ActiveState() with { CurrentMode = GameMode.Legacy };

        var result = PlayerDeathResolver.Resolve(state, new PlayerDeathCommand());

        Assert.False(result.State.Player.Alive);
        Assert.Equal(0, result.State.Player.HitPoints);
        Assert.Equal(state.Player.Id, result.State.Player.Id);
        Assert.Equal(state.Player.Attributes, result.State.Player.Attributes);
        Assert.Equal(state.Player.Level, result.State.Player.Level);
        Assert.Equal(state.Player.Experience, result.State.Player.Experience);
        Assert.Equal(state.Player.Talents, result.State.Player.Talents);
        Assert.Equal(state.Player.Spells, result.State.Player.Spells);
        Assert.Equal(0, result.State.Player.CarriedGold);
        Assert.Empty(result.State.Player.Inventory);
        Assert.Equal(state.Player.EquipmentSlots.Select(slot => slot.SlotId), result.State.Player.EquipmentSlots.Select(slot => slot.SlotId));
        Assert.All(result.State.Player.EquipmentSlots, slot => Assert.Null(slot.ItemInstanceId));
        Assert.False(result.State.Expedition.Active);
        Assert.Equal(0, result.State.Expedition.CarriedGold);
        Assert.Empty(result.State.Expedition.AcquiredItems);
        Assert.Equal(state.SecuredProgress, result.State.SecuredProgress);
        Assert.Equal(state.Legacy.PersistentMap, result.State.Legacy.PersistentMap);
        var record = Assert.Single(result.State.Legacy.PreviousHeroes);
        Assert.Equal(state.Player.Id, record.HeroId);
        Assert.Equal(state.Player.Attributes, record.Attributes);
        Assert.Equal(state.Player.Level, record.Level);
        Assert.Equal(state.Player.Experience, record.Experience);
        Assert.Equal(state.Player.Position, record.DeathPosition);
        Assert.Equal(state.Expedition.ExpeditionId, record.ExpeditionId);
        Assert.Equal(state.Expedition.DeepestFloorReached, record.DeepestFloorReached);
        var grave = Assert.Single(result.State.Legacy.Graves);
        Assert.Equal(state.Player.Id, grave.HeroId);
        Assert.Equal(state.Player.Position, grave.Position);
        Assert.Equal(state.Expedition.ExpeditionId, grave.ExpeditionId);
        Assert.Equal(state.Knowledge, result.State.Knowledge);

        Assert.Collection(
            result.Events,
            domainEvent => Assert.IsType<PlayerDiedEvent>(domainEvent),
            domainEvent => Assert.IsType<ExpeditionFailedEvent>(domainEvent));
    }

    [Fact]
    public void Legacy_death_replays_to_the_same_state_and_events()
    {
        var first = PlayerDeathResolver.Resolve(
            ActiveState() with { CurrentMode = GameMode.Legacy },
            new PlayerDeathCommand());
        var second = PlayerDeathResolver.Resolve(
            ActiveState() with { CurrentMode = GameMode.Legacy },
            new PlayerDeathCommand());

        Assert.Equal(
            SaveGameSerializer.Serialize(first.State),
            SaveGameSerializer.Serialize(second.State));
        Assert.Equal(first.Events, second.Events);
    }

    [Fact]
    public void Legacy_death_appends_to_existing_dead_hero_records_without_changing_prior_records()
    {
        var prior = new DeadHeroRecord(
            Guid.Parse("00000000-0000-0000-0000-000000000005"),
            new PlayerAttributes(1, 2, 3, 4, 5, 6),
            2,
            19,
            new DungeonPosition(1, 1, 1),
            Guid.Parse("00000000-0000-0000-0000-000000000006"),
            1);
        var priorGrave = new GraveRecord(
            prior.HeroId,
            prior.DeathPosition,
            prior.ExpeditionId);
        var state = ActiveState() with
        {
            CurrentMode = GameMode.Legacy,
            Legacy = new LegacyState
            {
                PersistentMap = ActiveState().Legacy.PersistentMap,
                PreviousHeroes = [prior],
                Graves = [priorGrave]
            }
        };

        var result = PlayerDeathResolver.Resolve(state, new PlayerDeathCommand());

        Assert.Equal([prior], result.State.Legacy.PreviousHeroes.Take(1));
        Assert.Equal(2, result.State.Legacy.PreviousHeroes.Count);
        Assert.Equal([priorGrave], result.State.Legacy.Graves.Take(1));
        Assert.Equal(2, result.State.Legacy.Graves.Count);
    }

    [Fact]
    public void Adventure_death_returns_to_the_inn_retains_the_character_and_loses_expedition_treasure()
    {
        var state = ActiveState() with { CurrentMode = GameMode.Adventure };

        var result = PlayerDeathResolver.Resolve(state, new PlayerDeathCommand());

        Assert.True(result.State.Player.Alive);
        Assert.Equal(state.Player.MaxHitPoints, result.State.Player.HitPoints);
        Assert.Equal(state.Player.Id, result.State.Player.Id);
        Assert.Equal(state.Player.Attributes, result.State.Player.Attributes);
        Assert.Equal(state.Player.Level, result.State.Player.Level);
        Assert.Equal(state.Player.Experience, result.State.Player.Experience);
        Assert.Equal(state.Player.Inventory, result.State.Player.Inventory);
        Assert.Equal(state.Player.EquipmentSlots, result.State.Player.EquipmentSlots);
        Assert.Equal(state.Player.Talents, result.State.Player.Talents);
        Assert.Equal(state.Player.Spells, result.State.Player.Spells);
        Assert.Equal(state.Player.Injuries, result.State.Player.Injuries);
        Assert.Equal(state.Player.TemporaryEffects, result.State.Player.TemporaryEffects);
        Assert.Equal(0, result.State.Player.CarriedGold);
        Assert.False(result.State.Expedition.Active);
        Assert.Equal(0, result.State.Expedition.CarriedGold);
        Assert.Empty(result.State.Expedition.AcquiredItems);
        Assert.True(result.State.Inn.IsAtInn);
        Assert.Null(result.State.Combat);
        Assert.Equal(state.SecuredProgress, result.State.SecuredProgress);
        Assert.Equal(state.Legacy, result.State.Legacy);
        Assert.Equal(state.Knowledge, result.State.Knowledge);

        Assert.Collection(
            result.Events,
            domainEvent => Assert.IsType<PlayerDiedEvent>(domainEvent),
            domainEvent => Assert.IsType<ExpeditionFailedEvent>(domainEvent));
    }

    [Fact]
    public void Adventure_death_replays_to_the_same_state_and_events()
    {
        var first = PlayerDeathResolver.Resolve(
            ActiveState() with { CurrentMode = GameMode.Adventure },
            new PlayerDeathCommand());
        var second = PlayerDeathResolver.Resolve(
            ActiveState() with { CurrentMode = GameMode.Adventure },
            new PlayerDeathCommand());

        Assert.Equal(SaveGameSerializer.Serialize(first.State), SaveGameSerializer.Serialize(second.State));
        Assert.Equal(first.Events, second.Events);
    }

    [Fact]
    public void Death_requires_a_live_expedition_and_depleted_hit_points()
    {
        var command = new PlayerDeathCommand();

        Assert.Throws<ArgumentNullException>(() => PlayerDeathResolver.Resolve(null!, command));
        Assert.Throws<ArgumentNullException>(() => PlayerDeathResolver.Resolve(ActiveState(), null!));
        Assert.Throws<InvalidOperationException>(() => PlayerDeathResolver.Resolve(
            GameState.Create(1234), command));
        Assert.Throws<InvalidOperationException>(() => PlayerDeathResolver.Resolve(
            ActiveState() with { Player = ActiveState().Player with { HitPoints = 1 } }, command));
        Assert.Throws<InvalidOperationException>(() => PlayerDeathResolver.Resolve(
            ActiveState() with { Player = ActiveState().Player with { Alive = false } }, command));
    }

    [Fact]
    public void Equal_death_inputs_replay_to_equal_state_and_events()
    {
        var first = PlayerDeathResolver.Resolve(ActiveState(), new PlayerDeathCommand());
        var second = PlayerDeathResolver.Resolve(ActiveState(), new PlayerDeathCommand());

        Assert.Equal(first.State, second.State);
        Assert.Equal(first.Events, second.Events);
    }

    [Theory]
    [InlineData(GameMode.Classic)]
    [InlineData(GameMode.Legacy)]
    [InlineData(GameMode.Adventure)]
    public void Death_state_round_trips_through_the_explicit_save_contract(GameMode mode)
    {
        var result = PlayerDeathResolver.Resolve(
            ActiveState() with { CurrentMode = mode },
            new PlayerDeathCommand());

        var restored = SaveGameSerializer.Deserialize(SaveGameSerializer.Serialize(result.State));

        Assert.Equal(SaveGameSerializer.Serialize(result.State), SaveGameSerializer.Serialize(restored));
        Assert.Equal(mode == GameMode.Adventure, restored.Player.Alive);
        Assert.False(restored.Expedition.Active);
        Assert.Null(restored.Combat);
    }

    [Fact]
    public void A_dead_player_cannot_continue_walking_or_change_floors()
    {
        var state = PlayerDeathResolver.Resolve(ActiveState(), new PlayerDeathCommand()).State;
        var layout = new FloorLayoutGenerator().Generate(1234, "generator-1", 1);

        Assert.Throws<InvalidOperationException>(() => DungeonWalkingResolver.Move(
            state,
            new MoveCommand(MovementDirection.North),
            layout));
        Assert.Throws<InvalidOperationException>(() => FloorTransitionResolver.Apply(
            state,
            new ChangeFloorCommand(StairDirection.Up),
            layout,
            layout));
    }

    private static GameState ActiveState()
    {
        var expeditionId = Guid.Parse("00000000-0000-0000-0000-000000000002");
        var itemId = Guid.Parse("00000000-0000-0000-0000-000000000003");
        return GameState.Create(1234) with
        {
            Inn = new InnState { IsAtInn = false },
            Player = new PlayerState
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000004"),
                Attributes = new PlayerAttributes(7, 8, 9, 10, 11, 12),
                Level = 3,
                Experience = 42,
                Position = new DungeonPosition(2, 3, 4),
                HitPoints = 0,
                MaxHitPoints = 10,
                SpellPower = 2,
                MaxSpellPower = 5,
                Inventory = ["potion"],
                EquipmentSlots = [new EquipmentSlotState("weapon", itemId)],
                Talents = ["talent"],
                Spells = ["spell"],
                Injuries = ["injury"],
                TemporaryEffects = ["effect"],
                CarriedGold = 17
            },
            Expedition = new ExpeditionState
            {
                ExpeditionId = expeditionId,
                DeepestFloorReached = 2,
                CarriedGold = 17,
                AcquiredItems = ["item"],
                Active = true
            },
            Legacy = new LegacyState
            {
                PersistentMap = new PersistentMapState(
                    observedPositions: [new DungeonPosition(1, 0, 0)],
                    visitedPositions: [new DungeonPosition(1, 0, 0)])
            },
            SecuredProgress = new SecuredProgressState { SecuredGold = 29 }
        };
    }
}
