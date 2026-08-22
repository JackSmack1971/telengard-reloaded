using Telengard.Content;
using Telengard.Core.Economy;
using Telengard.Core.Meta;
using Telengard.Core.Rng;
using Telengard.Core.Simulation;
using Telengard.Core.World.Generation;
using Telengard.Save;
using Xunit;

namespace Telengard.Architecture.Tests;

public sealed class TreasureAcquisitionTests
{
    [Fact]
    public void Treasure_adds_gold_and_items_to_the_unsecured_expedition_pool()
    {
        var state = InDungeon(new ExpeditionState { Active = true }) with
        {
            SecuredProgress = new SecuredProgressState { SecuredGold = 11 }
        };

        var result = TreasureAcquisitionResolver.Resolve(
            state,
            new AcquireTreasureCommand(23, ["relic", "potion"]));

        Assert.Equal(23, result.State.Expedition.CarriedGold);
        Assert.Equal(23, result.State.Player.CarriedGold);
        Assert.Equal(["relic", "potion"], result.State.Expedition.AcquiredItems);
        Assert.Equal(11, result.State.SecuredProgress.SecuredGold);
        var acquired = Assert.IsType<TreasureAcquiredEvent>(Assert.Single(result.Events));
        Assert.Equal(new TreasureAcquiredEvent(23, 2, 23), acquired);
    }

    [Fact]
    public void Item_only_treasure_does_not_change_carried_gold()
    {
        var state = InDungeon(new ExpeditionState { Active = true, CarriedGold = 7 }) with
        {
            Player = new PlayerState { CarriedGold = 7 }
        };

        var result = TreasureAcquisitionResolver.Resolve(state, new AcquireTreasureCommand(0, ["relic"]));

        Assert.Equal(7, result.State.Expedition.CarriedGold);
        Assert.Equal(7, result.State.Player.CarriedGold);
        Assert.Equal(["relic"], result.State.Expedition.AcquiredItems);
        Assert.Equal(new TreasureAcquiredEvent(0, 1, 7), Assert.Single(result.Events));
    }

    [Fact]
    public void Return_to_the_inn_secures_treasure_gold_after_acquisition()
    {
        var layout = new FloorLayoutGenerator().Generate(1234, "generator-1", 1);
        var state = DungeonWalkingResolver.Enter(
            GameState.Create(1234),
            new EnterDungeonCommand(),
            layout).State;
        var acquired = TreasureAcquisitionResolver.Resolve(
            state,
            new AcquireTreasureCommand(23, ["relic"]));

        var returned = DungeonWalkingResolver.Leave(
            acquired.State with { Player = acquired.State.Player with { Position = layout.StairsDown } },
            new LeaveDungeonCommand(),
            layout);

        Assert.Equal(23, returned.State.SecuredProgress.SecuredGold);
        Assert.Equal(["relic"], returned.State.Player.Inventory);
        Assert.Empty(returned.State.Expedition.AcquiredItems);
        Assert.Equal(0, returned.State.Expedition.CarriedGold);
        Assert.Collection(
            returned.Events,
            domainEvent => Assert.IsType<DungeonLeftEvent>(domainEvent),
            domainEvent => Assert.IsType<GoldSecuredEvent>(domainEvent),
            domainEvent => Assert.Equal(new TreasureItemsSecuredEvent(1), domainEvent),
            domainEvent => Assert.IsType<ExpeditionSucceededEvent>(domainEvent));
    }

    [Fact]
    public void Acquisition_replays_and_round_trips_through_the_existing_save_boundary()
    {
        var state = InDungeon(new ExpeditionState { Active = true });
        var command = new AcquireTreasureCommand(17, ["relic"]);

        var first = TreasureAcquisitionResolver.Resolve(state, command);
        var second = TreasureAcquisitionResolver.Resolve(state, command);
        var restored = SaveGameSerializer.Deserialize(SaveGameSerializer.Serialize(first.State));

        Assert.Equal(SaveGameSerializer.Serialize(first.State), SaveGameSerializer.Serialize(second.State));
        Assert.Equal(first.Events, second.Events);
        Assert.Equal(SaveGameSerializer.Serialize(first.State), SaveGameSerializer.Serialize(restored));
    }

    [Fact]
    public void Acquired_treasure_survives_suspend_and_resume()
    {
        var state = InDungeon(new ExpeditionState { Active = true });
        var acquired = TreasureAcquisitionResolver.Resolve(
            state,
            new AcquireTreasureCommand(17, ["relic"]));

        var suspended = ExpeditionSuspensionResolver.Suspend(
            acquired.State,
            new SuspendExpeditionCommand());
        var restored = SaveGameSerializer.Deserialize(SaveGameSerializer.Serialize(suspended.State));

        Assert.Equal(17, restored.Expedition.CarriedGold);
        Assert.Equal(["relic"], restored.Expedition.AcquiredItems);
        Assert.True(restored.Expedition.Active);
        Assert.False(restored.Inn.IsAtInn);
    }

    [Fact]
    public void Acquisition_rejects_invalid_state_and_overflow_before_mutation()
    {
        var inactive = GameState.Create(1234);
        var atInn = InDungeon(new ExpeditionState { Active = true });
        var inconsistent = InDungeon(new ExpeditionState { Active = true }) with
        {
            Player = new PlayerState { CarriedGold = 1 }
        };
        var overflow = InDungeon(new ExpeditionState { Active = true, CarriedGold = int.MaxValue }) with
        {
            Player = new PlayerState { CarriedGold = int.MaxValue }
        };
        var dead = InDungeon(new ExpeditionState { Active = true }) with
        {
            Player = new PlayerState { Alive = false }
        };

        Assert.Throws<InvalidOperationException>(() => TreasureAcquisitionResolver.Resolve(
            inactive,
            new AcquireTreasureCommand(1)));
        Assert.Throws<InvalidOperationException>(() => TreasureAcquisitionResolver.Resolve(
            atInn with { Inn = new InnState { IsAtInn = true } },
            new AcquireTreasureCommand(1)));
        Assert.Throws<InvalidOperationException>(() => TreasureAcquisitionResolver.Resolve(
            inconsistent,
            new AcquireTreasureCommand(1)));
        Assert.Throws<InvalidOperationException>(() => TreasureAcquisitionResolver.Resolve(
            dead,
            new AcquireTreasureCommand(1)));
        Assert.Throws<OverflowException>(() => TreasureAcquisitionResolver.Resolve(
            overflow,
            new AcquireTreasureCommand(1)));
        Assert.Equal(0, inactive.Expedition.CarriedGold);
    }

    [Fact]
    public void Loot_table_selection_uses_configured_entries_and_replays_from_scoped_inputs()
    {
        var table = new LootTable("upper-ruins", [
            new LootTableEntry("relic", 1),
            new LootTableEntry("potion", 3)]);
        var position = new DungeonPosition(1, 4, 5);
        var expeditionId = Guid.Parse("00000000-0000-0000-0000-000000000001");

        var first = LootTableEngine.Select(table, 1234, "content-1", position, expeditionId, 0);
        var second = LootTableEngine.Select(table, 1234, "content-1", position, expeditionId, 0);
        var rare = LootTableEngine.Select(table, 1234, "content-1", position, expeditionId, 11);

        Assert.Equal(first, second);
        Assert.Equal("potion", first);
        Assert.Equal("relic", rare);
        Assert.Contains(first, table.Entries.Select(entry => entry.ItemId));
        Assert.Equal(
            first,
            LootTableEngine.Select(
                table,
                new DeterministicRng(1234, "content-1").CreateStream(
                    "loot-table",
                    "table:upper-ruins",
                    $"expedition:{expeditionId:D}",
                    "floor:1",
                    "x:4",
                    "y:5",
                    "acquisition:0")));
    }

    [Fact]
    public void Treasure_commands_validate_empty_and_invalid_item_payloads()
    {
        Assert.Throws<ArgumentException>(() => new AcquireTreasureCommand(0));
        Assert.Throws<ArgumentException>(() => new AcquireTreasureCommand(0, [" "]));
        Assert.Throws<ArgumentOutOfRangeException>(() => new AcquireTreasureCommand(-1, ["relic"]));
    }

    private static GameState InDungeon(ExpeditionState expedition) => GameState.Create(1234) with
    {
        Inn = new InnState { IsAtInn = false },
        Expedition = expedition,
        Player = new PlayerState { CarriedGold = expedition.CarriedGold }
    };
}
