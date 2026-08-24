namespace Telengard.Core.Simulation;

public enum SimulationTimeMode
{
    Normal,
    Slowed,
    Paused
}

public sealed record AdvanceSimulationCommand(long Ticks) : ICommand;

public sealed record SimulationAdvancedEvent(long Ticks, long SimulationTick) : IDomainEvent;

public sealed class SimulationClock
{
    private double _accumulator;

    public SimulationClock(double simulationHz, double slowedScale)
    {
        if (!double.IsFinite(simulationHz) || simulationHz <= 0) throw new ArgumentOutOfRangeException(nameof(simulationHz));
        if (!double.IsFinite(slowedScale) || slowedScale <= 0 || slowedScale > 1) throw new ArgumentOutOfRangeException(nameof(slowedScale));
        SimulationHz = simulationHz;
        SlowedScale = slowedScale;
        Mode = SimulationTimeMode.Normal;
    }

    public double SimulationHz { get; }
    public double SlowedScale { get; }
    public SimulationTimeMode Mode { get; private set; }

    public void SetMode(SimulationTimeMode mode)
    {
        if (!Enum.IsDefined(mode)) throw new ArgumentOutOfRangeException(nameof(mode));
        Mode = mode;
    }

    public long Advance(double elapsedSeconds)
    {
        if (!double.IsFinite(elapsedSeconds) || elapsedSeconds < 0) throw new ArgumentOutOfRangeException(nameof(elapsedSeconds));
        if (Mode == SimulationTimeMode.Paused) return 0;

        var scale = Mode == SimulationTimeMode.Slowed ? SlowedScale : 1d;
        _accumulator += elapsedSeconds * SimulationHz * scale;
        var ticks = (long)Math.Floor(_accumulator);
        _accumulator -= ticks;
        return ticks;
    }
}

public static class SimulationTimeResolver
{
    public static CommandResult Advance(GameState state, AdvanceSimulationCommand command)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(command);
        if (command.Ticks <= 0) throw new ArgumentOutOfRangeException(nameof(command), "Simulation ticks must be positive.");
        var tick = checked(state.SimulationTick + command.Ticks);
        var expedition = state.Expedition.Active
            ? state.Expedition with { SimulationTicks = checked(state.Expedition.SimulationTicks + command.Ticks) }
            : state.Expedition;
        return new CommandResult(
            state with { SimulationTick = tick, Expedition = expedition },
            [new SimulationAdvancedEvent(command.Ticks, tick)]);
    }
}
