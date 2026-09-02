namespace FwLiteShared.Analytics;

/// <summary>
/// Disables Mixpanel when the process is running under CI (GitHub Actions, etc.).
/// </summary>
public sealed class CiAnalyticsSuppressor : IAnalyticsSuppressor
{
    public bool ShouldSuppress() => MixpanelAnalytics.IsCiEnvironment();
}
