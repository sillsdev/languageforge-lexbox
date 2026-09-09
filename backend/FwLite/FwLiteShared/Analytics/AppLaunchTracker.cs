using Microsoft.Extensions.Hosting;

namespace FwLiteShared.Analytics;

/// <summary>
/// Fires Mixpanel <c>app_launched</c> once when hosted services start (process start, not OS resume).
/// </summary>
public class AppLaunchTracker(IAnalyticsService analytics) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        MixpanelAnalytics.RecordProcessStart(analytics);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
