using FwLiteShared.Auth;
using FwLiteShared.Events;
using LexCore.Analytics;

namespace FwLiteShared.Analytics;

/// <summary>
/// FwLite-specific Mixpanel helpers (identity from lexbox.org auth, app_launched, host names).
/// The product-agnostic transport, tokens and CI detection live in <see cref="LexCore.Analytics"/>
/// and are re-exposed here for FwLite callers and tests.
/// </summary>
public static class MixpanelAnalytics
{
    /// <summary>Mixpanel debug/test project token. Decoded at runtime; not a secret.</summary>
    public static string DebugProjectToken => MixpanelTokens.DebugProjectToken;

    /// <summary>Mixpanel release/production project token. Decoded at runtime; not a secret.</summary>
    public static string ProductionProjectToken => MixpanelTokens.ProductionProjectToken;

    public const string TrackUrl = MixpanelClient.TrackUrl;
    public const string HttpClientName = MixpanelClient.HttpClientName;
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
    public static string? SelectToken(bool isDevelopment, AnalyticsConfig config) =>
        MixpanelTokens.SelectToken(isDevelopment, config.DebugProjectToken, config.ProductionToken);

    public static bool IsProductionLexbox(LexboxServer server) =>
        string.Equals(server.Authority.Host, ProductionLexboxHost, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// True when the process is running under CI. GitHub Actions sets <c>CI</c> and <c>GITHUB_ACTIONS</c>.
    /// </summary>
    public static bool IsCiEnvironment(IReadOnlyDictionary<string, string?>? environmentVariables = null) =>
        AnalyticsCiEnvironment.IsCiEnvironment(environmentVariables);

    public static bool IsTruthyEnv(string? value) => AnalyticsCiEnvironment.IsTruthyEnv(value);

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
            case AuthenticationChangeCause.Refresh:
            case AuthenticationChangeCause.SessionExpired:
            case AuthenticationChangeCause.Login:
                break;
        }
    }
}
