using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using FwLiteShared.Auth;
using FwLiteShared.Events;
using FwLiteShared.Sync;
using LcmCrdt;
using LexCore.Entities;
using LexCore.Sync;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MiniLcm.Push;
using SIL.Harmony.Core;

namespace FwLiteShared.Projects;

public class LexboxProjectService : IDisposable
{
    private readonly OAuthClientFactory clientFactory;
    private readonly ILogger<LexboxProjectService> logger;
    private readonly IHttpMessageHandlerFactory httpMessageHandlerFactory;
    private readonly BackgroundSyncService backgroundSyncService;
    private readonly IOptions<AuthConfig> options;
    private readonly IMemoryCache cache;
    private readonly GlobalEventBus globalEventBus;
    private readonly IDisposable onAuthChangedSubscription;
    // Stable record of projects we've been asked to listen for, by project id. Survives connection
    // lifetimes (unlike _reconnectProjects) so we can resubscribe after a hub-connection rebuild.
    private readonly ConcurrentDictionary<Guid, ProjectData> _trackedProjects = new();

    public LexboxProjectService(
        OAuthClientFactory clientFactory,
        ILogger<LexboxProjectService> logger,
        IHttpMessageHandlerFactory httpMessageHandlerFactory,
        BackgroundSyncService backgroundSyncService,
        IOptions<AuthConfig> options,
        IMemoryCache cache,
        GlobalEventBus globalEventBus)
    {
        this.clientFactory = clientFactory;
        this.logger = logger;
        this.httpMessageHandlerFactory = httpMessageHandlerFactory;
        this.backgroundSyncService = backgroundSyncService;
        this.options = options;
        this.cache = cache;
        this.globalEventBus = globalEventBus;
        onAuthChangedSubscription = globalEventBus.OnAuthenticationChanged.Subscribe(@event =>
        {
            InvalidateProjectsCache(@event.Server);
            _ = OnAuthenticationChangedAsync(@event.Server);
        });
    }

    private async Task OnAuthenticationChangedAsync(LexboxServer server)
    {
        try
        {
            // Tear down any cached hub connection — it may be holding stale auth, or belong to a
            // previously-signed-in identity. The next ListenForProjectChanges call will rebuild it
            // with the current token (or no-op if the user is now logged out).
            await EvictAndStopIfCached(HubConnectionCacheKey(server), cache, logger);

            // Try to (re)start listeners for tracked projects on this server. If auth is gone (logout),
            // StartLexboxProjectChangeListener returns null and we no-op. If auth is valid (login),
            // we get a fresh listener without waiting for the user to manually sync or reopen.
            bool ForThisServer(ProjectData p) => p.ServerId == server.Id;
            logger.LogInformation("Auth changed for {Server}, re-establishing push listeners for {Count} tracked project(s)",
                server.Authority, _trackedProjects.Values.Count(ForThisServer));
            await EnsureListenersForTrackedProjects(ForThisServer);
        }
        catch (Exception e)
        {
            logger.LogWarning(e, "Failed to handle authentication change for {Server}", server.Authority);
        }
    }

    // Re-establish push listeners for every tracked project (a project is skipped only when its server has a
    // live connection AND the project is registered on it, so a periodic call is a true no-op once subscribed;
    // a logged-out server no-ops on the token check). The per-project check matters: single-project paths
    // (post-sync, project-open) subscribe only the requesting project when they rebuild a connection, leaving
    // sibling projects unsubscribed until this pass heals them. Used by connectivity-regained and periodic
    // recovery so a listener that failed to start while offline comes back without a manual sync or edit.
    public Task EnsureListenersForTrackedProjects() => EnsureListenersForTrackedProjects(null);

    private async Task EnsureListenersForTrackedProjects(Func<ProjectData, bool>? filter)
    {
        // Snapshot connected servers BEFORE the loop, not per project: when a server is down, its first project
        // opens the connection and the rest must still subscribe on it within this same pass.
        var connectedServers = ConnectedServerConnections();
        foreach (var project in _trackedProjects.Values)
        {
            if (filter is not null && !filter(project)) continue;
            if (project.ServerId is { } serverId
                && connectedServers.TryGetValue(serverId, out var connection)
                && IsRegisteredForResubscribe(connection, project.Id))
            {
                continue;
            }
            try
            {
                await ListenForProjectChanges(project, CancellationToken.None);
            }
            catch (Exception e)
            {
                logger.LogWarning(e, "Failed to (re)start project change listener for {ProjectName}", project.Name);
            }
        }
    }

    private Dictionary<string, HubConnection> ConnectedServerConnections()
    {
        var connected = new Dictionary<string, HubConnection>();
        foreach (var project in _trackedProjects.Values)
        {
            if (project.ServerId is not string serverId || connected.ContainsKey(serverId)) continue;
            if (options.Value.TryGetServer(project, out var server)
                && cache.TryGetValue(HubConnectionCacheKey(server), out HubConnection? connection)
                && connection is { State: HubConnectionState.Connected })
            {
                connected.Add(serverId, connection);
            }
        }
        return connected;
    }

    public void Dispose()
    {
        onAuthChangedSubscription.Dispose();
    }

    public LexboxServer[] Servers()
    {
        return options.Value.LexboxServers;
    }

    public LexboxServer? GetServer(ProjectData? projectData)
    {
        if (projectData is null) return null;
        return Servers().FirstOrDefault(s => s.Id == projectData.ServerId);
    }

    public async Task<ListProjectsResult> GetLexboxProjects(LexboxServer server)
    {
        return await cache.GetOrCreateAsync(CacheKey(server),
            async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
                var httpClient = await clientFactory.GetClient(server).CreateHttpClient();
                if (httpClient is null) return new([], false);
                try
                {
                    return await httpClient.GetFromJsonAsync<ListProjectsResult>("api/crdt/listProjectsV2") ?? new([], false);
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Error getting lexbox projects");
                    return new([], false);
                }
            }) ?? new([], false);
    }

    public async Task<LexboxUser?> GetLexboxUser(LexboxServer server)
    {
        return await clientFactory.GetClient(server).GetCurrentUser();
    }

    private static string CacheKey(LexboxServer server)
    {
        return $"Projects|{server.Authority.Authority}";
    }

    public async Task<(DownloadProjectByCodeResult, Guid?)> GetLexboxProjectId(LexboxServer server, string code)
    {
        var httpClient = await clientFactory.GetClient(server).CreateHttpClient();
        if (httpClient is null) return (DownloadProjectByCodeResult.Forbidden, null);
        try
        {
            var result = await httpClient.GetAsync($"api/crdt/lookupProjectId?code={code}");
            if (result.StatusCode == System.Net.HttpStatusCode.Forbidden) // 403
            {
                return (DownloadProjectByCodeResult.Forbidden, null);
            }
            if (result.StatusCode == System.Net.HttpStatusCode.NotFound) // 404
            {
                return (DownloadProjectByCodeResult.ProjectNotFound, null);
            }
            if (result.StatusCode == System.Net.HttpStatusCode.NotAcceptable) // 406
            {
                return (DownloadProjectByCodeResult.NotCrdtProject, null);
            }
            var guid = await result.Content.ReadFromJsonAsync<Guid?>();
            return (DownloadProjectByCodeResult.Success, guid);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error getting lexbox project id");
            return (DownloadProjectByCodeResult.Forbidden, null);
        }
    }

    public async Task<ProjectSyncStatus> GetLexboxSyncStatus(LexboxServer server, Guid projectId)
    {
        var client = clientFactory.GetClient(server);
        var httpClient = await client.CreateHttpClient();
        if (httpClient is null)
            return ProjectSyncStatus.Unknown(await client.IsSignedIn()
                ? ProjectSyncStatusErrorCode.Offline
                : ProjectSyncStatusErrorCode.NotLoggedIn);
        try
        {
            var status = await httpClient.GetFromJsonAsync<ProjectSyncStatus>($"api/fw-lite/sync/status/{projectId}");
            return status ?? ProjectSyncStatus.Unknown(ProjectSyncStatusErrorCode.Unknown, "No status returned from server");
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error getting lexbox sync status");
            return ProjectSyncStatus.Unknown(ProjectSyncStatusErrorCode.Unknown, e.Message);
        }
    }

    public async Task<HttpResponseMessage?> TriggerLexboxSync(LexboxServer server, Guid projectId)
    {
        var httpClient = await clientFactory.GetClient(server).CreateHttpClient();
        if (httpClient is null) return null;
        try
        {
            return await httpClient.PostAsync($"api/fw-lite/sync/trigger/{projectId}", null);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error triggering lexbox sync");
            return null;
        }
    }

    public async Task<SyncJobResult> AwaitLexboxSyncFinished(LexboxServer server, Guid projectId, int timeoutSeconds = 15 * 60)
    {
        var client = clientFactory.GetClient(server);
        var httpClient = await client.CreateHttpClient();
        if (httpClient is null)
        {
            return await client.IsSignedIn()
                ? new SyncJobResult(SyncJobStatusEnum.UnableToSync, "Unable to reach the lexbox server, check your internet connection and try again")
                : new SyncJobResult(SyncJobStatusEnum.UnableToAuthenticate, "Unable to retrieve sync status when logged out, try again after logging in to lexbox server");
        }
        var giveUpAt = DateTime.UtcNow + TimeSpan.FromSeconds(timeoutSeconds);
        while (giveUpAt > DateTime.UtcNow)
        {
            try
            {
                // Avoid 30-second timeout by retrying every 25 seconds until max time reached
                var result = await httpClient.GetAsync(
                        $"api/fw-lite/sync/await-sync-finished/{projectId}",
                        new CancellationTokenSource(TimeSpan.FromSeconds(25)).Token);
                if (result.IsSuccessStatusCode)
                {
                    var content = await result.Content.ReadFromJsonAsync<SyncJobResult?>();
                    if (content is { Status: SyncJobStatusEnum.TimedOutAwaitingSyncStatus }) continue;
                    return content ?? new SyncJobResult(SyncJobStatusEnum.UnknownError, "Unknown error retrieving sync status");
                }
                else
                {
                    var errorMessage = await result.Content.ReadAsStringAsync();
                    return new SyncJobResult(SyncJobStatusEnum.UnknownError, errorMessage);
                }
            }
            catch (OperationCanceledException) { continue; }
            catch (Exception e)
            {
                logger.LogError(e, "Error waiting for lexbox sync to finish");
                return new SyncJobResult(SyncJobStatusEnum.UnknownError, e.ToString());
            }
        }
        logger.LogError("Timed out waiting for lexbox sync to finish");
        return new SyncJobResult(SyncJobStatusEnum.SyncJobTimedOut, "Timed out waiting for lexbox sync to finish");
    }

    public async Task<int?> CountPendingCrdtCommits(LexboxServer server, Guid projectId, SyncState localSyncState)
    {
        var httpClient = await clientFactory.GetClient(server).CreateHttpClient();
        if (httpClient is null) return null;
        try
        {
            var result = await httpClient.PostAsJsonAsync<SyncState>($"/api/crdt/{projectId}/countChanges", localSyncState);
            var text = await result.Content.ReadAsStringAsync();
            return int.Parse(text);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error counting pending changes in lexbox");
            return null;
        }
    }

    public void InvalidateProjectsCache(LexboxServer server)
    {
        cache.Remove(CacheKey(server));
    }

    private static readonly ConditionalWeakTable<HubConnection, HashSet<Guid>> _reconnectProjects = new();

    public async Task ListenForProjectChanges(ProjectData projectData, CancellationToken stoppingToken)
    {
        // Record intent to listen before any early-returns, so auth-change recovery can resubscribe
        // this project even if the steps below bail out (no server, connection not yet active, etc.).
        _trackedProjects[projectData.Id] = projectData;
        if (!options.Value.TryGetServer(projectData, out var server)) return;
        var lexboxConnection = await StartLexboxProjectChangeListener(server, stoppingToken);
        if (lexboxConnection is null) return;
        // Register for resubscription before any early-return on connection state, otherwise the
        // Reconnected handler won't know to resubscribe it when the connection comes back.
        RegisterForResubscribe(lexboxConnection, projectData.Id);
        // If the connection isn't active yet, avoid SendAsync which throws in Reconnecting/Disconnected.
        if (lexboxConnection.State == HubConnectionState.Reconnecting)
        {
            // Reconnected handler will resubscribe when the connection comes back.
            return;
        }
        if (lexboxConnection.State == HubConnectionState.Disconnected)
        {
            // Defensive: a cached connection is normally Connected. If we find a Disconnected one, try to
            // revive it; on failure evict it so the next call rebuilds rather than handing back a dead one.
            try
            {
                await lexboxConnection.StartAsync(stoppingToken);
            }
            catch (Exception e)
            {
                logger.LogWarning(e, "Failed to restart Lexbox listener");
                await EvictAndStopIfCached(HubConnectionCacheKey(server), cache, logger);
                return;
            }
            // Covers the current project (registered above) and any siblings on this connection.
            await ResubscribeRegisteredProjects(lexboxConnection);
            return;
        }
        try
        {
            await lexboxConnection.SendAsync(nameof(IProjectChangeHubServer.ListenForProjectChanges), projectData.Id, stoppingToken);
        }
        catch (Exception e)
        {
            // The connection can drop or be disposed between the state check and SendAsync (throwing
            // InvalidOperationException or ObjectDisposedException). Don't let that fail the caller (e.g.
            // project-open); the Reconnected handler resubscribes once the connection recovers.
            logger.LogWarning(e, "SignalR connection not active while subscribing to project changes");
        }
    }

    private static string HubConnectionCacheKey(LexboxServer server) => $"LexboxProjectChangeListener|{server.Authority.Authority}";

    // Internal for unit tests. Records the project in the per-connection resubscribe set (creating the set
    // and wiring the Reconnected handler on first use). Registration is unconditional — it does not depend
    // on connection state — so callers can register before early-returning when the connection isn't active,
    // and the Reconnected handler will still resubscribe the project once the connection recovers.
    internal static HashSet<Guid> RegisterForResubscribe(HubConnection connection, Guid projectId)
    {
        var subscribedProjects = _reconnectProjects.GetOrAdd(connection, static conn =>
        {
            conn.Reconnected += async _ => await ResubscribeRegisteredProjects(conn);
            return new HashSet<Guid>();
        });
        lock (subscribedProjects)
        {
            subscribedProjects.Add(projectId);
        }
        return subscribedProjects;
    }

    // Internal for unit tests. Used by the Reconnected handler and after a manual revive — a manual
    // StartAsync does not raise Reconnected, so the revive path must resubscribe explicitly.
    internal static async Task ResubscribeRegisteredProjects(HubConnection connection)
    {
        if (!_reconnectProjects.TryGetValue(connection, out var projects)) return;
        Guid[] projectIds;
        lock (projects)
        {
            projectIds = [.. projects];
        }
        foreach (var projectId in projectIds)
        {
            try
            {
                await connection.SendAsync(nameof(IProjectChangeHubServer.ListenForProjectChanges), projectId);
            }
            catch (Exception)
            {
                // Ensure one project's failing to resubscribe doesn't block others
            }
        }
    }

    // Internal for unit tests.
    internal static bool IsRegisteredForResubscribe(HubConnection connection, Guid projectId)
    {
        if (!_reconnectProjects.TryGetValue(connection, out var projects)) return false;
        lock (projects)
        {
            return projects.Contains(projectId);
        }
    }

    // Internal for unit tests.
    internal static async Task EvictAndStopIfCached(string cacheKey, IMemoryCache cache, ILogger logger)
    {
        if (!cache.TryGetValue(cacheKey, out HubConnection? connection) || connection is null) return;
        cache.Remove(cacheKey);
        try
        {
            await connection.StopAsync();
        }
        catch (Exception e)
        {
            logger.LogWarning(e, "Failed to stop evicted SignalR connection");
        }
    }

    // Internal for unit tests. SignalR's InfiniteRetryPolicy retries forever; this breaks the loop when the
    // user is logged out. We treat "no token" as "logged out" — distinguishing a transient token failure
    // from a real logout is OAuthClient's job (see its failure classifier), not ours. Once the user signs
    // back in, OnAuthenticationChangedAsync rebuilds the listener.
    internal static async Task HandleReconnecting(
        string cacheKey,
        IMemoryCache cache,
        ILogger logger,
        Func<Task> stopConnection,
        bool hasValidToken,
        Exception? exception)
    {
        if (exception is not null)
            logger.LogWarning(exception, "SignalR connection reconnecting");
        else
            logger.LogInformation("SignalR connection reconnecting");

        if (hasValidToken) return;

        cache.Remove(cacheKey);
        logger.LogWarning("SignalR reconnect aborted: no auth token (logged out); stopping connection");
        try
        {
            await stopConnection();
        }
        catch (Exception e)
        {
            logger.LogWarning(e, "Failed to stop SignalR connection after auth loss");
        }
    }

    private class InfiniteRetryPolicy : IRetryPolicy
    {
        public TimeSpan? NextRetryDelay(RetryContext retryContext)
        {
            return retryContext.PreviousRetryCount switch
            {
                0 => TimeSpan.Zero,
                1 => TimeSpan.FromSeconds(2),
                2 => TimeSpan.FromSeconds(10),
                3 => TimeSpan.FromSeconds(30),
                _ => TimeSpan.FromSeconds(60),
            };
        }
    }

    public async Task<HubConnection?> StartLexboxProjectChangeListener(LexboxServer server,
        CancellationToken stoppingToken)
    {
        // A null token means logged out for this server (OAuthClient's failure classifier owns the
        // transient-vs-logout decision — not us). Drop any stale cached connection and stand down;
        // sign-in fires AuthenticationChangedEvent and OnAuthenticationChangedAsync rebuilds the listener.
        if (await clientFactory.GetClient(server).GetCurrentToken() is null)
        {
            cache.Remove(HubConnectionCacheKey(server));
            logger.LogWarning("Unable to create signalR client, user is not authenticated to {OriginDomain}", server.Authority);
            return null;
        }
        if (cache.TryGetValue(HubConnectionCacheKey(server), out HubConnection? connection) && connection is not null)
        {
            return connection;
        }

        connection = new HubConnectionBuilder()
            //todo bridge logging to the aspnet logger
            .ConfigureLogging(logging => logging.AddConsole())
            .WithAutomaticReconnect(new InfiniteRetryPolicy())
            .WithUrl($"{server.Authority}api/hub/crdt/project-changes",
                connectionOptions =>
                {
                    connectionOptions.HttpMessageHandlerFactory = handler =>
                    {
                        //use a client that does not validate certs in dev
                        return httpMessageHandlerFactory.CreateHandler(OAuthClient.AuthHttpClientName);
                    };
                    connectionOptions.AccessTokenProvider =
                        async () => await clientFactory.GetClient(server).GetCurrentToken();
                })
            .Build();

        //it would be cleaner to pass the callback in to this method however it's not supposed to be generic, it should always trigger a sync
        connection.On(nameof(IProjectChangeHubClient.OnProjectUpdated),
            (Guid projectId, Guid? clientId) =>
            {
                logger.LogInformation("Received project update for {ProjectId}, triggering sync", projectId);
                backgroundSyncService.TriggerSync(projectId, clientId);
                return Task.CompletedTask;
            });

        try
        {
            var cacheKey = HubConnectionCacheKey(server);
            connection.Reconnecting += async exception =>
            {
                var hasValidToken = await clientFactory.GetClient(server).GetCurrentToken() is not null;
                await HandleReconnecting(cacheKey, cache, logger, () => connection.StopAsync(), hasValidToken, exception);
            };
            // TODO: If StartAsync fails due to transient network, consider retrying on next sync/on-demand
            await connection.StartAsync(stoppingToken);
            // intentionally AFTER StartAsync (see catch comment)
            connection.Closed += async (exception) =>
            {
                // Concurrent ListenForProjectChanges callers can build parallel HubConnections and only
                // one wins the cache slot. Don't blindly remove the cache entry on Closed — only do so
                // if WE are the cached connection, otherwise we'd evict the live replacement.
                if (cache.TryGetValue(HubConnectionCacheKey(server), out HubConnection? cached) && ReferenceEquals(cached, connection))
                    cache.Remove(HubConnectionCacheKey(server));
                await connection.DisposeAsync();
            };
            // ICacheEntry value returned from CreateEntry must be disposed in order to be committed to the cache,
            // so do not remove the "using var __" below that appears to be doing nothing.
            using var __ = cache.CreateEntry(HubConnectionCacheKey(server)).SetValue(connection).RegisterPostEvictionCallback(
                static (key, value, reason, state) =>
                {
                    if (value is HubConnection con)
                    {
                        _ = con.DisposeAsync();
                    }
                });
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

        return connection;
    }
}
