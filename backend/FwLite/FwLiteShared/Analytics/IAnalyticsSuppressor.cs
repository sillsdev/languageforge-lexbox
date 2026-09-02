namespace FwLiteShared.Analytics;

/// <summary>
/// Host-specific reasons to keep Mixpanel off (CI, Play pre-launch, etc.).
/// </summary>
public interface IAnalyticsSuppressor
{
    bool ShouldSuppress();
}
