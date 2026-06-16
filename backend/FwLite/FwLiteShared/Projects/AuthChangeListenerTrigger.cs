using FwLiteShared.Events;
using Microsoft.Extensions.Hosting;

namespace FwLiteShared.Projects;

// Re-establishes push listeners when authentication changes (sign-in restores them; sign-out evicts stale
// connections). Auth is a host-agnostic GlobalEventBus event, so — unlike the host-specific connectivity
// triggers — this registers once in the shared kernel and runs everywhere. HandleAuthChanged owns the
// reaction and its own error handling; this owns only the subscription lifecycle.
public sealed class AuthChangeListenerTrigger(
    GlobalEventBus globalEventBus,
    LexboxProjectService lexboxProjectService) : IHostedService
{
    private IDisposable? _subscription;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _subscription = globalEventBus.OnAuthenticationChanged
            .Subscribe(@event => _ = lexboxProjectService.HandleAuthChanged(@event.Server));
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _subscription?.Dispose();
        _subscription = null;
        return Task.CompletedTask;
    }
}
