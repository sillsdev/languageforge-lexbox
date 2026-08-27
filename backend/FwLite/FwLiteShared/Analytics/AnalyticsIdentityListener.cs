using FwLiteShared.Auth;
using FwLiteShared.Events;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FwLiteShared.Analytics;

/// <summary>
/// Identifies Mixpanel from the production lexbox.org user at process start and on auth changes.
/// Staging/local logins never identify. Failures are logged and never block host startup.
/// </summary>
public class AnalyticsIdentityListener(
    IAnalyticsService analytics,
    OAuthClientFactory clientFactory,
    IOptions<AuthConfig> authConfig,
    GlobalEventBus eventBus,
    ILogger<AnalyticsIdentityListener> logger) : IHostedService, IDisposable
{
    private IDisposable? _subscription;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _subscription = eventBus.OnAuthenticationChanged.Subscribe(changed => _ = OnAuthenticationChanged(changed));
        _ = IdentifyAtStart(cancellationToken);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _subscription?.Dispose();
        _subscription = null;
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _subscription?.Dispose();
    }

    internal async Task IdentifyAtStart(CancellationToken cancellationToken = default)
    {
        try
        {
            var server = authConfig.Value.LexboxServers.FirstOrDefault(MixpanelAnalytics.IsProductionLexbox);
            if (server is null)
                return;
            var user = await clientFactory.GetClient(server).GetCurrentUser();
            // Offline / expired token: keep the persisted AnalyticsUserId already loaded by AnalyticsService.
            // Logged-out at start must not Reset — that would rotate $device_id on every process start.
            if (user is null)
                return;
            MixpanelAnalytics.ApplyAuthChange(analytics, server, AuthenticationChangeCause.Login, user);
        }
        catch (Exception e)
        {
            logger.LogWarning(e, "Failed to apply Mixpanel identity at process start");
        }
    }

    internal async Task OnAuthenticationChanged(AuthenticationChangedEvent changed)
    {
        try
        {
            if (!MixpanelAnalytics.IsProductionLexbox(changed.Server))
                return;
            switch (changed.Cause)
            {
                case AuthenticationChangeCause.Login:
                    var user = await clientFactory.GetClient(changed.Server).GetCurrentUser();
                    MixpanelAnalytics.ApplyAuthChange(analytics, changed.Server, changed.Cause, user);
                    break;
                case AuthenticationChangeCause.Logout:
                    MixpanelAnalytics.ApplyAuthChange(analytics, changed.Server, changed.Cause, user: null);
                    break;
            }
        }
        catch (Exception e)
        {
            logger.LogWarning(e, "Failed to apply Mixpanel identity after authentication change");
        }
    }
}
