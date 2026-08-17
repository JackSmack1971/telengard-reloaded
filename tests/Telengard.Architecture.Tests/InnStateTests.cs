using Telengard.Core.Simulation;
using Telengard.Core.World.Generation;
using Xunit;

namespace Telengard.Architecture.Tests;

public sealed class InnStateTests
{
    [Fact]
    public void New_games_start_at_the_inn()
    {
        Assert.True(GameState.Create(1234).Inn.IsAtInn);
    }

    [Fact]
    public void Entry_leaves_the_inn_and_return_reaches_it()
    {
        var layout = new FloorLayoutGenerator().Generate(1234, "generator-1", 1);
        var state = GameState.Create(1234);

        var entered = DungeonWalkingResolver.Enter(state, new EnterDungeonCommand(), layout);
        var returned = DungeonWalkingResolver.Leave(
            entered.State with { Player = entered.State.Player with { Position = layout.StairsDown } },
            new LeaveDungeonCommand(),
            layout);

        Assert.False(entered.State.Inn.IsAtInn);
        Assert.True(returned.State.Inn.IsAtInn);
        Assert.False(returned.State.Expedition.Active);
    }

    [Fact]
    public void Entry_rejects_a_state_that_is_not_at_the_inn()
    {
        var layout = new FloorLayoutGenerator().Generate(1234, "generator-1", 1);
        var state = GameState.Create(1234) with { Inn = new InnState { IsAtInn = false } };

        Assert.Throws<InvalidOperationException>(() =>
            DungeonWalkingResolver.Enter(state, new EnterDungeonCommand(), layout));
    }

    [Fact]
    public void Leaving_requires_an_active_expedition()
    {
        var layout = new FloorLayoutGenerator().Generate(1234, "generator-1", 1);
        var state = GameState.Create(1234) with { Player = new PlayerState { Position = layout.StairsDown } };

        Assert.Throws<InvalidOperationException>(() =>
            DungeonWalkingResolver.Leave(state, new LeaveDungeonCommand(), layout));
    }
}
