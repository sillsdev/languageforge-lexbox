using System.Reactive.Linq;
using System.Reactive.Subjects;
using LcmCrdt;
using LcmCrdt.Objects;
using LocalWebApp.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;

namespace LocalWebApp.Services;

public class ChangeEventBus(
    ProjectContext projectContext,
    IHubContext<CrdtMiniLcmApiHub, ILexboxHubClient> hubContext,
    ILogger<ChangeEventBus> logger,
    IMemoryCache cache)
    : IDisposable
{
    public IDisposable ListenForEntryChanges(string projectName, string connectionId) =>
        _entryUpdated
            .Where(n => n.ProjectName == projectName)
            .Subscribe(n => OnEntryChangedExternal(n.Entry, connectionId));

    private void OnEntryChangedExternal(Entry e, string connectionId)
    {
        var currentFilter = CrdtMiniLcmApiHub.CurrentProjectFilter(cache, connectionId);
        if (currentFilter.Invoke(e))
        {
            _ = hubContext.Clients.Client(connectionId).OnEntryUpdated(e);
        }
    }

    private record struct ChangeNotification(Entry Entry, string ProjectName);

    private readonly Subject<ChangeNotification> _entryUpdated = new();

    public IObservable<Entry> OnEntryUpdated
    {
        get
        {
            var projectName = projectContext.Project?.Name ?? throw new InvalidOperationException("Not in a project");
            return _entryUpdated
                .Where(n => n.ProjectName == projectName)
                .Select(n => n.Entry);
        }
    }

    public void NotifyEntryUpdated(Entry entry)
    {
        _entryUpdated.OnNext(new ChangeNotification(entry,
            projectContext.Project?.Name ?? throw new InvalidOperationException("Not in a project")));
    }

    public void Dispose()
    {
        _entryUpdated.OnCompleted();
        _entryUpdated.Dispose();
    }
}
