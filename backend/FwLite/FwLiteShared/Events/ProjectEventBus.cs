using System.Reactive.Linq;
using LcmCrdt;
using Microsoft.Extensions.Logging;
using MiniLcm.Models;

namespace FwLiteShared.Events;

public class ProjectEventBus : IDisposable
{
    private IDisposable changeEventBusSubscription;

    private readonly GlobalEventBus _globalEventBus;
    private readonly ILogger<ProjectEventBus> _logger;

    public ProjectEventBus(GlobalEventBus globalEventBus,
        ILogger<ProjectEventBus> logger,
        ChangeEventBus changeEventBus)
    {
        _globalEventBus = globalEventBus;
        _logger = logger;
        changeEventBusSubscription = changeEventBus.OnProjectEntryUpdated().Subscribe(
            changeNotification =>
            {
                PublishEvent(changeNotification.Project, new EntryChangedEvent(changeNotification.Entry));
            });
    }

    public void PublishEvent(IProjectIdentifier project, IFwEvent @event)
    {
        if (@event.IsGlobal)
        {
            _globalEventBus.PublishEvent(@event);
            return;
        }
        _logger.LogInformation("Publishing project event {@event}", @event);
        _globalEventBus.PublishEvent(new ProjectEvent(@event, project));
    }

    private IObservable<T> OnProjectEvent<T>(IProjectIdentifier project) where T : IFwEvent
    {
        return _globalEventBus.OnGlobalEvent
            .OfType<ProjectEvent>()
            .Where(e => e.MatchesProject(project) && e.Event is T)
            .Select(e => (T)e.Event);
    }

    public IObservable<EntryChangedEvent> OnEntryChanged(IProjectIdentifier project)
    {
        return OnProjectEvent<EntryChangedEvent>(project);
    }

    public void Dispose()
    {
    }
}
