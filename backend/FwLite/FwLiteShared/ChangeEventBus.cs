using System.Reactive.Linq;
using System.Reactive.Subjects;
using LcmCrdt;
using MiniLcm.Models;

namespace FwLiteShared;

public class ChangeEventBus
    : IDisposable
{

    public record struct ChangeNotification(Entry Entry, CrdtProject Project);

    private readonly Subject<ChangeNotification> _entryUpdated = new();
    public IObservable<ChangeNotification> OnProjectEntryUpdated()
    {
        return _entryUpdated;
    }

    public IObservable<Entry> OnProjectEntryUpdated(CrdtProject project)
    {
        var projectName = project.Name;
        return _entryUpdated
            .Where(n => n.Project.Name == projectName)
            .Select(n => n.Entry);
    }

    public void NotifyEntryUpdated(Entry entry, CrdtProject project)
    {
        _entryUpdated.OnNext(new ChangeNotification(entry, project));
    }

    public void Dispose()
    {
        _entryUpdated.OnCompleted();
        _entryUpdated.Dispose();
    }
}
