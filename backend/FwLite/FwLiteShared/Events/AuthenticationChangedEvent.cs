namespace FwLiteShared.Events;

public record AuthenticationChangedEvent(string ServerId) : IFwEvent
{
    public FwEventType Type => FwEventType.AuthenticationChanged;
    public bool IsGlobal => true;
}
