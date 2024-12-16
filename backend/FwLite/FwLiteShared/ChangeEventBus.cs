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

    public IObservable<Entry> OnProjectEntryUpdated(CrdtProject project)
    {
        var projectName = project.Name;
        return _entryUpdated
            .Where(n => n.ProjectName == projectName)
            .Select(n => n.Entry);
    }
    public IObservable<Entry> OnEntryUpdated
    {
        get
        {
            return OnProjectEntryUpdated(projectContext.Project ?? throw new InvalidOperationException("Not in a project"));
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
