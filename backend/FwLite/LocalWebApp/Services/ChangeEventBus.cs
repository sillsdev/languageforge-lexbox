using System.Reactive.Linq;
using System.Reactive.Subjects;
using LcmCrdt;
using LcmCrdt.Objects;
using LocalWebApp.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace LocalWebApp.Services;

public class ChangeEventBus(ProjectContext projectContext, IHubContext<CrdtMiniLcmApiHub, ILexboxHubClient> hubContext)
    : IDisposable
{
    private IDisposable? _subscription;

    public void SetupSignalRSubscription()
    {
        if (_subscription is not null) return;
        _subscription = _entryUpdated.Subscribe(notification =>
        {
            _ = hubContext.Clients.Group(CrdtMiniLcmApiHub.ProjectGroup(notification.ProjectName)).OnEntryUpdated(notification.Entry);
        });
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
        _subscription?.Dispose();
    }
}
