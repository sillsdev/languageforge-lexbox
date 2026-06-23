using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FwLiteShared.Projects;

// Recovering a push listener that failed its initial StartAsync (e.g. offline at project-open) is otherwise
// event-driven: a successful sync, a connectivity change (MAUI's ConnectivitySyncTrigger or FwLiteWeb's
// NetworkChangeSyncTrigger), or an auth change re-runs it. Those can all miss — the OS network signal is
// optimistic (a virtual adapter reads "available", so a real-uplink return may not fire) and an idle user may
// never sync — leaving the listener down indefinitely. This periodic check is the cross-platform backstop.
// EnsureListenersForTrackedProjects is idempotent: a healthy cached connection short-circuits and a
// logged-out server no-ops on the token check.
public sealed class PushListenerRecoveryService(
    LexboxProjectChangeListener lexboxProjectService,
    ILogger<PushListenerRecoveryService> logger) : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // PeriodicTimer's first tick is one interval out, so this never races project-open's own start.
        using var timer = new PeriodicTimer(CheckInterval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                logger.LogDebug("Running periodic push listener recovery check");
                await lexboxProjectService.EnsureListenersForTrackedProjects(cancellationToken: stoppingToken);
            }
            catch (Exception e)
            {
                logger.LogWarning(e, "Periodic push listener recovery failed");
            }
        }
    }
}
