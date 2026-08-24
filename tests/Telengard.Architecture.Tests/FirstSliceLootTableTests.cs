using Telengard.Content;
using Telengard.Core.Economy;
using Telengard.Core.Meta;
using Telengard.Core.Simulation;
using Telengard.Core.World.Generation;
using Xunit;

namespace Telengard.Architecture.Tests;

public sealed class FirstSliceLootTableTests
{
    [Fact]
    public void Production_pack_resolves_the_band_and_monster_loot_references()
    {
        var pack = ContentPackLoader.Load(RepositoryContentRoot());
        var table = Assert.Single(pack.LootTables.Definitions.Values);
        var band = pack.Bands.GetRequired("upper-ruins");

        Assert.Equal("upper-ruins-loot", table.Id);
        Assert.Equal(table.Id, band.LootProfile);
        Assert.Equal(pack.Items.Count, table.Entries.Count);
        Assert.Equal(pack.Items.Definitions.Keys, table.Entries.Select(entry => entry.ItemId));
        Assert.All(table.Entries, entry => Assert.Equal(1, entry.Weight));
        Assert.All(pack.Monsters.Definitions.Values, monster => Assert.Equal(table.Id, monster.LootTable));
    }

    [Fact]
    public void Production_loot_selection_replays_and_selected_item_remains_unsecured_until_return()
    {
        var pack = ContentPackLoader.Load(RepositoryContentRoot());
        var table = pack.LootTables.GetRequired("upper-ruins-loot");
        var position = new DungeonPosition(1, 4, 5);
        var expeditionId = Guid.Parse("00000000-0000-0000-0000-000000000001");

        var first = LootTableEngine.Select(table, 1234, pack.ContentVersion, position, expeditionId, 0);
        var second = LootTableEngine.Select(table, 1234, pack.ContentVersion, position, expeditionId, 0);
        Assert.Equal(first, second);
        Assert.Contains(first, pack.Items.Definitions.Keys);

        var state = GameState.Create(1234) with
        {
            Inn = new InnState { IsAtInn = false },
            Expedition = new ExpeditionState { Active = true },
            Player = new PlayerState()
        };
        var acquired = TreasureAcquisitionResolver.Resolve(
            state,
            new AcquireTreasureCommand(0, [first]));

        Assert.Equal([first], acquired.State.Expedition.AcquiredItems);
        Assert.Empty(acquired.State.Player.Inventory);

        var layout = new FloorLayoutGenerator().Generate(1234, "generator-1", 1);
        var returned = DungeonWalkingResolver.Leave(
            acquired.State with { Player = acquired.State.Player with { Position = layout.StairsDown } },
            new LeaveDungeonCommand(),
            layout);

        Assert.Equal([first], returned.State.Player.Inventory);
        Assert.Empty(returned.State.Expedition.AcquiredItems);
    }

    private static string RepositoryContentRoot() =>
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "content");
}
