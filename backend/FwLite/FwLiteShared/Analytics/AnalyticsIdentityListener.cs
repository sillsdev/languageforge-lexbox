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
    ILogger<AnalyticsIdentityListener> logger,
    IHostApplicationLifetime? applicationLifetime = null) : IHostedService, IDisposable
{
    private IDisposable? _subscription;

    // Serializes identity mutations so a logout can't be overwritten by an in-flight
    // startup/login GetCurrentUser that resolves with a now-stale account.
    private readonly SemaphoreSlim _identityGate = new(1, 1);

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
        _identityGate.Dispose();
    }

    /// <summary>
    /// Applies one identity change under <see cref="_identityGate"/> so login and logout can't interleave.
    /// The <paramref name="getCurrentUser"/> lookup runs inside the gate (login only) so a logout waiting on
    /// the gate always wins over a startup/login lookup that resolves with a stale account.
    /// </summary>
    internal async Task RunIdentityOperation(
        LexboxServer server,
        AuthenticationChangeCause cause,
        Func<Task<LexboxUser?>> getCurrentUser,
        CancellationToken cancellationToken = default)
    {
        await _identityGate.WaitAsync(cancellationToken);
        try
        {
            var user = cause == AuthenticationChangeCause.Login ? await getCurrentUser() : null;
            MixpanelAnalytics.ApplyAuthChange(analytics, server, cause, user);
        }
        finally
        {
            _identityGate.Release();
        }
    }

    internal async Task IdentifyAtStart(CancellationToken cancellationToken = default)
    {
        try
        {
            // Web: OAuthClient needs the listening address (IRedirectUrlProvider / UrlContext).
            // That is only available after ApplicationStarted. MAUI has no IHostApplicationLifetime.
            await WaitUntilStarted(cancellationToken);
            var server = authConfig.Value.LexboxServers.FirstOrDefault(MixpanelAnalytics.IsProductionLexbox);
            if (server is null)
                return;
            // Offline / expired token yields a null user: ApplyAuthChange(Login, null) is a no-op, so the
            // persisted AnalyticsUserId is kept and $device_id is not rotated on every process start.
            await RunIdentityOperation(server,
                AuthenticationChangeCause.Login,
                () => clientFactory.GetClient(server).GetCurrentUser(),
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Shutdown before identify completed.
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
                case AuthenticationChangeCause.Logout:
                    await RunIdentityOperation(changed.Server,
                        changed.Cause,
                        () => clientFactory.GetClient(changed.Server).GetCurrentUser());
                    break;
            }
        }
        catch (Exception e)
        {
            logger.LogWarning(e, "Failed to apply Mixpanel identity after authentication change");
        }
    }

    private Task WaitUntilStarted(CancellationToken cancellationToken)
    {
        if (applicationLifetime is null)
            return Task.CompletedTask;
        var tcs = new TaskCompletionSource();
        applicationLifetime.ApplicationStarted.Register(() => tcs.TrySetResult());
        cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken));
        applicationLifetime.ApplicationStopping.Register(() => tcs.TrySetCanceled());
        return tcs.Task;
    }
}
