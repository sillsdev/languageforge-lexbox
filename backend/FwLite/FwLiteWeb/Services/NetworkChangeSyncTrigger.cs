using System.Net.NetworkInformation;
using FwLiteShared.Projects;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FwLiteWeb.Services;

// Cross-platform counterpart to MAUI's ConnectivitySyncTrigger, for hosts without IConnectivity: when the OS
// reports network availability returning, re-ensure push listeners so a session started offline picks up
// without waiting on PushListenerRecoveryService's periodic backstop. NetworkAvailabilityChanged is the
// System.Net.NetworkInformation analog of MAUI's ConnectivityChanged and is edge-triggered (it only fires on
// a change), so e.IsAvailable alone is the "came back online" signal — no previous-state tracking needed.
// It shares GetIsNetworkAvailable's optimism (a virtual adapter keeps availability true), so it can miss a
// real-uplink recovery; the periodic backstop still covers those.
public sealed class NetworkChangeSyncTrigger(
    LexboxProjectService lexboxProjectService,
    ILogger<NetworkChangeSyncTrigger> logger) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
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
    }

    private async Task EnsureListeners()
    {
        try
        {
            await lexboxProjectService.EnsureListenersForTrackedProjects(kickReconnecting: true);
        }
        catch (Exception e)
        {
            logger.LogWarning(e, "Failed to ensure push listeners after network availability change");
        }
    }
}
