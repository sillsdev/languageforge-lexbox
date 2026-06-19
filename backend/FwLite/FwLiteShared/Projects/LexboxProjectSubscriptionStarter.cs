using FwLiteShared.Auth;
using Microsoft.Extensions.Logging;

namespace FwLiteShared.Projects;

public sealed class LexboxProjectSubscriptionStarter(
    LexboxHubConnectionRegistry connectionRegistry,
    ILogger<LexboxProjectSubscriptionStarter> logger)
{
    public async Task SubscribeWhenReady(
        LexboxServer server,
        LexboxHubConnection connection,
        Guid projectId,
        SemaphoreSlim connectionGate,
        CancellationToken stoppingToken,
        Action<LexboxServer> onConnected)
    {
        // Register before any early-return on connection state, otherwise the Reconnected handler
        // won't know to resubscribe this project when the connection comes back.
        connection.RegisterForResubscribe(projectId);
        if (connection.IsReconnecting)
        {
            // Reconnected handler will resubscribe when the connection comes back.
            return;
        }
        if (connection.IsDisconnected)
        {
            await ReviveDisconnectedConnection(server, connection, connectionGate, stoppingToken, onConnected);
            return;
        }

        try
        {
            await connection.ListenForProjectChanges(projectId, stoppingToken);
        }
        catch (Exception e)
        {
            // The connection can drop or be disposed between the state check and SendAsync (throwing
            // InvalidOperationException or ObjectDisposedException). Don't let that fail the caller (e.g.
            // project-open); the Reconnected handler resubscribes once the connection recovers.
            logger.LogWarning(e, "SignalR connection not active while subscribing to project changes");
        }
    }

    private async Task ReviveDisconnectedConnection(
        LexboxServer server,
        LexboxHubConnection connection,
        SemaphoreSlim connectionGate,
        CancellationToken stoppingToken,
        Action<LexboxServer> onConnected)
    {
        // Defensive: a cached connection is normally Connected. If we find a Disconnected one, try to
        // revive it; on failure evict it so the next call rebuilds rather than handing back a dead one.
        await connectionGate.WaitAsync(stoppingToken);
        try
        {
            // Re-check under the gate: a concurrent caller may have revived it, and a second StartAsync
            // would throw and wrongly evict the freshly revived connection.
            if (connection.IsDisconnected)
                await connection.Connection.StartAsync(stoppingToken);
        }
        catch (Exception e)
        {
            logger.LogWarning(e, "Failed to restart Lexbox listener");
            await connectionRegistry.EvictAndStopIfCached(server, connection);
            return;
        }
        finally
        {
            connectionGate.Release();
        }
        // Covers the current project (registered above) and any siblings on this connection.
        await connection.ResubscribeRegisteredProjects();
        // The connection just came back up; flush/pull like the other reconnect paths.
        onConnected(server);
    }
}
