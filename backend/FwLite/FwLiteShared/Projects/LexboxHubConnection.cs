using FwLiteShared.Auth;
using FwLiteShared.Sync;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using MiniLcm.Push;

namespace FwLiteShared.Projects;

public sealed class LexboxHubConnection(
    LexboxServer server,
    HubConnection connection,
    IMemoryCache cache,
    BackgroundSyncService backgroundSyncService,
    Func<Task<bool>> isSignedIn,
    Action<LexboxServer> onConnected,
    ILogger logger) : IAsyncDisposable
{
    private readonly HashSet<Guid> registeredProjects = [];

    public LexboxServer Server { get; } = server;
    public HubConnection Connection { get; } = connection;
    public HubConnectionState State => Connection.State;
    public bool IsConnected => State == HubConnectionState.Connected;
    public bool IsReconnecting => State == HubConnectionState.Reconnecting;
    public bool IsDisconnected => State == HubConnectionState.Disconnected;

    public static LexboxHubConnection? Get(IMemoryCache cache, LexboxServer server)
    {
        return cache.Get<LexboxHubConnection>(CacheKey(server));
    }

    public static void Set(IMemoryCache cache, LexboxServer server, LexboxHubConnection connection)
    {
        // ICacheEntry value returned from CreateEntry must be disposed in order to be committed to the cache,
        // so do not remove the "using var __" below that appears to be doing nothing.
        using var __ = cache.CreateEntry(CacheKey(server)).SetValue(connection).RegisterPostEvictionCallback(
            static (key, value, reason, state) =>
            {
                if (value is LexboxHubConnection con)
                {
                    _ = con.DisposeAsync();
                }
            });
    }

    public async Task StartAsync(CancellationToken stoppingToken)
    {
        Connection.Reconnecting += OnReconnecting;
        await Connection.StartAsync(stoppingToken);
        Connection.Closed += OnClosed;
        Connection.Reconnected += OnReconnected;
    }

    public void OnProjectUpdated(Guid projectId, Guid? clientId)
    {
        logger.LogInformation("Received project update for {ProjectId}, triggering sync", projectId);
        try
        {
            backgroundSyncService.TriggerSync(projectId, clientId);
        }
        catch (Exception e)
        {
            // TriggerSync throws if the background sync service isn't running (e.g. during shutdown);
            // don't let that bubble into SignalR's dispatcher.
            logger.LogWarning(e, "Failed to trigger sync for {ProjectId}", projectId);
        }
    }

    public async Task ListenForProjectChanges(Guid projectId, CancellationToken stoppingToken = default)
    {
        RegisterForResubscribe(projectId);
        await Connection.SendAsync(nameof(IProjectChangeHubServer.ListenForProjectChanges), projectId, stoppingToken);
    }

    public void RegisterForResubscribe(Guid projectId)
    {
        lock (registeredProjects)
        {
            registeredProjects.Add(projectId);
        }
    }

    public bool IsRegisteredForResubscribe(Guid projectId)
    {
        lock (registeredProjects)
        {
            return registeredProjects.Contains(projectId);
        }
    }

    // Used by the Reconnected handler and after a manual revive — a manual
    // StartAsync does not raise Reconnected, so the revive path must resubscribe explicitly.
    public async Task ResubscribeRegisteredProjects()
    {
        Guid[] projectIds;
        lock (registeredProjects)
        {
            projectIds = [.. registeredProjects];
        }
        foreach (var projectId in projectIds)
        {
            try
            {
                await Connection.SendAsync(nameof(IProjectChangeHubServer.ListenForProjectChanges), projectId);
            }
            catch (Exception)
            {
                // Ensure one project's failing to resubscribe doesn't block others.
            }
        }
    }

    // When expectedConnection is given, evicts only if the cached instance is that connection — callers
    // holding a specific dead connection must not evict a live replacement that was cached after they last looked.
    internal static async Task EvictAndStopIfCached(
        LexboxServer server,
        IMemoryCache cache,
        ILogger logger,
        LexboxHubConnection? expectedConnection = null)
    {
        var connection = Get(cache, server);
        if (connection is null) return;
        if (expectedConnection is not null && !ReferenceEquals(connection, expectedConnection)) return;
        Remove(cache, server);
        await StopConnection(connection, logger);
    }

    // The retry policy never gives up; this breaks the loop when the user is logged out. Logged-out is tested
    // by account presence (IsSignedIn), not by whether a token can be fetched right now: reconnecting happens
    // precisely when the network is flaky, and fetching a token can require a refresh round-trip that fails
    // transiently — which would be a false logout. Account presence is a local-only read and only flips on a
    // real logout. Once the user signs back in, auth-change handling rebuilds the listener.
    internal static async Task HandleReconnecting(
        LexboxServer server,
        IMemoryCache cache,
        ILogger logger,
        LexboxHubConnection connection,
        Func<Task> stopConnection,
        bool isSignedIn,
        Exception? exception)
    {
        if (exception is not null)
            logger.LogWarning(exception, "SignalR connection reconnecting");
        else
            logger.LogInformation("SignalR connection reconnecting");

        if (isSignedIn) return;

        // Same guard as the Closed handler: a replacement connection may have been cached since this one
        // started reconnecting; only evict the entry if it is still ours. Stopping ourselves is right
        // regardless — this connection belongs to a signed-out user.
        if (Get(cache, server) is {} cached && ReferenceEquals(cached, connection))
            Remove(cache, server);
        logger.LogWarning("SignalR reconnect aborted: user is logged out; stopping connection");
        try
        {
            await stopConnection();
        }
        catch (Exception e)
        {
            logger.LogWarning(e, "Failed to stop SignalR connection after auth loss");
        }
    }

    public async Task StopAsync()
    {
        await Connection.StopAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await Connection.DisposeAsync();
    }

    private async Task OnReconnecting(Exception? exception)
    {
        await HandleReconnecting(Server, cache, logger, this, StopAsync, await isSignedIn(), exception);
    }

    private async Task OnClosed(Exception? exception)
    {
        // A stopped connection may have been replaced in the cache before this handler runs. Don't
        // blindly remove the cache entry on Closed — only do so if WE are the cached connection,
        // otherwise we'd evict the live replacement.
        if (Get(cache, Server) is {} cached && ReferenceEquals(cached, this))
            Remove(cache, Server);
        await DisposeAsync();
    }

    private Task OnReconnected(string? connectionId)
    {
        // Auto-reconnected this same connection (no rebuild → the fresh-build sync below didn't run).
        logger.LogInformation("SignalR reconnected to {Server}; syncing", Server.Authority);
        onConnected(Server);
        return Task.CompletedTask;
    }

    private static async Task StopConnection(LexboxHubConnection connection, ILogger logger)
    {
        try
        {
            await connection.StopAsync();
        }
        catch (ObjectDisposedException e)
        {
            // A HubConnection mid-reconnect has already disposed itself, so stopping one we're evicting
            // from a kick throws ObjectDisposedException even though the eviction itself succeeded.
            logger.LogDebug(e, "Evicted SignalR connection was already disposed");
        }
        catch (Exception e)
        {
            logger.LogWarning(e, "Failed to stop evicted SignalR connection");
        }
    }

    private static string CacheKey(LexboxServer server) => $"{nameof(LexboxHubConnection)}|{server.Authority.Authority}";

    private static void Remove(IMemoryCache cache, LexboxServer server)
    {
        cache.Remove(CacheKey(server));
    }
}
