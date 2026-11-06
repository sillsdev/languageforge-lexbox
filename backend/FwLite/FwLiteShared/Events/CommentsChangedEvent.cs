namespace FwLiteShared.Events;

/// <summary>
/// Comment threads or comments changed; consumers re-query. Local read-status writes don't fire it.
/// </summary>
public class CommentsChangedEvent : IFwEvent
{
    public FwEventType Type => FwEventType.CommentsChanged;
    public bool IsGlobal => false;
}
