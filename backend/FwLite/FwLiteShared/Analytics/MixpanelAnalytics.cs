using FwLiteShared.Auth;

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

    /// <summary>
    /// Debug / UseDevAssets uses the hardcoded debug token. Release uses <paramref name="releaseToken"/>;
    /// empty means do not send.
    /// </summary>
    public static string? SelectToken(bool isDevelopment, bool useDevAssets, string? releaseToken = null)
    {
        if (isDevelopment || useDevAssets)
            return DebugProjectToken;
        return string.IsNullOrWhiteSpace(releaseToken) ? null : releaseToken;
    }

    public static bool IsProductionLexbox(LexboxServer server) =>
        string.Equals(server.Authority.Host, ProductionLexboxHost, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Identify only for production lexbox.org. Logout (null user) resets. Empty <see cref="LexboxUser.Id"/> stays anonymous.
    /// Non-lexbox.org servers are ignored (no identify, no reset).
    /// </summary>
    public static void ApplyAuthChange(IAnalyticsService analytics, LexboxServer server, LexboxUser? user)
    {
        if (!IsProductionLexbox(server))
            return;
        if (user is null)
            analytics.Reset();
        else if (!string.IsNullOrWhiteSpace(user.Id))
            analytics.Identify(user.Id);
    }
}
