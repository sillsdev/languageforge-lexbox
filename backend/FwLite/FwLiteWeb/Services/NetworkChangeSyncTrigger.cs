using System.Net.NetworkInformation;
using FwLiteShared.AppUpdate;
using FwLiteShared.Projects;

namespace FwLiteWeb.Services;

// Cross-platform counterpart to MAUI's ConnectivitySyncTrigger, for hosts without IConnectivity: when the OS
// reports network availability returning, re-ensure push listeners so a session started offline picks up
// without waiting on PushListenerRecoveryService's periodic backstop, and retry the startup update check. NetworkAvailabilityChanged is the
// System.Net.NetworkInformation analog of MAUI's ConnectivityChanged and is edge-triggered (it only fires on
// a change), so e.IsAvailable alone is the "came back online" signal — no previous-state tracking needed.
// It shares GetIsNetworkAvailable's optimism (a virtual adapter keeps availability true), so it can miss a
// real-uplink recovery; the periodic backstop still covers those.
public sealed class NetworkChangeSyncTrigger(
    LexboxProjectChangeListener lexboxProjectChangeListener,
    UpdateChecker updateChecker,
    ILogger<NetworkChangeSyncTrigger> logger) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Watching network availability to re-establish push listeners");
        NetworkChange.NetworkAvailabilityChanged += OnNetworkAvailabilityChanged;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        NetworkChange.NetworkAvailabilityChanged -= OnNetworkAvailabilityChanged;
        return Task.CompletedTask;
    }

    private void OnNetworkAvailabilityChanged(object? sender, NetworkAvailabilityEventArgs e)
    {
        if (!e.IsAvailable) return;
        logger.LogInformation("Network availability regained; ensuring push listeners");
        _ = EnsureListeners();
        _ = RetryUpdateCheck();
    }

    //the startup check is a no-op when it fails before reaching the server (no throttle record), so
    //this retries it once the network is actually usable. TryUpdate itself honors the interval gate.
    private async Task RetryUpdateCheck()
    {
        try
        {
            await updateChecker.TryUpdate();
        }
        catch (Exception e)
        {
            logger.LogWarning(e, "Failed to check for updates after network availability change");
        }
    }

    private async Task EnsureListeners(CancellationToken cancellationToken = default)
    {
        try
        {
            await lexboxProjectChangeListener.EnsureListenersForTrackedProjects(kickReconnecting: true, cancellationToken: cancellationToken);
        }
        catch (Exception e)
        {
            logger.LogWarning(e, "Failed to ensure push listeners after network availability change");
        }
    }
}
