using Telengard.Core.Simulation;
using Telengard.Core.Rng;
using Telengard.TestHarness;
using Xunit;

namespace Telengard.Architecture.Tests;

public sealed class SimulationTestHarnessTests
{
    [Fact]
    public void Run_replays_commands_through_save_reload_checkpoint()
    {
        var commands = new Func<CommandDispatcher, CommandResult>[]
        {
            dispatcher => dispatcher.Dispatch(new AdvanceCommand(2)),
            dispatcher => dispatcher.Dispatch(new AdvanceCommand(3))
        };

        var result = SimulationTestHarness.Run(1234, RegisterHandlers, commands, [1]);

        Assert.Equal(5, result.FinalState.SimulationTick);
        Assert.Equal(2, result.Events.Count);
        Assert.Contains("\"simulationTick\": 5", result.FinalSave);
    }

    [Fact]
    public void AssertDeterministic_compares_state_and_event_results()
    {
        var commands = new Func<CommandDispatcher, CommandResult>[]
        {
            dispatcher => dispatcher.Dispatch(new AdvanceCommand(4)),
            dispatcher => dispatcher.Dispatch(new AdvanceCommand(1))
        };

        SimulationTestHarness.AssertDeterministic(1234, RegisterHandlers, commands, [1]);
    }

    [Fact]
    public void Run_reproduces_rng_driven_state_events_and_save_without_presentation()
    {
        var commands = new Func<CommandDispatcher, CommandResult>[]
        {
            dispatcher => dispatcher.Dispatch(new RollCommand("loot")),
            dispatcher => dispatcher.Dispatch(new RollCommand("loot"))
        };

        var first = SimulationTestHarness.Run(1234, RegisterRngHandlers, commands, [1]);
        var second = SimulationTestHarness.Run(1234, RegisterRngHandlers, commands, [1]);

        Assert.NotEqual(GameState.Create(1234), first.FinalState);
        Assert.Equal(first.FinalState, second.FinalState);
        Assert.Equal(first.Events, second.Events);
        Assert.Equal(first.FinalSave, second.FinalSave);
        Assert.Equal(2, first.Events.Count);
    }

    [Fact]
    public void Harness_validates_null_scripts_and_commands()
    {
        Assert.Throws<ArgumentNullException>(() => SimulationTestHarness.Run(1, null!, []));
        Assert.Throws<ArgumentNullException>(() => SimulationTestHarness.Run(1, RegisterHandlers, null!));
        Assert.Throws<ArgumentNullException>(() => SimulationTestHarness.Run(1, RegisterHandlers, [null!]));
        Assert.Throws<InvalidOperationException>(() => SimulationTestHarness.Run(1, _ => { }, [dispatcher => null!]));
        SimulationTestHarness.Run(1, RegisterHandlers, [], null);
        Assert.Throws<ArgumentNullException>(() => SimulationTestHarness.AssertDeterministic(1, RegisterHandlers, null!));
    }

    [Fact]
    public void Harness_detects_nondeterministic_state_and_event_results()
    {
        var stateCalls = 0;
        var stateCommands = new Func<CommandDispatcher, CommandResult>[]
        {
            dispatcher => dispatcher.Dispatch(new AdvanceCommand(++stateCalls))
        };
        Assert.Throws<InvalidOperationException>(() => SimulationTestHarness.AssertDeterministic(
            1,
            RegisterHandlers,
            stateCommands));

        var eventCalls = 0;
        Assert.Throws<InvalidOperationException>(() => SimulationTestHarness.AssertDeterministic(
            1,
            dispatcher => dispatcher.Register<AdvanceCommand>((state, _) => new CommandResult(
                state, [new AdvancedEvent(++eventCalls)])),
            [dispatcher => dispatcher.Dispatch(new AdvanceCommand(0))]));
    }

    private static void RegisterHandlers(CommandDispatcher dispatcher)
        => dispatcher.Register<AdvanceCommand>((state, command) => new CommandResult(
            state with { SimulationTick = state.SimulationTick + command.Ticks },
            [new AdvancedEvent(command.Ticks)]));

    private static void RegisterRngHandlers(CommandDispatcher dispatcher)
        => dispatcher.Register<RollCommand>((state, command) =>
        {
            var stream = new DeterministicRng(state.WorldSeed, state.Versions.GeneratorVersion)
                .CreateStream(command.StreamName, $"tick:{state.SimulationTick}");
            var roll = stream.NextInt(0, 100);
            return new CommandResult(
                state with
                {
                    SimulationTick = state.SimulationTick + 1,
                    Player = state.Player with { Experience = state.Player.Experience + roll }
                },
                [new RolledEvent(command.StreamName, roll)]);
        });

    private sealed record AdvanceCommand(int Ticks) : ICommand;
    private sealed record AdvancedEvent(int Ticks) : IDomainEvent;
    private sealed record RollCommand(string StreamName) : ICommand;
    private sealed record RolledEvent(string StreamName, int Value) : IDomainEvent;
}
