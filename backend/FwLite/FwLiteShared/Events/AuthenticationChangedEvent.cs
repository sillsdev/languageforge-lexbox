using System.Text.Json.Serialization;
using FwLiteShared.Auth;

namespace FwLiteShared.Events;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AuthenticationChangeCause
{
    Login,
    Logout,
    /// <summary>Successful silent token refresh. Not published today; reserved so consumers can ignore it.</summary>
    Refresh,
    /// <summary>Silent token acquisition failed permanently (expired/revoked refresh token) and the local account was removed.</summary>
    SessionExpired,
}

public record AuthenticationChangedEvent(LexboxServer Server, AuthenticationChangeCause Cause) : IFwEvent
{
    public FwEventType Type => FwEventType.AuthenticationChanged;
    public bool IsGlobal => true;
}
