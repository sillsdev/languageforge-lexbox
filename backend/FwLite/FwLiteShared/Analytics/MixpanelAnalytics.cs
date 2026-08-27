using FwLiteShared.Auth;
using FwLiteShared.Events;

namespace FwLiteShared.Analytics;

public static class MixpanelAnalytics
{
    /// <summary>
    /// Mixpanel debug/test project token. Not a secret; hardcoded by design for this MVP.
    /// </summary>
    public const string DebugProjectToken = "5b901726cd330cf6fa1d270fe3c705e8";

    public const string TrackUrl = "https://api.mixpanel.com/track";
    public const string HttpClientName = "Mixpanel";
    public const string ProductionLexboxHost = "lexbox.org";
    public const string AppLaunchedEvent = "app_launched";
    public const string MauiHost = "maui";
    public const string WebHost = "web";

    /// <summary>
    /// Fire <see cref="AppLaunchedEvent"/> once per process start. Does not throw.
    /// </summary>
    public static void RecordProcessStart(IAnalyticsService analytics)
    {
        analytics.Track(AppLaunchedEvent);
    }

    /// <summary>
    /// Development uses <see cref="AnalyticsConfig.DebugProjectToken"/>.
    /// Release uses <see cref="AnalyticsConfig.ProductionToken"/>; empty means do not send.
    /// </summary>
    public static string? SelectToken(bool isDevelopment, AnalyticsConfig config)
    {
        var token = isDevelopment ? config.DebugProjectToken : config.ProductionToken;
        return string.IsNullOrWhiteSpace(token) ? null : token;
    }

    public static bool IsProductionLexbox(LexboxServer server) =>
        string.Equals(server.Authority.Host, ProductionLexboxHost, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Identify on lexbox.org login (persisted <c>$user_id</c>; a different user rotates <c>$device_id</c>).
    /// Reset only on explicit logout. Refresh and session expiry leave identity unchanged.
    /// Empty <see cref="LexboxUser.Id"/> stays anonymous. Non-lexbox.org servers are ignored.
    /// </summary>
    public static void ApplyAuthChange(
        IAnalyticsService analytics,
        LexboxServer server,
        AuthenticationChangeCause cause,
        LexboxUser? user)
    {
        if (!IsProductionLexbox(server))
            return;
        switch (cause)
        {
            case AuthenticationChangeCause.Login when !string.IsNullOrWhiteSpace(user?.Id):
                analytics.Identify(user.Id);
                break;
            case AuthenticationChangeCause.Logout:
                analytics.Reset();
                break;
        }
    }
}
