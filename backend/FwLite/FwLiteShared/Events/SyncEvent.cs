using LexCore.Sync;

namespace FwLiteShared.Events;

public class SyncEvent(SyncStatus status) : IFwEvent
{
    public FwEventType Type => FwEventType.Sync;
    public bool IsGlobal => false;
    public SyncStatus Status { get; init; } = status;
}
