using FwLiteShared.Auth;

namespace FwLiteShared.Events;

public record AuthenticationChangedEvent(LexboxServer Server) : IFwEvent
{
    public FwEventType Type => FwEventType.AuthenticationChanged;
    public bool IsGlobal => true;
}
