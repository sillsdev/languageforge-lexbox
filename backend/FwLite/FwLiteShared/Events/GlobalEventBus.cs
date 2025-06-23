using System.Reactive.Linq;
using System.Reactive.Subjects;
using Microsoft.Extensions.Logging;

namespace FwLiteShared.Events;

public class GlobalEventBus(ILogger<GlobalEventBus> logger) : IDisposable
{
    private readonly Subject<IFwEvent> _globalEventSubject = new();

    public IObservable<IFwEvent> OnGlobalEvent => _globalEventSubject;
    public IObservable<AuthenticationChangedEvent> OnAuthenticationChanged => OnGlobalEvent.OfType<AuthenticationChangedEvent>();
    private readonly Dictionary<FwEventType, IFwEvent> _lastEvent = new();

    public void PublishEvent(IFwEvent @event)
    {
        if (!@event.IsGlobal) throw new ArgumentException($"Event {@event.GetType()} is not global");
        logger.LogTrace("Publishing global event {@event}", @event);
        _globalEventSubject.OnNext(@event);
        _lastEvent[@event.Type] = @event;
    }

    public IFwEvent? GetLastEvent(FwEventType type)
    {
        return _lastEvent.TryGetValue(type, out var result) ? result : null;
    }

    public void Dispose()
    {
        _globalEventSubject.OnCompleted();
        _globalEventSubject.Dispose();
    }
}
