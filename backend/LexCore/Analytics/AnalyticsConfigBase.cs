namespace LexCore.Analytics;

/// <summary>
/// Configuration shared by every product's analytics pipeline. Bound from the <c>Analytics</c>
/// configuration section. Products may derive from this to add product-specific settings.
/// </summary>
public class AnalyticsConfigBase
{
    /// <summary>
    /// Master switch. When false, nothing is sent regardless of any per-user opt-out.
    /// Override with <c>Analytics__Enabled</c> (e.g. disabled in k8s except production).
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Mixpanel debug/test project token. Not a secret; encoded in source and decoded at runtime.
    /// </summary>
    public string DebugProjectToken { get; set; } = MixpanelTokens.DebugProjectToken;

    /// <summary>
    /// Release Mixpanel token. Defaults to the production project token (encoded in source).
    /// Null/empty means do not send.
    /// </summary>
    public string? ProductionToken { get; set; } = MixpanelTokens.ProductionProjectToken;

    /// <summary>
    /// Value of the Mixpanel <c>product</c> super-property (see <see cref="MixpanelProducts"/>),
    /// distinguishing which product an event came from in the unified Mixpanel project.
    /// </summary>
    public string? Product { get; set; }
}
