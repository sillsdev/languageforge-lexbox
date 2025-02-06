using System.Reactive.Linq;
using System.Reactive.Subjects;
using Microsoft.Extensions.Logging;

namespace FwLiteShared.Events;

public class GlobalEventBus : IDisposable
{
    private readonly Subject<IFwEvent> _globalEventSubject = new();
    private readonly ILogger<GlobalEventBus> _logger;
    private IDisposable changeEventBusSubscription;

    public GlobalEventBus(ILogger<GlobalEventBus> logger, ChangeEventBus changeEventBus)
    {
        _logger = logger;
        changeEventBusSubscription = changeEventBus.OnProjectEntryUpdated().Subscribe(
            changeNotification =>
            {
                PublishEvent(new ProjectEvent(new EntryChangedEvent(changeNotification.Entry), changeNotification.Project));
            });
    }

    public IObservable<IFwEvent> OnGlobalEvent => _globalEventSubject;
    public IObservable<AuthenticationChangedEvent> OnAuthenticationChanged => OnGlobalEvent.OfType<AuthenticationChangedEvent>();
    public void PublishEvent(IFwEvent @event)
    {
        if (!@event.IsGlobal) throw new ArgumentException($"Event {@event.GetType()} is not global");
        _logger.LogTrace("Publishing global event {@event}", @event);
        _globalEventSubject.OnNext(@event);
    }

    public void Dispose()
    {
        _globalEventSubject.OnCompleted();
        _globalEventSubject.Dispose();
        changeEventBusSubscription?.Dispose();
    }
}
