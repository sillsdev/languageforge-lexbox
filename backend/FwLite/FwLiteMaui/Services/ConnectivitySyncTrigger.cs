using FwLiteShared.Projects;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FwLiteMaui.Services;

// Primary use case: app started offline should start syncing if the device comes online
public sealed class ConnectivitySyncTrigger(
    IConnectivity connectivity,
    LexboxProjectService lexboxProjectService,
    ILogger<ConnectivitySyncTrigger> logger) : IHostedService
{
    private NetworkAccess _lastAccess;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _lastAccess = connectivity.NetworkAccess;
        connectivity.ConnectivityChanged += OnConnectivityChanged;
        logger.LogInformation("Watching device connectivity to re-establish push listeners (current access: {NetworkAccess})", _lastAccess);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        connectivity.ConnectivityChanged -= OnConnectivityChanged;
        return Task.CompletedTask;
    }

    private void OnConnectivityChanged(object? sender, ConnectivityChangedEventArgs e)
    {
        var previous = _lastAccess;
        var current = _lastAccess = e.NetworkAccess;

        logger.LogInformation("Device connectivity changed: {Previous} -> {Current} (profiles: {Profiles})",
            previous, current, string.Join(", ", e.ConnectionProfiles));

        if (!ShouldRecover(previous, current)) return;

        logger.LogInformation("Connectivity regained (internet access); ensuring push listeners");
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
            logger.LogWarning(e, "Failed to ensure push listeners after connectivity change");
        }
    }

    // Only react to a transition INTO internet access, so recovery doesn't re-run on every connectivity
    // change (e.g. wifi<->cellular) while already online. Idempotent recovery makes a missed edge harmless;
    // this just keeps the common flapping case quiet.
    public static bool ShouldRecover(NetworkAccess previous, NetworkAccess current)
    {
        return current == NetworkAccess.Internet && previous != NetworkAccess.Internet;
    }
}
