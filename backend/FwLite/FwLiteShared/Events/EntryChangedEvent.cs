using MiniLcm.Models;

namespace FwLiteShared.Events;

public class EntryChangedEvent(Entry entry) : IFwEvent
{
    public FwEventType Type => FwEventType.EntryChanged;
    public bool IsGlobal => false;
    public Entry Entry { get; init; } = entry;
}
