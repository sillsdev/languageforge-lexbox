namespace FwLiteShared.Events;

public class EntriesChangedEvent(Guid[] changedEntryIds, Guid[] deletedEntryIds) : IFwEvent
{
    public FwEventType Type => FwEventType.EntriesChanged;
    public bool IsGlobal => false;
    public Guid[] ChangedEntryIds { get; init; } = changedEntryIds;
    public Guid[] DeletedEntryIds { get; init; } = deletedEntryIds;
}
