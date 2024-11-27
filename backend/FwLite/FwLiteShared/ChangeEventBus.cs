using System.Reactive.Linq;
using System.Reactive.Subjects;
using LcmCrdt;
using MiniLcm.Models;

namespace FwLiteShared;

public class ChangeEventBus(ProjectContext projectContext)
    : IDisposable
{

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
