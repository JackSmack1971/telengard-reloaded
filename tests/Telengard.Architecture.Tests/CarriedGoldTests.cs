using Telengard.Core.Economy;
using Telengard.Core.Simulation;
using Xunit;

namespace Telengard.Architecture.Tests;

public sealed class CarriedGoldTests
{
    [Fact]
    public void Acquiring_gold_updates_the_carried_pool_and_emits_a_committed_event()
    {
        var state = InDungeon(new ExpeditionState { Active = true }) with
        {
            SecuredProgress = new SecuredProgressState { SecuredGold = 11 }
        };
        var dispatcher = new CommandDispatcher(state);
        dispatcher.Register<AcquireGoldCommand>(CarriedGoldResolver.Acquire);

        var result = dispatcher.Dispatch(new AcquireGoldCommand(23));

        Assert.Equal(23, result.State.Expedition.CarriedGold);
        Assert.Equal(23, result.State.Player.CarriedGold);
        Assert.Equal(11, result.State.SecuredProgress.SecuredGold);
        var acquired = Assert.IsType<GoldAcquiredEvent>(Assert.Single(result.Events));
        Assert.Equal(23, acquired.Amount);
        Assert.Equal(23, acquired.CarriedGold);
    }

    [Fact]
    public void Acquisition_is_deterministic_for_equal_state_and_command()
    {
        var command = new AcquireGoldCommand(7);

        var first = CarriedGoldResolver.Acquire(InDungeon(new ExpeditionState { Active = true }), command);
        var second = CarriedGoldResolver.Acquire(InDungeon(new ExpeditionState { Active = true }), command);

        Assert.Equal(first.State, second.State);
        Assert.Equal(first.Events, second.Events);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Non_positive_acquisition_is_rejected_before_mutation(int amount)
    {
        var state = InDungeon(new ExpeditionState { Active = true });

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CarriedGoldResolver.Acquire(state, new AcquireGoldCommand(amount)));
        Assert.Equal(0, state.Expedition.CarriedGold);
    }

    [Fact]
    public void Acquisition_requires_a_live_dungeon_expedition()
    {
        var inactive = GameState.Create(1234);
        var atInn = InDungeon(new ExpeditionState { Active = true }) with
        {
            Inn = new InnState { IsAtInn = true }
        };

        Assert.Throws<InvalidOperationException>(() => CarriedGoldResolver.Acquire(inactive, new AcquireGoldCommand(1)));
        Assert.Throws<InvalidOperationException>(() => CarriedGoldResolver.Acquire(atInn, new AcquireGoldCommand(1)));
    }

    [Fact]
    public void Acquisition_rejects_inconsistent_mirrored_carried_gold()
    {
        var state = InDungeon(new ExpeditionState { Active = true }) with
        {
            Player = new PlayerState { CarriedGold = 1 }
        };

        Assert.Throws<InvalidOperationException>(() => CarriedGoldResolver.Acquire(state, new AcquireGoldCommand(1)));
    }

    [Fact]
    public void Acquisition_rejects_integer_overflow()
    {
        var state = InDungeon(new ExpeditionState { Active = true, CarriedGold = int.MaxValue }) with
        {
            Player = new PlayerState { CarriedGold = int.MaxValue }
        };

        Assert.Throws<OverflowException>(() => CarriedGoldResolver.Acquire(state, new AcquireGoldCommand(1)));
    }

    [Fact]
    public void Acquisition_accepts_the_largest_representable_total()
    {
        var state = InDungeon(new ExpeditionState { Active = true, CarriedGold = 1 }) with
        {
            Player = new PlayerState { CarriedGold = 1 }
        };

        var result = CarriedGoldResolver.Acquire(state, new AcquireGoldCommand(int.MaxValue - 1));

        Assert.Equal(int.MaxValue, result.State.Expedition.CarriedGold);
        Assert.Equal(int.MaxValue, result.State.Player.CarriedGold);
    }

    [Fact]
    public void Acquisition_rejects_null_arguments_at_the_boundary()
    {
        Assert.Throws<ArgumentNullException>(() => CarriedGoldResolver.Acquire(null!, new AcquireGoldCommand(1)));
        Assert.Throws<ArgumentNullException>(() => CarriedGoldResolver.Acquire(GameState.Create(1), null!));
    }

    [Fact]
    public void Acquisition_rejects_negative_existing_carried_gold()
    {
        var state = InDungeon(new ExpeditionState { Active = true, CarriedGold = -1 }) with
        {
            Player = new PlayerState { CarriedGold = -1 }
        };

        Assert.Throws<InvalidOperationException>(() => CarriedGoldResolver.Acquire(state, new AcquireGoldCommand(1)));
    }

    [Fact]
    public void Carried_gold_round_trips_through_the_explicit_save_dto()
    {
        var state = InDungeon(new ExpeditionState { Active = true, CarriedGold = 31 }) with
        {
            Player = new PlayerState { CarriedGold = 31 }
        };

        var restored = Telengard.Save.SaveGameSerializer.Deserialize(
            Telengard.Save.SaveGameSerializer.Serialize(state));

        Assert.Equal(31, restored.Expedition.CarriedGold);
        Assert.Equal(31, restored.Player.CarriedGold);
    }

    private static GameState InDungeon(ExpeditionState expedition) => GameState.Create(1234) with
    {
        Inn = new InnState { IsAtInn = false },
        Expedition = expedition,
        Player = new PlayerState { CarriedGold = expedition.CarriedGold }
    };
}
