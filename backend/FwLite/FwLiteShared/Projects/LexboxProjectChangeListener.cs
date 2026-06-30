using System.Collections.Concurrent;
using FwLiteShared.Auth;
using FwLiteShared.Events;
using LcmCrdt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FwLiteShared.Projects;

public sealed class LexboxProjectChangeListener : IAsyncDisposable
{
    private readonly ConcurrentDictionary<LexboxServer, LexboxHubConnection> _connections = new();
    private readonly IServiceProvider serviceProvider;
    private readonly IOptions<AuthConfig> options;
    private readonly ILogger<LexboxProjectChangeListener> logger;
    private readonly IDisposable authChangedSubscription;

    public LexboxProjectChangeListener(
        IServiceProvider serviceProvider,
        IOptions<AuthConfig> options,
        GlobalEventBus globalEventBus,
        ILogger<LexboxProjectChangeListener> logger)
    {
        this.serviceProvider = serviceProvider;
        this.options = options;
        this.logger = logger;

        authChangedSubscription = globalEventBus.OnAuthenticationChanged
            .Subscribe(@event => _ = HandleAuthChanged(@event.Server));
    }

    public async Task HandleAuthChanged(LexboxServer server)
    {
        try
        {
            _connections.TryGetValue(server, out var connection);
            if (connection is not null)
            {
                await connection.OnAuthChanged();
            }
        }
        catch (Exception e)
        {
            logger.LogWarning(e, "Failed to handle authentication change for {Server}", server.Authority);
        }
    }

    public async Task EnsureListenersForTrackedProjects(bool kickReconnecting = false, CancellationToken cancellationToken = default)
    {
        foreach (var lexboxHubConnection in _connections.Values)
        {
            await lexboxHubConnection.EnsureConnected(kickReconnecting, cancellationToken);
        }
    }

    public async Task ListenForProjectChanges(ProjectData projectData, CancellationToken stoppingToken = default, bool kickReconnecting = false)
    {
        if (!options.Value.TryGetServer(projectData, out var server)) return;
        var connection = _connections.GetOrAdd(server,
            (s) => ActivatorUtilities.CreateInstance<LexboxHubConnection>(serviceProvider, s));
        await connection.ListenForProjectChanges(projectData.Id, stoppingToken, kickReconnecting);
    }

    public async ValueTask DisposeAsync()
    {
        authChangedSubscription.Dispose();
        foreach (var connection in _connections.Values)
        {
            await connection.DisposeAsync();
        }
        _connections.Clear();
    }
}
