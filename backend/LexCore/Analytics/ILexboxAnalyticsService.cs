using LexCore.Auth;

namespace LexCore.Analytics;

/// <summary>
/// LexBox-side analytics: sends <c>product=lexbox</c> events to the shared Mixpanel project.
/// The implementation resolves the current user and attaches it to each event. The FwLite counterpart
/// is FwLiteShared's <c>AnalyticsService</c>; both sit on the LexCore Mixpanel core.
/// </summary>
public interface ILexboxAnalyticsService
{
    public const string SendReceiveEvent = "send_receive";
    public const string FwLiteSyncEvent = "fw_lite_sync";
    public const string LoginCompletedEvent = "login_completed";
    public const string AccountCreatedEvent = "account_created";

    /// <summary>The event property carrying how the user authenticated ("password" or "google").</summary>
    public const string LoginTypeProperty = "login_type";
    public const string PasswordLoginType = "password";
    public const string GoogleLoginType = "google";

    /// <summary>The event property carrying how the account was created (see <see cref="AccountCreatedVia"/>).</summary>
    public const string CreatedViaProperty = "created_via";

    /// <summary>
    /// The <see cref="SendReceiveEvent"/> property carrying which way data moved: <see cref="SendDirection"/>
    /// (a push) or <see cref="ReceiveDirection"/> (a fetch). A full send/receive fires both, so this lets us
    /// deduplicate per operation later if needed.
    /// </summary>
    public const string DirectionProperty = "direction";
    public const string SendDirection = "send";
    public const string ReceiveDirection = "receive";

    /// <summary>
    /// Track a Mercurial send/receive for the current user. <paramref name="direction"/> is one of
    /// <see cref="SendDirection"/> (a push — unbundle / resumable pushBundleChunk) or <see cref="ReceiveDirection"/>
    /// (a fetch — getbundle / the first resumable pull chunk). Fire-and-forget: returns a <see cref="Task"/> the
    /// caller may discard. Sends nothing (a completed task) when analytics is disabled, no token is configured, the
    /// user has opted out, or there is no identified user — e.g. an automated service-account sync. Never throws.
    /// </summary>
    Task TrackSendReceive(string direction);

    /// <summary>
    /// Track a FieldWorks Lite (CRDT) sync for the current user, fired when a client fetches changes from the
    /// crdt controller. Fire-and-forget: sends nothing when analytics is disabled, no token is configured, the
    /// user has opted out, or there is no identified user. Never throws.
    /// </summary>
    Task TrackFwLiteSync();

    /// <summary>
    /// Track a successful login for <paramref name="user"/>. <paramref name="loginType"/> is one of
    /// <see cref="PasswordLoginType"/> or <see cref="GoogleLoginType"/>. The user is passed explicitly
    /// because the login request itself is anonymous (the sign-in cookie is only just being issued).
    /// Fire-and-forget: sends nothing when analytics is disabled, no token is configured, the user has
    /// opted out, or there is no identified user. Never throws.
    /// </summary>
    Task TrackLoginCompleted(LexAuthUser user, string loginType);

    /// <summary>
    /// Track a newly created user account, keyed to the new user's id. <paramref name="createdVia"/> describes
    /// which flow created it. The id is passed explicitly because the actor may be anonymous (self-registration)
    /// or a different user (admin/project flows). Pass <paramref name="optedOutOfAnalytics"/> = true to honour a
    /// choice the user made during creation (e.g. the register form's consent checkbox); the event is then not
    /// sent. Fire-and-forget: sends nothing when analytics is disabled, no token is configured, or the user opted
    /// out. Never throws.
    /// </summary>
    Task TrackAccountCreated(Guid userId, AccountCreatedVia createdVia, bool optedOutOfAnalytics = false);
}
