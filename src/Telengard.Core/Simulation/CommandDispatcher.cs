using Telengard.Core.Events;

namespace Telengard.Core.Simulation;

public interface ICommand;

public interface IDomainEvent;

public sealed record CommandResult
{
    public CommandResult(GameState state, IEnumerable<IDomainEvent>? events = null)
    {
        State = state ?? throw new ArgumentNullException(nameof(state));
        Events = events?.ToArray() ?? Array.Empty<IDomainEvent>();
    }

    public GameState State { get; }
    public IReadOnlyList<IDomainEvent> Events { get; }
}

public sealed class CommandDispatcher
{
    private readonly Dictionary<Type, Func<GameState, ICommand, CommandResult>> _handlers = [];

    public CommandDispatcher(GameState initialState, DomainEventBus? eventBus = null)
    {
        CurrentState = initialState ?? throw new ArgumentNullException(nameof(initialState));
        EventBus = eventBus;
    }

    public GameState CurrentState { get; private set; }
    public DomainEventBus? EventBus { get; }

    public void Register<TCommand>(Func<GameState, TCommand, CommandResult> handler)
        where TCommand : ICommand
    {
        ArgumentNullException.ThrowIfNull(handler);

        if (!_handlers.TryAdd(typeof(TCommand), (state, command) => handler(state, (TCommand)command)))
        {
            throw new InvalidOperationException($"A handler is already registered for {typeof(TCommand).Name}.");
        }
    }

    public CommandResult Dispatch<TCommand>(TCommand command)
        where TCommand : ICommand
    {
        ArgumentNullException.ThrowIfNull(command);

        if (!_handlers.TryGetValue(typeof(TCommand), out var handler))
        {
            throw new InvalidOperationException($"No handler is registered for {typeof(TCommand).Name}.");
        }

        var result = handler(CurrentState, command)
            ?? throw new InvalidOperationException("A command handler returned no result.");
        CurrentState = result.State;
        EventBus?.Publish(result.Events);
        return result;
    }
}
