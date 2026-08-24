using Telengard.Core.Simulation;
using Xunit;

namespace Telengard.Architecture.Tests;

public sealed class SimulationClockTests
{
    [Fact]
    public void Clock_accumulates_fractional_ticks_without_render_rate_dependence()
    {
        var clock = new SimulationClock(10, 0.2);

        Assert.Equal(0, clock.Advance(0.04));
        Assert.Equal(1, clock.Advance(0.06));
        Assert.Equal(1, clock.Advance(0.1));
    }

    [Fact]
    public void Slowed_and_paused_modes_control_tick_production()
    {
        var clock = new SimulationClock(10, 0.2);

        clock.SetMode(SimulationTimeMode.Slowed);
        Assert.Equal(1, clock.Advance(0.5));
        clock.SetMode(SimulationTimeMode.Paused);
        Assert.Equal(0, clock.Advance(10));
    }

    [Fact]
    public void Advance_command_updates_authoritative_tick_and_expedition_time()
    {
        var state = GameState.Create(1234) with
        {
            Expedition = new ExpeditionState { Active = true }
        };

        var result = SimulationTimeResolver.Advance(state, new AdvanceSimulationCommand(3));

        Assert.Equal(3, result.State.SimulationTick);
        Assert.Equal(3, result.State.Expedition.SimulationTicks);
        Assert.Equal(new SimulationAdvancedEvent(3, 3), Assert.Single(result.Events));
    }

    [Fact]
    public void Advance_command_rejects_non_positive_ticks_before_mutation()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            SimulationTimeResolver.Advance(GameState.Create(1234), new AdvanceSimulationCommand(0)));
    }
}
