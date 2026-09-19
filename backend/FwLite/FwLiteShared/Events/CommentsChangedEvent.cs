namespace FwLiteShared.Events;

/// <summary>
/// Coarse, project-scoped signal that comment threads, comments, or their local read status changed.
/// Carries no payload: consumers (comment panel, unread badge, unread-filtered entry list) simply re-query.
/// </summary>
public class CommentsChangedEvent : IFwEvent
{
    public FwEventType Type => FwEventType.CommentsChanged;
    public bool IsGlobal => false;
}
