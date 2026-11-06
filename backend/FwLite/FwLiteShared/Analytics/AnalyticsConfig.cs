using LexCore.Analytics;

namespace FwLiteShared.Analytics;

public class AnalyticsConfig : AnalyticsConfigBase
{
    public AnalyticsConfig()
    {
        Product = MixpanelProducts.FwLite;
    }

    /// <summary>Mixpanel super property <c>host</c>: <c>maui</c> or <c>web</c>.</summary>
    public string? Host { get; set; }
}
