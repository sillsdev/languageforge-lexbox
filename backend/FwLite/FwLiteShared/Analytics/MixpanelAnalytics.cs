namespace FwLiteShared.Analytics;

public static class MixpanelAnalytics
{
    /// <summary>
    /// Mixpanel debug/test project token. Not a secret; hardcoded by design for this MVP.
    /// </summary>
    public const string DebugProjectToken = "5b901726cd330cf6fa1d270fe3c705e8";

    public const string TrackUrl = "https://api.mixpanel.com/track";
    public const string HttpClientName = "Mixpanel";

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
}
