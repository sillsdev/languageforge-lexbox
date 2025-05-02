namespace FwLiteShared.Events;

public class EntryDeletedEvent(Guid entryId) : IFwEvent
{
    public FwEventType Type => FwEventType.EntryDeleted;
    public bool IsGlobal => false;
    public Guid EntryId { get; init; } = entryId;
}
