using MiniLcm.Models;

namespace FwLiteShared.Events;

public record EntryChangedEvent(Entry Entry) : IFwEvent
{
    public FwEventType Type => FwEventType.EntryChanged;
    public bool IsGlobal => false;
}
