using System.Collections.Concurrent;
using FwLiteShared.Auth;
using FwLiteShared.Services;
using FwLiteShared.Sync;
using LcmCrdt;
using LcmCrdt.RemoteSync;
using LexCore.Entities;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MiniLcm.Push;

namespace FwLiteShared.Projects;

public sealed class LexboxProjectChangeListener(
    OAuthClientFactory clientFactory,
    ILogger<LexboxProjectChangeListener> logger,
    ILoggerFactory loggerFactory,
    IHttpMessageHandlerFactory httpMessageHandlerFactory,
    BackgroundSyncService backgroundSyncService,
    IOptions<AuthConfig> options,
    IMemoryCache cache,
    INetworkStatus networkStatus)
{
    // Stable record of projects we've been asked to listen for, by project id. Survives connection
    // lifetimes so we can resubscribe after a hub-connection rebuild.
    private readonly ConcurrentDictionary<Guid, ProjectData> _trackedProjects = new();
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _connectionGates = new();

    private SemaphoreSlim ConnectionGate(LexboxServer server) =>
        _connectionGates.GetOrAdd(server.Authority.Authority, static _ => new SemaphoreSlim(1, 1));

    public async Task HandleAuthChanged(LexboxServer server)
    {
        // Tear down any cached hub connection — it may be holding stale auth, or belong to a
        // previously-signed-in identity. The next ListenForProjectChanges call will rebuild it
        // with the current token (or no-op if the user is now logged out).
        await LexboxHubConnection.EvictAndStopIfCached(server, cache, logger);

        // Try to (re)start listeners for tracked projects on this server. If auth is gone (logout),
        // StartLexboxProjectChangeListener returns null and we no-op. If auth is valid (login),
        // we get a fresh listener without waiting for the user to manually sync or reopen.
        logger.LogInformation("Auth changed for {Server}, re-establishing push listeners for {Count} tracked project(s)",
            server.Authority, _trackedProjects.Values.Count(p => p.ServerId == server.Id));
        await EnsureListenersForTrackedProjects(server);
    }

    // Re-establish push listeners for every tracked project (a project is skipped only when its server has a
    // live connection AND the project is already registered on it, so a periodic call is a true no-op once subscribed;
    // a logged-out server no-ops on the token check). The per-project check matters: single-project paths
    // (post-sync, project-open) subscribe only the requesting project when they rebuild a connection, leaving
    // sibling projects unsubscribed until this pass heals them. Used by connectivity-regained, app-resume
    // and periodic recovery so a listener that failed to start while offline comes back without a manual
    // sync or edit.
    public Task EnsureListenersForTrackedProjects(bool kickReconnecting = false) =>
        EnsureListenersForTrackedProjects(null, kickReconnecting);

    private async Task EnsureListenersForTrackedProjects(LexboxServer? server, bool kickReconnecting = false)
    {
        // Snapshot connected servers BEFORE the loop, not per project: when a server is down, its first project
        // opens the connection and the rest must still subscribe on it within this same pass.
        var connectedServers = ConnectedServerConnections();
        foreach (var project in _trackedProjects.Values)
        {
            if (project.ServerId is null) continue;
            if (server is not null && project.ServerId != server.Id) continue;
            if (connectedServers.TryGetValue(project.ServerId, out var connection)
                && connection.IsRegisteredForResubscribe(project.Id))
            {
                continue;
            }
            try
            {
                await ListenForProjectChanges(project, CancellationToken.None, kickReconnecting);
            }
            catch (Exception e)
            {
                logger.LogWarning(e, "Failed to (re)start project change listener for {ProjectName}", project.Name);
            }
        }
    }

    private Dictionary<string, LexboxHubConnection> ConnectedServerConnections()
    {
        var connected = new Dictionary<string, LexboxHubConnection>();
        foreach (var project in _trackedProjects.Values)
        {
            if (project.ServerId is not string serverId || connected.ContainsKey(serverId)) continue;
            if (options.Value.TryGetServer(project, out var server)
                && LexboxHubConnection.Get(cache, server) is { IsConnected: true } connection)
            {
                connected.Add(serverId, connection);
            }
        }
        return connected;
    }

    public async Task ListenForProjectChanges(ProjectData projectData, CancellationToken stoppingToken, bool kickReconnecting = false)
    {
        // Record intent to listen before any early-returns, so auth-change recovery can resubscribe
        // this project even if the steps below bail out (no server, connection not yet active, etc.).
        _trackedProjects[projectData.Id] = projectData;
        if (!options.Value.TryGetServer(projectData, out var server)) return;
        if (kickReconnecting) await KickIfReconnecting(server);
        var lexboxConnection = await StartLexboxProjectChangeListener(server, stoppingToken);
        if (lexboxConnection is null) return;
        // Register for resubscription before any early-return on connection state, otherwise the
        // Reconnected handler won't know to resubscribe it when the connection comes back.
        lexboxConnection.RegisterForResubscribe(projectData.Id);
        // If the connection isn't active yet, avoid SendAsync which throws in Reconnecting/Disconnected.
        if (lexboxConnection.IsReconnecting)
        {
            // Reconnected handler will resubscribe when the connection comes back.
            return;
        }
        if (lexboxConnection.IsDisconnected)
        {
            // Defensive: a cached connection is normally Connected. If we find a Disconnected one, try to
            // revive it; on failure evict it so the next call rebuilds rather than handing back a dead one.
            var gate = ConnectionGate(server);
            await gate.WaitAsync(stoppingToken);
            try
            {
                // Re-check under the gate: a concurrent caller may have revived it, and a second StartAsync
                // would throw and wrongly evict the freshly revived connection.
                if (lexboxConnection.IsDisconnected)
                    await lexboxConnection.Connection.StartAsync(stoppingToken);
            }
            catch (Exception e)
            {
                logger.LogWarning(e, "Failed to restart Lexbox listener");
                await LexboxHubConnection.EvictAndStopIfCached(server, cache, logger, lexboxConnection);
                return;
            }
            finally
            {
                gate.Release();
            }
            // Covers the current project (registered above) and any siblings on this connection.
            await lexboxConnection.ResubscribeRegisteredProjects();
            // The connection just came back up; flush/pull like the other reconnect paths.
            TriggerSyncForServerProjects(server);
        }
        else
        {
            try
            {
                await lexboxConnection.ListenForProjectChanges(projectData.Id, stoppingToken);
            }
            catch (Exception e)
            {
                // The connection can drop or be disposed between the state check and SendAsync (throwing
                // InvalidOperationException or ObjectDisposedException). Don't let that fail the caller (e.g.
                // project-open); the Reconnected handler resubscribes once the connection recovers.
                logger.LogWarning(e, "SignalR connection not active while subscribing to project changes");
            }
        }
    }

    // Only for callers with strong evidence the network just returned (connectivity regained, app resume, a
    // successful sync to this server): SignalR has no retry-now API, so stop-and-rebuild is the only way to
    // skip a committed backoff delay. Never kick without such evidence — a wrong kick trades the automatic
    // retry loop for the much slower recovery backstops.
    private async Task KickIfReconnecting(LexboxServer server)
    {
        if (LexboxHubConnection.Get(cache, server) is { IsReconnecting: true } connection)
        {
            logger.LogInformation("Network recovery signal: interrupting mid-backoff reconnect to {Server}", server.Authority);
            await LexboxHubConnection.EvictAndStopIfCached(server, cache, logger, connection);
        }
    }

    // A listener (re)connecting moves no data on its own — the server only pushes OnProjectUpdated for changes
    // made after (re)subscribe — so changes authored while offline, and remote changes that arrived during the
    // outage, need an explicit sync once the connection is back. Call ONLY on a real connect transition (fresh
    // build, SignalR Reconnected, or a revive), never on a cache-hit re-ensure, otherwise it loops with the
    // post-sync TryEnsureProjectChangeListener.
    private void TriggerSyncForServerProjects(LexboxServer server)
    {
        // A genuine connect transition means SignalR just reached this server, which proves it's reachable.
        // Both these per-server caches can hold a stale failure from the offline period: the health verdict
        // (cached unhealthy for 30 min) would make this catch-up sync a silent no-op, and the project list
        // (cached empty for 5 min) would keep showing nothing. Drop both so they re-fetch now that we're back.
        CrdtHttpSyncService.InvalidateServerHealth(cache, server.Authority.Authority);
        cache.Remove(LexboxProjectService.ProjectListCacheKey(server));
        foreach (var project in _trackedProjects.Values)
        {
            if (project.ServerId != server.Id) continue;
            try
            {
                backgroundSyncService.TriggerSync(project.Id);
            }
            catch (Exception e)
            {
                // TriggerSync throws if the background sync service isn't running yet (e.g. startup/shutdown).
                logger.LogWarning(e, "Failed to trigger sync after listener (re)connect for {ProjectName}", project.Name);
            }
        }
    }

    public async Task<LexboxHubConnection?> StartLexboxProjectChangeListener(LexboxServer server, CancellationToken stoppingToken)
    {
        // Reuse an existing connection without consulting auth: it may be healthily auto-reconnecting, and a
        // transient token-refresh failure must not be mistaken for a logout and tear it down — the same
        // false-logout trap HandleReconnecting avoids. (Removing the cache entry disposes the connection via
        // the post-eviction callback, killing its retry loop and resubscribe set.)
        if (LexboxHubConnection.Get(cache, server) is { } connection)
        {
            return connection;
        }

        // No connection to reuse: build one only if actually signed in. Logged-out is tested by account
        // presence (IsSignedIn) — a local-only read that flips solely on a real logout — not GetCurrentToken,
        // which can be null transiently (expired token + flaky network) while the user is still signed in.
        // Once the user signs back in, auth-change handling rebuilds the listener.
        if (!await clientFactory.GetClient(server).IsSignedIn())
        {
            logger.LogWarning("Unable to create signalR client, user is not authenticated to {OriginDomain}", server.Authority);
            return null;
        }

        // Serialize creation per server: concurrent callers would otherwise each build a connection, and the
        // cache's last-writer-wins replacement disposes the loser mid-use and discards its resubscribe set.
        var gate = ConnectionGate(server);
        await gate.WaitAsync(stoppingToken);
        try
        {
            if (LexboxHubConnection.Get(cache, server) is { } cachedConnection)
            {
                return cachedConnection;
            }
            return await BuildAndStartConnection(server, stoppingToken);
        }
        finally
        {
            gate.Release();
        }
    }

    private async Task<LexboxHubConnection?> BuildAndStartConnection(LexboxServer server, CancellationToken stoppingToken)
    {
        var hubConnection = new HubConnectionBuilder()
            // Hand the builder's internal DI the app's logger factory so HubConnection's own logs (reconnect
            // attempts, close reasons) land in the app log instead of a console-only factory. An externally
            // supplied instance is not disposed with the connection's service provider.
            .ConfigureLogging(logging => logging.Services.AddSingleton(loggerFactory))
            .WithAutomaticReconnect(new AdaptiveRetryPolicy(networkStatus, logger))
            .WithUrl($"{server.Authority}api/hub/crdt/project-changes",
                connectionOptions =>
                {
                    connectionOptions.HttpMessageHandlerFactory = handler =>
                    {
                        // Use a client that does not validate certs in dev.
                        return httpMessageHandlerFactory.CreateHandler(OAuthClient.AuthHttpClientName);
                    };
                    connectionOptions.AccessTokenProvider =
                        async () => await clientFactory.GetClient(server).GetCurrentToken();
                })
            .Build();

        var connection = new LexboxHubConnection(
            server,
            hubConnection,
            cache,
            backgroundSyncService,
            async () => await clientFactory.GetClient(server).IsSignedIn(),
            TriggerSyncForServerProjects,
            logger);

        hubConnection.On(nameof(IProjectChangeHubClient.OnProjectUpdated),
            (Guid projectId, Guid? clientId) =>
            {
                connection.OnProjectUpdated(projectId, clientId);
                return Task.CompletedTask;
            });

        try
        {
            await connection.StartAsync(stoppingToken);
            LexboxHubConnection.Set(cache, server, connection);
        }
        catch (Exception e)
        {
            // Nothing is cached yet (CreateEntry runs after StartAsync) and the Closed handler that would
            // dispose this connection isn't wired on the failure path — dispose here so a failed StartAsync
            // (e.g. offline at project-open, retried on the next sync) doesn't leak the connection.
            await connection.DisposeAsync();
            logger.LogWarning(e, "Failed to start Lexbox listener");
            return null;
        }

        // Freshly built+connected (e.g. first listen or a kick-rebuild after the network returned). Flush
        // offline-authored changes and pull anything missed; the connection is now cached, so the sync's
        // own re-ensure hits the cache and won't rebuild (no loop).
        TriggerSyncForServerProjects(server);
        return connection;
    }

    // Must never return null — that ends SignalR's retry loop for good (the deliberate stop on logout is
    // HandleReconnecting's job). SignalR consults the policy once per attempt and a returned delay cannot be
    // interrupted except by StopAsync, so the delay is the worst-case recovery latency: keep it short while
    // the device reports online (the problem is server-side and could end any moment) and long while offline
    // (every attempt is a pointless local failure; connectivity/resume triggers handle the transition back).
    internal class AdaptiveRetryPolicy(INetworkStatus networkStatus, ILogger? logger = null) : IRetryPolicy
    {
        public TimeSpan? NextRetryDelay(RetryContext retryContext)
        {
            var isOnline = networkStatus.IsOnline;
            var delay = retryContext.PreviousRetryCount switch
            {
                0 => TimeSpan.Zero,
                1 => TimeSpan.FromSeconds(5),
                _ when !isOnline => TimeSpan.FromSeconds(60),
                _ => TimeSpan.FromSeconds(10),
            };
            // Per-attempt detail for diagnosing reconnect behaviour in the field; the connection-level
            // reconnecting/recovered/synced events carry the story at Information.
            logger?.LogDebug("SignalR reconnect attempt #{Attempt}: device reports online={IsOnline}, next retry in {Delay}",
                retryContext.PreviousRetryCount, isOnline, delay);
            return delay;
        }
    }
}
