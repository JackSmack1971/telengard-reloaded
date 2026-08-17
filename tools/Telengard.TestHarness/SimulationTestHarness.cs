using Telengard.Core.Simulation;
using Telengard.Save;

namespace Telengard.TestHarness;

public sealed record SimulationRunResult(
    GameState FinalState,
    IReadOnlyList<IDomainEvent> Events,
    string FinalSave);

public static class SimulationTestHarness
{
    public static SimulationRunResult Run(
        long seed,
        Action<CommandDispatcher> registerHandlers,
        IEnumerable<Func<CommandDispatcher, CommandResult>> commands,
        IEnumerable<int>? saveAndReloadAfter = null)
    {
        ArgumentNullException.ThrowIfNull(registerHandlers);
        ArgumentNullException.ThrowIfNull(commands);

        var reloadAfter = saveAndReloadAfter?.ToHashSet() ?? [];
        var dispatcher = CreateDispatcher(seed, registerHandlers);
        var events = new List<IDomainEvent>();
        var commandNumber = 0;

        foreach (var command in commands)
        {
            ArgumentNullException.ThrowIfNull(command);
            var result = command(dispatcher) ?? throw new InvalidOperationException("A scripted command returned no result.");
            events.AddRange(result.Events);
            commandNumber++;

            if (reloadAfter.Contains(commandNumber))
            {
                var save = SaveGameSerializer.Serialize(dispatcher.CurrentState);
                dispatcher = CreateDispatcher(SaveGameSerializer.Deserialize(save), registerHandlers);
            }
        }

        var finalSave = SaveGameSerializer.Serialize(dispatcher.CurrentState);
        return new SimulationRunResult(dispatcher.CurrentState, events, finalSave);
    }

    public static void AssertDeterministic(
        long seed,
        Action<CommandDispatcher> registerHandlers,
        IEnumerable<Func<CommandDispatcher, CommandResult>> commands,
        IEnumerable<int>? saveAndReloadAfter = null)
    {
        ArgumentNullException.ThrowIfNull(commands);
        var scriptedCommands = commands.ToArray();
        var checkpoints = saveAndReloadAfter?.ToArray();
        var first = Run(seed, registerHandlers, scriptedCommands, checkpoints);
        var second = Run(seed, registerHandlers, scriptedCommands, checkpoints);

        if (first.FinalSave != second.FinalSave || !EventSignatures(first.Events).SequenceEqual(EventSignatures(second.Events)))
        {
            throw new InvalidOperationException("The scripted simulation was not deterministic.");
        }
    }

    private static CommandDispatcher CreateDispatcher(long seed, Action<CommandDispatcher> registerHandlers)
        => CreateDispatcher(GameState.Create(seed), registerHandlers);

    private static CommandDispatcher CreateDispatcher(GameState state, Action<CommandDispatcher> registerHandlers)
    {
        var dispatcher = new CommandDispatcher(state);
        registerHandlers(dispatcher);
        return dispatcher;
    }

    private static IEnumerable<string> EventSignatures(IEnumerable<IDomainEvent> events)
        => events.Select(domainEvent => $"{domainEvent.GetType().AssemblyQualifiedName}:{domainEvent}");
}
