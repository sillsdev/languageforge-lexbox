using System.Reactive.Linq;
using System.Reactive.Subjects;
using LcmCrdt;
using MiniLcm.Models;

namespace FwLiteShared;

public class ChangeEventBus
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

    public void NotifyEntryUpdated(Entry entry, CrdtProject project)
    {
        _entryUpdated.OnNext(new ChangeNotification(entry, project.Name));
    }

    public void Dispose()
    {
        _entryUpdated.OnCompleted();
        _entryUpdated.Dispose();
    }
}
