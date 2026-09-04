namespace FwLiteShared.Analytics;

public class AnalyticsConfig
{
    /// <summary>Mixpanel super property <c>host</c>: <c>maui</c> or <c>web</c>.</summary>
    public string? Host { get; set; }

    /// <summary>
    /// Master switch. When false, nothing is sent and <c>GetAnalyticsEnabled</c> is false
    /// regardless of the user's opt-out preference. Override with <c>Analytics__Enabled</c>.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Mixpanel debug/test project token. Not a secret; encoded in source and decoded at runtime.
    /// </summary>
    public string DebugProjectToken { get; set; } = MixpanelAnalytics.DebugProjectToken;

    /// <summary>
    /// Release Mixpanel token. Defaults to the production project token (encoded in source).
    /// Null/empty means do not send.
    /// </summary>
    public string? ProductionToken { get; set; } = MixpanelAnalytics.ProductionProjectToken;
}
