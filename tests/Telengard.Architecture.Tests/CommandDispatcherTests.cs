using Telengard.Core.Events;
using Telengard.Core.Simulation;
using Xunit;

namespace Telengard.Architecture.Tests;

public sealed class CommandDispatcherTests
{
    [Fact]
    public void Dispatch_commits_handler_state_and_returns_events()
    {
        var initial = GameState.Create(1234);
        var dispatcher = new CommandDispatcher(initial);
        dispatcher.Register<AdvanceCommand>((state, command) => new CommandResult(
            state with { SimulationTick = state.SimulationTick + command.Ticks },
            [new AdvancedEvent(command.Ticks)]));

        var result = dispatcher.Dispatch(new AdvanceCommand(3));

        Assert.Equal(3, result.State.SimulationTick);
        Assert.Equal(result.State, dispatcher.CurrentState);
        var domainEvent = Assert.IsType<AdvancedEvent>(Assert.Single(result.Events));
        Assert.Equal(3, domainEvent.Ticks);
    }

    [Fact]
    public void Dispatch_rejects_unregistered_commands_without_changing_state()
    {
        var initial = GameState.Create(1234);
        var dispatcher = new CommandDispatcher(initial);

        Assert.Throws<InvalidOperationException>(() => dispatcher.Dispatch(new AdvanceCommand(1)));
        Assert.Equal(initial, dispatcher.CurrentState);
    }

    [Fact]
    public void Dispatch_does_not_commit_when_the_handler_fails()
    {
        var initial = GameState.Create(1234);
        var dispatcher = new CommandDispatcher(initial);
        dispatcher.Register<AdvanceCommand>((_, _) => throw new InvalidOperationException("invalid command"));

        Assert.Throws<InvalidOperationException>(() => dispatcher.Dispatch(new AdvanceCommand(1)));
        Assert.Equal(initial, dispatcher.CurrentState);
    }

    [Fact]
    public void Register_rejects_duplicate_command_handlers()
    {
        var dispatcher = new CommandDispatcher(GameState.Create(1234));
        dispatcher.Register<AdvanceCommand>((state, _) => new CommandResult(state));

        Assert.Throws<InvalidOperationException>(() =>
            dispatcher.Register<AdvanceCommand>((state, _) => new CommandResult(state)));
    }

    [Fact]
    public void Dispatch_publishes_events_after_committing_state()
    {
        var dispatcher = new CommandDispatcher(GameState.Create(1234), new DomainEventBus());
        var observedState = 0L;
        dispatcher.EventBus!.Subscribe<AdvancedEvent>(_ => observedState = dispatcher.CurrentState.SimulationTick);
        dispatcher.Register<AdvanceCommand>((state, command) => new CommandResult(
            state with { SimulationTick = state.SimulationTick + command.Ticks },
            [new AdvancedEvent(command.Ticks)]));

        dispatcher.Dispatch(new AdvanceCommand(3));

        Assert.Equal(3, observedState);
    }

    [Fact]
    public void Event_bus_preserves_event_and_subscription_order_and_supports_unsubscribe()
    {
        var bus = new DomainEventBus();
        var received = new List<string>();
        bus.Subscribe<AdvancedEvent>(e => received.Add($"typed:{e.Ticks}"));
        var first = bus.Subscribe<IDomainEvent>(_ => received.Add("all"));

        bus.Publish([new AdvancedEvent(1), new AdvancedEvent(2)]);
        first.Dispose();
        first.Dispose();
        bus.Publish(new AdvancedEvent(3));

        Assert.Equal(["typed:1", "all", "typed:2", "all", "typed:3"], received);
    }

    [Fact]
    public void Event_bus_rejects_null_inputs_and_null_events()
    {
        var bus = new DomainEventBus();

        Assert.Throws<ArgumentNullException>(() => bus.Subscribe<AdvancedEvent>(null!));
        Assert.Throws<ArgumentNullException>(() => bus.Publish((IEnumerable<IDomainEvent>)null!));
        Assert.Throws<ArgumentNullException>(() => bus.Publish((IDomainEvent)null!));
        Assert.Throws<ArgumentNullException>(() => bus.Publish(new IDomainEvent[] { null! }));
    }

    [Fact]
    public void Dispatch_does_not_publish_when_the_handler_fails()
    {
        var bus = new DomainEventBus();
        var published = 0;
        bus.Subscribe<AdvancedEvent>(_ => published++);
        var dispatcher = new CommandDispatcher(GameState.Create(1234), bus);
        dispatcher.Register<AdvanceCommand>((_, _) => throw new InvalidOperationException("invalid command"));

        Assert.Throws<InvalidOperationException>(() => dispatcher.Dispatch(new AdvanceCommand(1)));
        Assert.Equal(0, published);
    }

    [Fact]
    public void Dispatcher_and_command_results_validate_null_boundaries()
    {
        Assert.Throws<ArgumentNullException>(() => new CommandResult(null!));
        Assert.Empty(new CommandResult(GameState.Create(1234)).Events);
        Assert.Throws<ArgumentNullException>(() => new CommandDispatcher(null!));

        var dispatcher = new CommandDispatcher(GameState.Create(1234));
        Assert.Throws<ArgumentNullException>(() => dispatcher.Register<AdvanceCommand>(null!));
        Assert.Throws<ArgumentNullException>(() => dispatcher.Dispatch<AdvanceCommand>(null!));

        dispatcher.Register<AdvanceCommand>((_, _) => null!);
        Assert.Throws<InvalidOperationException>(() => dispatcher.Dispatch(new AdvanceCommand(1)));
    }

    private sealed record AdvanceCommand(int Ticks) : ICommand;

    private sealed record AdvancedEvent(int Ticks) : IDomainEvent;
}
