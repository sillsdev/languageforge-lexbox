using LexCore.Auth;

namespace LexCore.Analytics;

/// <summary>
/// LexBox-side analytics: sends <c>product=lexbox</c> events to the shared Mixpanel project.
/// The implementation resolves the current user and attaches it to each event. The FwLite counterpart
/// is FwLiteShared's <c>AnalyticsService</c>; both sit on the LexCore Mixpanel core.
/// </summary>
public interface ILexboxAnalyticsService
{
    public const string SendReceiveCompletedEvent = "send_receive_completed";
    public const string LoginCompletedEvent = "login_completed";

    /// <summary>The event property carrying how the user authenticated ("password" or "google").</summary>
    public const string LoginTypeProperty = "login_type";
    public const string PasswordLoginType = "password";
    public const string GoogleLoginType = "google";

    /// <summary>
    /// Track a completed send/receive for the current user. Fire-and-forget: returns a <see cref="Task"/>
    /// the caller may discard. Sends nothing (a completed task) when analytics is disabled, no token is
    /// configured, or there is no identified user — e.g. an automated service-account sync. Never throws.
    /// </summary>
    Task TrackSendReceiveCompleted();

    /// <summary>
    /// Track a successful login for <paramref name="user"/>. <paramref name="loginType"/> is one of
    /// <see cref="PasswordLoginType"/> or <see cref="GoogleLoginType"/>. The user is passed explicitly
    /// because the login request itself is anonymous (the sign-in cookie is only just being issued).
    /// Fire-and-forget: sends nothing when analytics is disabled, no token is configured, the user has
    /// opted out, or there is no identified user. Never throws.
    /// </summary>
    Task TrackLoginCompleted(LexAuthUser user, string loginType);
}
