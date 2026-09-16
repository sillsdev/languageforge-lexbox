namespace LexCore.Analytics;

/// <summary>
/// LexBox-side analytics: sends <c>product=lexbox</c> events to the shared Mixpanel project.
/// The implementation resolves the current user and attaches it to each event. The FwLite counterpart
/// is FwLiteShared's <c>AnalyticsService</c>; both sit on the LexCore Mixpanel core.
/// </summary>
public interface ILexboxAnalyticsService
{
    public const string SendReceiveCompletedEvent = "send_receive_completed";

    /// <summary>
    /// Track a completed send/receive for the current user. Fire-and-forget: returns a <see cref="Task"/>
    /// the caller may discard. Sends nothing (a completed task) when analytics is disabled, no token is
    /// configured, or there is no identified user — e.g. an automated service-account sync. Never throws.
    /// </summary>
    Task TrackSendReceiveCompleted();
}
