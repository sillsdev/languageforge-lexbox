using System.Collections.Concurrent;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Options;
using MiniLcm.Push;

namespace FwHeadless.Services;

/// <summary>
/// Listens on Lexbox's CrdtProjectChangeHub for commits pushed to the headless harmony projects FwHeadless holds,
/// as the service account. FW Lite's listener for the same hub is FwLiteShared/Projects/LexboxHubConnection.cs:
/// a change to the hub contract (IProjectChangeHubServer/IProjectChangeHubClient, route, resubscribe after reconnect)
/// needs the same change there.
/// </summary>
public sealed class LexboxProjectChangeListener(
    IHttpMessageHandlerFactory httpMessageHandlerFactory,
    IOptions<FwHeadlessConfig> config,
    ILoggerFactory loggerFactory,
    ILogger<LexboxProjectChangeListener> logger,
    Action<Guid, Guid?> onProjectUpdated) : IAsyncDisposable
{
    private const string HubPath = "api/hub/crdt/project-changes";
    private readonly ConcurrentDictionary<Guid, byte> _wanted = new();
    // projects subscribed on the current connection; the hub forgets groups when a connection drops
    private readonly HashSet<Guid> _subscribed = [];
    private readonly SemaphoreSlim _lock = new(1, 1);
    private HubConnection? _connection;

    /// <summary>
    /// Adds the projects to those listened to and subscribes any not yet subscribed on the current connection,
    /// (re)connecting first when needed.
    /// </summary>
    public async Task ListenTo(IEnumerable<Guid> projectIds, CancellationToken stoppingToken)
    {
        foreach (var projectId in projectIds) _wanted.TryAdd(projectId, 0);
        await _lock.WaitAsync(stoppingToken);
        try
        {
            if (!await EnsureConnected(stoppingToken)) return;
            await SubscribeWanted(stoppingToken);
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<bool> EnsureConnected(CancellationToken stoppingToken)
    {
        switch (_connection?.State)
        {
            case HubConnectionState.Connected:
                return true;
            case HubConnectionState.Connecting or HubConnectionState.Reconnecting:
                return false;
        }

        await CleanupConnection();
        var connection = NewConnection();
        try
        {
            await connection.StartAsync(stoppingToken);
        }
        catch (Exception e) when (!stoppingToken.IsCancellationRequested)
        {
            logger.LogWarning(e, "Failed to connect to the Lexbox project change hub, retrying on the next scan");
            await connection.DisposeAsync();
            return false;
        }
        _connection = connection;
        logger.LogInformation("Connected to the Lexbox project change hub");
        return true;
    }

    private HubConnection NewConnection()
    {
        var connection = new HubConnectionBuilder()
            .ConfigureLogging(logging => logging.Services.AddSingleton(loggerFactory))
            .WithAutomaticReconnect(new RetryForeverPolicy())
            .WithUrl(new Uri(new Uri(config.Value.LexboxUrl), HubPath),
                // The service account authenticates with a cookie added by HttpClientAuthHandler in the Lexbox client's
                // handler pipeline. Only the HTTP transports send their requests through that pipeline, so no WebSockets.
                HttpTransportType.ServerSentEvents | HttpTransportType.LongPolling,
                options => options.HttpMessageHandlerFactory =
                    _ => httpMessageHandlerFactory.CreateHandler(FwHeadlessKernel.LexboxHttpClientName))
            .Build();
        connection.On<Guid, Guid?>(nameof(IProjectChangeHubClient.OnProjectUpdated), (projectId, clientId) =>
        {
            onProjectUpdated(projectId, clientId);
            return Task.CompletedTask;
        });
        connection.Reconnected += async _ =>
        {
            logger.LogInformation("Reconnected to the Lexbox project change hub, resubscribing");
            await _lock.WaitAsync();
            try
            {
                _subscribed.Clear();
                await SubscribeWanted(CancellationToken.None);
            }
            finally
            {
                _lock.Release();
            }
        };
        return connection;
    }

    private async Task SubscribeWanted(CancellationToken stoppingToken)
    {
        if (_connection is not { State: HubConnectionState.Connected } connection) return;
        foreach (var projectId in _wanted.Keys)
        {
            if (_subscribed.Contains(projectId)) continue;
            try
            {
                await connection.InvokeAsync(nameof(IProjectChangeHubServer.ListenForProjectChanges), projectId, stoppingToken);
                _subscribed.Add(projectId);
            }
            catch (Exception e) when (!stoppingToken.IsCancellationRequested)
            {
                // one project failing (e.g. deleted on Lexbox) must not block the others; retried on the next scan
                logger.LogWarning(e, "Failed to listen for changes to project {ProjectId}", projectId);
            }
        }
    }

    private async Task CleanupConnection()
    {
        _subscribed.Clear();
        var connection = _connection;
        _connection = null;
        if (connection is not null) await connection.DisposeAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await CleanupConnection();
        _lock.Dispose();
    }

    private sealed class RetryForeverPolicy : IRetryPolicy
    {
        public TimeSpan? NextRetryDelay(RetryContext retryContext) => retryContext.PreviousRetryCount switch
        {
            0 => TimeSpan.Zero,
            1 => TimeSpan.FromSeconds(5),
            _ => TimeSpan.FromSeconds(30),
        };
    }
}
