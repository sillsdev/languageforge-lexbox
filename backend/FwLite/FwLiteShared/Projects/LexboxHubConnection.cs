using FwLiteShared.Auth;
using FwLiteShared.Sync;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MiniLcm.Push;

namespace FwLiteShared.Projects;

public sealed class LexboxHubConnection(
    LexboxServer server,
    ILoggerFactory loggerFactory,
    IHttpMessageHandlerFactory httpMessageHandlerFactory,
    OAuthClientFactory clientFactory,
    BackgroundSyncService backgroundSyncService,
    ILogger logger) : IAsyncDisposable
{
    private readonly HashSet<Guid> registeredProjects = [];

    public LexboxServer Server { get; } = server;
    private HubConnection? connection;
    public HubConnectionState State => connection?.State ?? HubConnectionState.Disconnected;
    public bool IsConnected => State == HubConnectionState.Connected;
    public bool IsReconnecting => State == HubConnectionState.Reconnecting;
    public bool IsDisconnected => State == HubConnectionState.Disconnected;

    private async Task NewConnection(CancellationToken stoppingToken)
    {
        connection = new HubConnectionBuilder()
            // Hand the builder's internal DI the app's logger factory so HubConnection's own logs (reconnect
            // attempts, close reasons) land in the app log instead of a console-only factory. An externally
            // supplied instance is not disposed with the connection's service provider.
            .ConfigureLogging(logging => logging.Services.AddSingleton(loggerFactory))
            .WithAutomaticReconnect(new LexboxProjectChangeListener.AdaptiveRetryPolicy(networkStatus, logger))
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
        
        connection.On(nameof(IProjectChangeHubClient.OnProjectUpdated),
            (Guid projectId, Guid? clientId) =>
            {
                OnProjectUpdated(projectId, clientId);
                return Task.CompletedTask;
            });
        connection.Reconnecting += OnReconnecting;
        await connection.StartAsync(stoppingToken);
        connection.Closed += OnClosed;
        connection.Reconnected += OnReconnected;

        foreach (var project in registeredProjects)
        {
            await ListenForProjectChanges(project, stoppingToken);
        }
    }

    public async ValueTask EnsureConnected(bool kickReconnecting = false, CancellationToken stoppingToken = default)
    {
        if (IsConnected) return;
        if (connection is null)
        {
            
        } else if (kickReconnecting && IsReconnecting)
        {
            await CleanupConnection();
        } else if (IsDisconnected)
        {
            try
            {
                await connection.StartAsync(stoppingToken);
                //todo: resubscribe registered projects
            } catch (Exception e)
            {
                logger.LogWarning(e, "Failed to restart Lexbox listener");
                await CleanupConnection();
            }
        }

        await NewConnection(stoppingToken);
    }

    private async ValueTask CleanupConnection()
    {
        await (connection?.DisposeAsync() ?? ValueTask.CompletedTask);
        connection = null;
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
        await connection.SendAsync(nameof(IProjectChangeHubServer.ListenForProjectChanges), projectId, stoppingToken);
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
                await connection.SendAsync(nameof(IProjectChangeHubServer.ListenForProjectChanges), projectId);
            }
            catch (Exception)
            {
                // Ensure one project's failing to resubscribe doesn't block others.
            }
        }
    }

    // The retry policy never gives up; this breaks the loop when the user is logged out. Logged-out is tested
    // by account presence (IsSignedIn), not by whether a token can be fetched right now: reconnecting happens
    // precisely when the network is flaky, and fetching a token can require a refresh round-trip that fails
    // transiently — which would be a false logout. Account presence is a local-only read and only flips on a
    // real logout. Once the user signs back in, auth-change handling rebuilds the listener.
    internal async Task HandleReconnecting(
        bool isSignedIn,
        Exception? exception,
        Func<Task>? stopConnection = null)
    {
        if (exception is not null)
            logger.LogWarning(exception, "SignalR connection reconnecting");
        else
            logger.LogInformation("SignalR connection reconnecting");

        if (isSignedIn) return;

        // Same guard as the Closed handler: a replacement connection may have been cached since this one
        // started reconnecting; only evict the entry if it is still ours. Stopping ourselves is right
        // regardless — this connection belongs to a signed-out user.
        registry.RemoveIfCached(Server, this);
        logger.LogWarning("SignalR reconnect aborted: user is logged out; stopping connection");
        try
        {
            await (stopConnection ?? StopAsync)();
        }
        catch (Exception e)
        {
            logger.LogWarning(e, "Failed to stop SignalR connection after auth loss");
        }
    }

    public async Task StopAsync()
    {
        await connection.StopAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await connection.DisposeAsync();
    }

    private async Task OnReconnecting(Exception? exception)
    {
        await HandleReconnecting(await isSignedIn(), exception);
    }

    private async Task OnClosed(Exception? exception)
    {
        // A stopped connection may have been replaced in the cache before this handler runs. Don't
        // blindly remove the cache entry on Closed — only do so if WE are the cached connection,
        // otherwise we'd evict the live replacement.
        registry.RemoveIfCached(Server, this);
        await DisposeAsync();
    }

    private Task OnReconnected(string? connectionId)
    {
        // Auto-reconnected this same connection (no rebuild → the fresh-build sync below didn't run).
        logger.LogInformation("SignalR reconnected to {Server}; syncing", Server.Authority);
        onConnected(Server);
        return Task.CompletedTask;
    }

}
