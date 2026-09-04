using System.Text;
using FwLiteShared.Auth;
using FwLiteShared.Events;

namespace FwLiteShared.Analytics;

public static class MixpanelAnalytics
{
    // Base64-encoded Mixpanel project tokens. These are write-only ingestion tokens, not secrets
    // (they're exposed in every client request), but they're encoded here so a plaintext token
    // string can't be trivially scraped from the public repo. Decoded once at runtime below.
    private const string DebugProjectTokenEncoded = "NWI5MDE3MjZjZDMzMGNmNmZhMWQyNzBmZTNjNzA1ZTg=";
    private const string ProductionProjectTokenEncoded = "YzA5ZDZhYmVjZWQ1MTE0YjBjM2YzMGY2ZjU1YmE3NjI=";

    /// <summary>Mixpanel debug/test project token. Decoded at runtime; not a secret.</summary>
    public static string DebugProjectToken { get; } = DecodeToken(DebugProjectTokenEncoded);

    /// <summary>Mixpanel release/production project token. Decoded at runtime; not a secret.</summary>
    public static string ProductionProjectToken { get; } = DecodeToken(ProductionProjectTokenEncoded);

    private static string DecodeToken(string encoded) => Encoding.UTF8.GetString(Convert.FromBase64String(encoded));

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
    /// True when the process is running under CI. GitHub Actions sets <c>CI</c> and <c>GITHUB_ACTIONS</c>.
    /// </summary>
    public static bool IsCiEnvironment(
        IReadOnlyDictionary<string, string?>? environmentVariables = null)
    {
        return IsTruthyEnv(ReadEnv(environmentVariables, "CI"))
            || IsTruthyEnv(ReadEnv(environmentVariables, "GITHUB_ACTIONS"));
    }

    /// <summary>
    /// Play pre-launch / Firebase Test Lab set Android setting <c>firebase.test.lab</c> to <c>true</c>.
    /// </summary>
    public static bool IsFirebaseTestLabSetting(string? value) => IsTruthyEnv(value);

    private static string? ReadEnv(IReadOnlyDictionary<string, string?>? environmentVariables, string key)
    {
        if (environmentVariables is not null)
            return environmentVariables.TryGetValue(key, out var value) ? value : null;
        return Environment.GetEnvironmentVariable(key);
    }

    private static bool IsTruthyEnv(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;
        return value.Equals("true", StringComparison.OrdinalIgnoreCase)
            || value.Equals("1", StringComparison.OrdinalIgnoreCase)
            || value.Equals("yes", StringComparison.OrdinalIgnoreCase);
    }

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
