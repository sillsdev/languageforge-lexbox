using System.Reactive.Linq;
using System.Reactive.Subjects;
using Microsoft.Extensions.Logging;

namespace FwLiteShared.Events;

public class GlobalEventBus(ILogger<GlobalEventBus> logger) : IDisposable
{
    private readonly Subject<IFwEvent> _globalEventSubject = new();

    public IObservable<IFwEvent> OnGlobalEvent => _globalEventSubject;
    public IObservable<AuthenticationChangedEvent> OnAuthenticationChanged => OnGlobalEvent.OfType<AuthenticationChangedEvent>();
    public void PublishEvent(IFwEvent @event)
    {
        if (!@event.IsGlobal) throw new ArgumentException($"Event {@event.GetType()} is not global");
        logger.LogTrace("Publishing global event {@event}", @event);
        _globalEventSubject.OnNext(@event);
    }

    public void Dispose()
    {
        _globalEventSubject.OnCompleted();
        _globalEventSubject.Dispose();
    }
}
