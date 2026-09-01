namespace FwLiteShared.Analytics;

public class AnalyticsConfig
{
    /// <summary>Mixpanel super property <c>host</c>: <c>maui</c> or <c>web</c>.</summary>
    public string? Host { get; set; }

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
