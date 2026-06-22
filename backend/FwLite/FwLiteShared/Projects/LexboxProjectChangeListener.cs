using System.Collections.Concurrent;
using FwLiteShared.Auth;
using LcmCrdt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FwLiteShared.Projects;

public sealed class LexboxProjectChangeListener(IServiceProvider serviceProvider, IOptions<AuthConfig> options) : IAsyncDisposable
{
    private readonly ConcurrentDictionary<LexboxServer, LexboxHubConnection> _connections = new();

    public async Task HandleAuthChanged(LexboxServer server)
    {
        _connections.TryGetValue(server, out var connection);
        if (connection is not null) {
            await connection.OnAuthChanged();
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
        foreach (var connection in _connections.Values)
        {
            await connection.DisposeAsync();
        }
        _connections.Clear();
    }
}
