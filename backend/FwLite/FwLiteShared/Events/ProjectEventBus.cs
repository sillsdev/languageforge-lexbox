using System.Reactive.Linq;
using LcmCrdt;
using Microsoft.Extensions.Logging;
using MiniLcm.Models;

namespace FwLiteShared.Events;

public class ProjectEventBus : IDisposable
{

    private readonly GlobalEventBus _globalEventBus;
    private readonly ILogger<ProjectEventBus> _logger;

    public ProjectEventBus(GlobalEventBus globalEventBus,
        ILogger<ProjectEventBus> logger
        )
    {
        _globalEventBus = globalEventBus;
        _logger = logger;
    }

    public void PublishEvent(IProjectIdentifier project, IFwEvent @event)
    {
        if (@event.IsGlobal)
        {
            _globalEventBus.PublishEvent(@event);
            return;
        }
        _logger.LogTrace("Publishing project event {@event}", @event);
        _globalEventBus.PublishEvent(new ProjectEvent(@event, project));
    }

    public void PublishEntriesChanged(IProjectIdentifier project,
        Guid[] changedEntryIds,
        Guid[] deletedEntryIds)
    {
        PublishEvent(project, new EntriesChangedEvent(changedEntryIds, deletedEntryIds));
    }

    public void PublishEntryChanged(IProjectIdentifier project, Guid entryId)
    {
        PublishEntriesChanged(project, [entryId], []);
    }

    public void PublishEntryDeleted(IProjectIdentifier project, Guid entryId)
    {
        PublishEntriesChanged(project, [], [entryId]);
    }

    private IObservable<T> OnProjectEvent<T>(IProjectIdentifier project) where T : IFwEvent
    {
        return _globalEventBus.OnGlobalEvent
            .OfType<ProjectEvent>()
            .Where(e => e.MatchesProject(project) && e.Event is T)
            .Select(e => (T)e.Event);
    }

    public IObservable<EntriesChangedEvent> OnEntriesChanged(IProjectIdentifier project)
    {
        return OnProjectEvent<EntriesChangedEvent>(project);
    }

    public void Dispose()
    {
    }
}
