using Telengard.Core.Simulation;

namespace Telengard.Core.Events;

public sealed class DomainEventBus
{
    private readonly object _gate = new();
    private readonly List<(Type EventType, Delegate Handler)> _handlers = [];

    public IDisposable Subscribe<TEvent>(Action<TEvent> handler)
        where TEvent : IDomainEvent
    {
        ArgumentNullException.ThrowIfNull(handler);

        lock (_gate)
        {
            _handlers.Add((typeof(TEvent), handler));
        }

        return new Subscription(() => Unsubscribe(typeof(TEvent), handler));
    }

    public void Publish(IEnumerable<IDomainEvent> events)
    {
        ArgumentNullException.ThrowIfNull(events);

        foreach (var domainEvent in events)
        {
            ArgumentNullException.ThrowIfNull(domainEvent);
            Publish(domainEvent);
        }
    }

    public void Publish(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        Delegate[] handlers;
        lock (_gate)
        {
            handlers = _handlers
                .Where(pair => pair.EventType.IsAssignableFrom(domainEvent.GetType()))
                .Select(pair => pair.Handler)
                .ToArray();
        }

        foreach (var handler in handlers)
        {
            handler.DynamicInvoke(domainEvent);
        }
    }

    private void Unsubscribe(Type eventType, Delegate handler)
    {
        lock (_gate)
        {
            _handlers.Remove((eventType, handler));
        }
    }

    private sealed class Subscription(Action dispose) : IDisposable
    {
        private Action? _dispose = dispose;

        public void Dispose() => Interlocked.Exchange(ref _dispose, null)?.Invoke();
    }
}
