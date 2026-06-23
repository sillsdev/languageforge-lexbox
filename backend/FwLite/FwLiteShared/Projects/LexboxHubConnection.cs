using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using FwLiteShared.Auth;
using FwLiteShared.Services;
using FwLiteShared.Sync;
using LcmCrdt.RemoteSync;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MiniLcm.Push;
using Nito.AsyncEx;

namespace FwLiteShared.Projects;

public sealed class LexboxHubConnection(
    LexboxServer server,
    ILoggerFactory loggerFactory,
    IHttpMessageHandlerFactory httpMessageHandlerFactory,
    OAuthClientFactory clientFactory,
    IBackgroundSyncService backgroundSyncService,
    INetworkStatus networkStatus,
    IMemoryCache cache,
    ILogger logger,
    ILexboxSignalRConnection? initialConnection = null,
    ILexboxSignalRConnectionFactory? connectionFactory = null,
    ILexboxHubConnectionAuth? auth = null) : IAsyncDisposable
{
    private readonly ConcurrentHashSet<Guid> registeredProjects = [];
    private readonly ILexboxSignalRConnectionFactory connectionFactory =
        connectionFactory ?? new SignalRLexboxSignalRConnectionFactory(loggerFactory, httpMessageHandlerFactory, networkStatus, logger);
    private readonly ILexboxHubConnectionAuth auth = auth ?? new OAuthLexboxHubConnectionAuth(clientFactory);

    public LexboxServer Server { get; } = server;
    private ILexboxSignalRConnection? connection = initialConnection;
    private SemaphoreSlim connectionLock = new(1, 1);
    [MemberNotNullWhen(true, nameof(connection))]
    public bool IsConnected => connection?.State == HubConnectionState.Connected;
    [MemberNotNullWhen(true, nameof(connection))]
    public bool IsReconnecting => connection?.State == HubConnectionState.Reconnecting;

    public async ValueTask<bool> EnsureConnected(bool kickReconnecting = false, CancellationToken stoppingToken = default)
    {
        if (IsConnected) return true;
        if (kickReconnecting && IsReconnecting)
        {
            logger.LogInformation("Network recovery signal: interrupting mid-backoff reconnect to {Server}", Server.Authority);
            await CleanupConnection();
        }

        if (IsReconnecting) return false;

        using var release = await connectionLock.LockAsync(stoppingToken);
        if (connection?.State == HubConnectionState.Disconnected)
        {
            try
            {
                await connection.StartAsync(stoppingToken);
                await OnConnected(stoppingToken);
                return true;
            }
            catch (Exception e)
            {
                logger.LogWarning(e, "Failed to restart Lexbox listener");
                await CleanupConnection();
            }
        }

        try
        {
            if (IsConnected) return true;
            await CleanupConnection();
            connection = await NewConnection(stoppingToken);
            if (connection is null) return false;
            await OnConnected(stoppingToken);
            return IsConnected;
        }
        catch (Exception e)
        {
            logger.LogWarning(e, "Failed to start Lexbox listener");
            await CleanupConnection();
            return false;
        }
    }

    private async Task<ILexboxSignalRConnection?> NewConnection(CancellationToken stoppingToken)
    {
        if (!await auth.IsSignedIn(Server))
        {
            logger.LogWarning("Unable to create signalR client, user is not authenticated to {OriginDomain}",
                Server.Authority);
            return null;
        }
        var hubConnection = connectionFactory.Create(Server, () => auth.GetCurrentToken(Server));
        hubConnection.OnProjectUpdated((projectId, clientId) =>
        {
            OnProjectUpdated(projectId, clientId);
            return Task.CompletedTask;
        });
        hubConnection.Reconnecting += exception => OnReconnecting(hubConnection, exception);
        hubConnection.Closed += exception => OnClosed(hubConnection, exception);
        hubConnection.Reconnected += _ => OnReconnected(hubConnection);

        try
        {
            await hubConnection.StartAsync(stoppingToken);
            return hubConnection;
        }
        catch
        {
            await hubConnection.DisposeAsync();
            throw;
        }
    }

    public async ValueTask OnAuthChanged()
    {
        logger.LogInformation("Auth changed for {Server}, re-establishing push listeners for {ProjectCount} tracked project(s)",
            Server.Authority, registeredProjects.Count);
        await CleanupConnection();
        await EnsureConnected();
    }

    private async ValueTask OnConnected(CancellationToken stoppingToken = default)
    {
        await SubscribeRegisteredProjects(stoppingToken);
        TriggerSyncForServerProjects();
    }

    //it might make more sense for this method to return an observable, but we'll leave this as is for now
    public async Task ListenForProjectChanges(Guid projectId, CancellationToken stoppingToken = default, bool kickReconnecting = false)
    {
        registeredProjects.Add(projectId);
        if (!await EnsureConnected(kickReconnecting, stoppingToken)) return;
        if (!IsConnected) return;
        try
        {
            await ListenForProjectChangesCore(projectId, stoppingToken);
        }
        catch (Exception e)
        {
            // The connection can drop or be disposed between EnsureConnected and SendAsync. The project is
            // already registered above, so a later reconnect/revive will resubscribe it.
            logger.LogWarning(e, "SignalR connection not active while subscribing to project changes");
        }
    }

    private async Task SubscribeRegisteredProjects(CancellationToken stoppingToken = default)
    {
        if (!IsConnected) return;
        foreach (var projectId in registeredProjects)
        {
            try
            {
                await ListenForProjectChangesCore(projectId, stoppingToken);
            }
            catch (Exception e)
            {
                // Ensure one project's failing to resubscribe doesn't block others.
                logger.LogWarning(e, "Failed to resubscribe to project changes for {ProjectId}", projectId);
            }
        }
    }

    // Used by the Reconnected handler and after a manual revive — a manual
    // StartAsync does not raise Reconnected, so the revive path must resubscribe explicitly.
    private Task ListenForProjectChangesCore(Guid projectId, CancellationToken stoppingToken = default) => connection?.ListenForProjectChanges(projectId, stoppingToken) ?? Task.CompletedTask;

    private async Task OnReconnecting(ILexboxSignalRConnection hubConnection, Exception? exception)
    {
        if (!ReferenceEquals(connection, hubConnection)) return;

        if (exception is not null)
            logger.LogWarning(exception, "SignalR connection reconnecting");
        else
            logger.LogInformation("SignalR connection reconnecting");
        if (await auth.IsSignedIn(Server))
        {
            return;
        }

        // The retry policy never gives up; this breaks the loop when the user is logged out. Logged-out is tested
        // by account presence (IsSignedIn), not by whether a token can be fetched right now: reconnecting happens
        // precisely when the network is flaky, and fetching a token can require a refresh round-trip that fails
        // transiently — which would be a false logout. Account presence is a local-only read and only flips on a
        // real logout. Once the user signs back in, auth-change handling rebuilds the listener.
        logger.LogWarning("SignalR reconnect aborted: user is logged out; stopping connection");
        try
        {
            await CleanupConnection();
        }
        catch (Exception e)
        {
            logger.LogWarning(e, "Failed to stop SignalR connection after auth loss");
        }
    }

    private async Task OnReconnected(ILexboxSignalRConnection hubConnection)
    {
        if (!ReferenceEquals(connection, hubConnection)) return;

        // Auto-reconnected this same connection (no rebuild → the fresh-build sync below didn't run).
        logger.LogInformation("SignalR reconnected to {Server}; syncing", Server.Authority);
        await OnConnected();
    }

    private async Task OnClosed(ILexboxSignalRConnection hubConnection, Exception? exception)
    {
        //set to null if it is the same connection
        Interlocked.CompareExchange(ref connection, null, hubConnection);
        await hubConnection.DisposeAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await CleanupConnection();
    }

    private async ValueTask CleanupConnection()
    {
        var oldConnection = Interlocked.Exchange(ref connection, null);
        await (oldConnection?.DisposeAsync() ?? ValueTask.CompletedTask);
    }

    private void OnProjectUpdated(Guid projectId, Guid? clientId)
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

    // A listener (re)connecting moves no data on its own — the server only pushes OnProjectUpdated for changes
    // made after (re)subscribe — so changes authored while offline, and remote changes that arrived during the
    // outage, need an explicit sync once the connection is back. Call ONLY on a real connect transition (fresh
    // build, SignalR Reconnected, or a revive), never on a cache-hit re-ensure, otherwise it loops with the
    // post-sync TryEnsureProjectChangeListener.
    private void TriggerSyncForServerProjects()
    {
        // A genuine connect transition means SignalR just reached this server, which proves it's reachable.
        // Both these per-server caches can hold a stale failure from the offline period: the health verdict
        // (cached unhealthy for 30 min) would make this catch-up sync a silent no-op, and the project list
        // (cached empty for 5 min) would keep showing nothing. Drop both so they re-fetch now that we're back.
        CrdtHttpSyncService.InvalidateServerHealth(cache, Server.Authority.Authority);
        cache.Remove(LexboxProjectService.ProjectListCacheKey(Server));
        foreach (var project in registeredProjects)
        {
            try
            {
                backgroundSyncService.TriggerSync(project);
            }
            catch (Exception e)
            {
                // TriggerSync throws if the background sync service isn't running yet (e.g. startup/shutdown).
                logger.LogWarning(e, "Failed to trigger sync after listener (re)connect for {ProjectId}",
                    project);
            }
        }
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
            logger?.LogDebug(
                "SignalR reconnect attempt #{Attempt}: device reports online={IsOnline}, next retry in {Delay}",
                retryContext.PreviousRetryCount, isOnline, delay);
            return delay;
        }
    }
}

public interface ILexboxHubConnectionAuth
{
    ValueTask<bool> IsSignedIn(LexboxServer server);
    ValueTask<string?> GetCurrentToken(LexboxServer server);
}

internal sealed class OAuthLexboxHubConnectionAuth(OAuthClientFactory clientFactory) : ILexboxHubConnectionAuth
{
    public ValueTask<bool> IsSignedIn(LexboxServer server) => clientFactory.GetClient(server).IsSignedIn();

    public ValueTask<string?> GetCurrentToken(LexboxServer server) => clientFactory.GetClient(server).GetCurrentToken();
}

public interface ILexboxSignalRConnectionFactory
{
    ILexboxSignalRConnection Create(LexboxServer server, Func<ValueTask<string?>> accessTokenProvider);
}

public interface ILexboxSignalRConnection : IAsyncDisposable
{
    HubConnectionState State { get; }
    Task StartAsync(CancellationToken cancellationToken = default);
    Task ListenForProjectChanges(Guid projectId, CancellationToken cancellationToken = default);
    IDisposable OnProjectUpdated(Func<Guid, Guid?, Task> handler);
    event Func<Exception?, Task>? Reconnecting;
    event Func<Exception?, Task>? Closed;
    event Func<string?, Task>? Reconnected;
}

internal sealed class SignalRLexboxSignalRConnectionFactory(
    ILoggerFactory loggerFactory,
    IHttpMessageHandlerFactory httpMessageHandlerFactory,
    INetworkStatus networkStatus,
    ILogger logger) : ILexboxSignalRConnectionFactory
{
    public ILexboxSignalRConnection Create(LexboxServer server, Func<ValueTask<string?>> accessTokenProvider)
    {
        var hubConnection = new HubConnectionBuilder()
            // Hand the builder's internal DI the app's logger factory so HubConnection's own logs (reconnect
            // attempts, close reasons) land in the app log instead of a console-only factory. An externally
            // supplied instance is not disposed with the connection's service provider.
            .ConfigureLogging(logging => logging.Services.AddSingleton(loggerFactory))
            .WithAutomaticReconnect(new LexboxHubConnection.AdaptiveRetryPolicy(networkStatus, logger))
            .WithUrl($"{server.Authority}api/hub/crdt/project-changes",
                connectionOptions =>
                {
                    connectionOptions.HttpMessageHandlerFactory = handler =>
                    {
                        // Use a client that does not validate certs in dev.
                        return httpMessageHandlerFactory.CreateHandler(OAuthClient.AuthHttpClientName);
                    };
                    connectionOptions.AccessTokenProvider = async () => await accessTokenProvider();
                })
            .Build();

        return new SignalRConnectionAdapter(hubConnection);
    }
}

internal sealed class SignalRConnectionAdapter(HubConnection connection) : ILexboxSignalRConnection
{
    public HubConnectionState State => connection.State;

    public event Func<Exception?, Task>? Reconnecting
    {
        add => connection.Reconnecting += value;
        remove => connection.Reconnecting -= value;
    }

    public event Func<Exception?, Task>? Closed
    {
        add => connection.Closed += value;
        remove => connection.Closed -= value;
    }

    public event Func<string?, Task>? Reconnected
    {
        add => connection.Reconnected += value;
        remove => connection.Reconnected -= value;
    }

    public Task StartAsync(CancellationToken cancellationToken = default) => connection.StartAsync(cancellationToken);

    public Task ListenForProjectChanges(Guid projectId, CancellationToken cancellationToken = default) =>
        connection.SendAsync(nameof(IProjectChangeHubServer.ListenForProjectChanges), projectId, cancellationToken);

    public IDisposable OnProjectUpdated(Func<Guid, Guid?, Task> handler) =>
        connection.On(nameof(IProjectChangeHubClient.OnProjectUpdated), handler);

    public ValueTask DisposeAsync() => connection.DisposeAsync();
}
