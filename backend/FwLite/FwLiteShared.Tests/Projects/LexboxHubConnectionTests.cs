using FwLiteShared.Auth;
using FwLiteShared.Projects;
using FwLiteShared.Services;
using FwLiteShared.Sync;
using LcmCrdt;
using LcmCrdt.RemoteSync;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using MiniLcm.Models;

namespace FwLiteShared.Tests.Projects;

public class LexboxHubConnectionTests
{
    private static readonly LexboxServer Server = new(new Uri("https://example.test/"), "Test");

    [Fact]
    public async Task EnsureConnected_WithConnectedInitialConnection_DoesNotConsultAuthOrFactory()
    {
        var connection = new FakeSignalRConnection { State = HubConnectionState.Connected };
        var auth = new FakeHubConnectionAuth { ThrowOnUse = true };
        var factory = new FakeSignalRConnectionFactory { ThrowOnCreate = true };
        await using var hubConnection = CreateHubConnection(factory, auth, connection);

        var result = await hubConnection.EnsureConnected();

        result.Should().BeTrue();
        auth.IsSignedInCalls.Should().Be(0);
        factory.CreateCalls.Should().Be(0);
        connection.DisposeCalls.Should().Be(0);
    }

    [Fact]
    public async Task EnsureConnected_WhenLoggedOut_DoesNotCreateConnection()
    {
        var auth = new FakeHubConnectionAuth { SignedIn = false };
        var factory = new FakeSignalRConnectionFactory();
        await using var hubConnection = CreateHubConnection(factory, auth);

        var result = await hubConnection.EnsureConnected();

        result.Should().BeFalse();
        factory.CreateCalls.Should().Be(0);
    }

    [Fact]
    public async Task Reconnecting_WhenSignedIn_KeepsConnection()
    {
        var connection = new FakeSignalRConnection();
        var auth = new FakeHubConnectionAuth { SignedIn = true };
        var factory = new FakeSignalRConnectionFactory(connection);
        await using var hubConnection = CreateHubConnection(factory, auth);

        (await hubConnection.EnsureConnected()).Should().BeTrue();
        await connection.RaiseReconnecting();

        hubConnection.IsReconnecting.Should().BeTrue();
        connection.DisposeCalls.Should().Be(0);
    }

    [Fact]
    public async Task EnsureConnected_WhenReconnectingWithoutKick_ReturnsFalseAndKeepsConnection()
    {
        var connection = new FakeSignalRConnection { State = HubConnectionState.Reconnecting };
        var auth = new FakeHubConnectionAuth { ThrowOnUse = true };
        var factory = new FakeSignalRConnectionFactory { ThrowOnCreate = true };
        await using var hubConnection = CreateHubConnection(factory, auth, connection);

        var result = await hubConnection.EnsureConnected();

        result.Should().BeFalse();
        connection.DisposeCalls.Should().Be(0);
        factory.CreateCalls.Should().Be(0);
        auth.IsSignedInCalls.Should().Be(0);
    }

    [Fact]
    public async Task EnsureConnected_WhenReconnectingWithKick_DisposesAndCreatesReplacement()
    {
        var projectId = Guid.NewGuid();
        var reconnecting = new FakeSignalRConnection { State = HubConnectionState.Reconnecting };
        var replacement = new FakeSignalRConnection();
        var factory = new FakeSignalRConnectionFactory(replacement);
        await using var hubConnection = CreateHubConnection(factory, initialConnection: reconnecting);

        await hubConnection.ListenForProjectChanges(projectId, kickReconnecting: true);

        reconnecting.DisposeCalls.Should().Be(1);
        replacement.State.Should().Be(HubConnectionState.Connected);
        replacement.ProjectSubscriptions.Should().Contain(projectId);
        factory.CreateCalls.Should().Be(1);
    }

    [Fact]
    public async Task Reconnecting_WhenLoggedOut_DisposesConnection()
    {
        var connection = new FakeSignalRConnection();
        var auth = new FakeHubConnectionAuth { SignedIn = true };
        var factory = new FakeSignalRConnectionFactory(connection);
        await using var hubConnection = CreateHubConnection(factory, auth);

        (await hubConnection.EnsureConnected()).Should().BeTrue();
        auth.SignedIn = false;
        await connection.RaiseReconnecting();

        hubConnection.IsConnected.Should().BeFalse();
        connection.DisposeCalls.Should().Be(1);
    }

    [Fact]
    public async Task Reconnecting_FromStaleConnection_DoesNotDisposeReplacement()
    {
        var stale = new FakeSignalRConnection();
        var replacement = new FakeSignalRConnection();
        var auth = new FakeHubConnectionAuth { SignedIn = true };
        var factory = new FakeSignalRConnectionFactory(stale, replacement);
        await using var hubConnection = CreateHubConnection(factory, auth);

        (await hubConnection.EnsureConnected()).Should().BeTrue();
        await hubConnection.OnAuthChanged();
        auth.SignedIn = false;
        await stale.RaiseReconnecting();

        hubConnection.IsConnected.Should().BeTrue();
        stale.DisposeCalls.Should().Be(1);
        replacement.DisposeCalls.Should().Be(0);
    }

    [Fact]
    public async Task EnsureConnected_WithDisconnectedConnection_RestartsSameConnectionWithoutFactory()
    {
        var disconnected = new FakeSignalRConnection { State = HubConnectionState.Disconnected };
        var auth = new FakeHubConnectionAuth { ThrowOnUse = true };
        var factory = new FakeSignalRConnectionFactory { ThrowOnCreate = true };
        await using var hubConnection = CreateHubConnection(factory, auth, disconnected);

        var result = await hubConnection.EnsureConnected();

        result.Should().BeTrue();
        disconnected.StartCalls.Should().Be(1);
        disconnected.State.Should().Be(HubConnectionState.Connected);
        disconnected.DisposeCalls.Should().Be(0);
        factory.CreateCalls.Should().Be(0);
    }

    [Fact]
    public async Task EnsureConnected_WithDisconnectedConnectionRestartFailure_DisposesAndCreatesNewConnection()
    {
        var disconnected = new FakeSignalRConnection
        {
            State = HubConnectionState.Disconnected,
            StartException = new InvalidOperationException("restart failed"),
        };
        var replacement = new FakeSignalRConnection();
        var factory = new FakeSignalRConnectionFactory(replacement);
        await using var hubConnection = CreateHubConnection(factory, initialConnection: disconnected);

        var result = await hubConnection.EnsureConnected();

        result.Should().BeTrue();
        disconnected.StartCalls.Should().Be(1);
        disconnected.DisposeCalls.Should().Be(1);
        replacement.StartCalls.Should().Be(1);
        replacement.State.Should().Be(HubConnectionState.Connected);
    }

    [Fact]
    public async Task EnsureConnected_WhenNewConnectionStartFails_DisposesCreatedConnectionAndReturnsFalse()
    {
        var failedConnection = new FakeSignalRConnection
        {
            StartException = new InvalidOperationException("start failed"),
        };
        var factory = new FakeSignalRConnectionFactory(failedConnection);
        await using var hubConnection = CreateHubConnection(factory);

        var result = await hubConnection.EnsureConnected();

        result.Should().BeFalse();
        failedConnection.StartCalls.Should().Be(1);
        failedConnection.DisposeCalls.Should().Be(1);
        factory.CreateCalls.Should().Be(1);
    }

    [Fact]
    public async Task ListenForProjectChanges_WhenFirstConnectFails_RegistersProjectForLaterSubscribe()
    {
        var firstProject = Guid.NewGuid();
        var secondProject = Guid.NewGuid();
        var failedConnection = new FakeSignalRConnection
        {
            StartException = new InvalidOperationException("network unavailable"),
        };
        var recoveredConnection = new FakeSignalRConnection();
        var factory = new FakeSignalRConnectionFactory(failedConnection, recoveredConnection);
        await using var hubConnection = CreateHubConnection(factory);

        await hubConnection.ListenForProjectChanges(firstProject);
        await hubConnection.ListenForProjectChanges(secondProject);

        failedConnection.DisposeCalls.Should().Be(1);
        recoveredConnection.ProjectSubscriptions.Should().Contain(firstProject);
        recoveredConnection.ProjectSubscriptions.Should().Contain(secondProject);
    }

    [Fact]
    public async Task ListenForProjectChanges_WhenSubscribeSendFails_SwallowsAndRegistersForLater()
    {
        var projectId = Guid.NewGuid();
        var connection = new FakeSignalRConnection
        {
            SubscribeException = new InvalidOperationException("send failed"),
        };
        var factory = new FakeSignalRConnectionFactory(connection);
        await using var hubConnection = CreateHubConnection(factory);

        await hubConnection.ListenForProjectChanges(projectId);

        connection.ProjectSubscriptions.Should().BeEmpty();

        connection.SubscribeException = null;
        await connection.RaiseReconnected();

        connection.ProjectSubscriptions.Should().Contain(projectId);
    }

    [Fact]
    public async Task OnAuthChanged_WhenSignedIn_RebuildsConnectionAndResubscribesRegisteredProjects()
    {
        var projectId = Guid.NewGuid();
        var oldConnection = new FakeSignalRConnection();
        var replacement = new FakeSignalRConnection();
        var factory = new FakeSignalRConnectionFactory(oldConnection, replacement);
        await using var hubConnection = CreateHubConnection(factory);

        await hubConnection.ListenForProjectChanges(projectId);
        await hubConnection.OnAuthChanged();

        oldConnection.DisposeCalls.Should().Be(1);
        replacement.State.Should().Be(HubConnectionState.Connected);
        replacement.ProjectSubscriptions.Should().Contain(projectId);
    }

    [Fact]
    public async Task OnAuthChanged_WhenLoggedOut_DisposesConnectionAndDoesNotCreateReplacement()
    {
        var connection = new FakeSignalRConnection { State = HubConnectionState.Connected };
        var auth = new FakeHubConnectionAuth { SignedIn = false };
        var factory = new FakeSignalRConnectionFactory { ThrowOnCreate = true };
        await using var hubConnection = CreateHubConnection(factory, auth, connection);

        await hubConnection.OnAuthChanged();

        connection.DisposeCalls.Should().Be(1);
        hubConnection.IsConnected.Should().BeFalse();
        auth.IsSignedInCalls.Should().Be(1);
        factory.CreateCalls.Should().Be(0);
    }

    [Fact]
    public async Task Reconnected_ResubscribesRegisteredProjects()
    {
        var projectId = Guid.NewGuid();
        var connection = new FakeSignalRConnection();
        var factory = new FakeSignalRConnectionFactory(connection);
        await using var hubConnection = CreateHubConnection(factory);

        await hubConnection.ListenForProjectChanges(projectId);
        connection.ProjectSubscriptions.Clear();
        await connection.RaiseReconnected();

        connection.ProjectSubscriptions.Should().Contain(projectId);
    }

    [Fact]
    public async Task SubscribeRegisteredProjects_WhenOneProjectFails_ContinuesWithRemainingProjects()
    {
        var firstProject = Guid.NewGuid();
        var secondProject = Guid.NewGuid();
        var connection = new FakeSignalRConnection();
        var factory = new FakeSignalRConnectionFactory(connection);
        await using var hubConnection = CreateHubConnection(factory);

        await hubConnection.ListenForProjectChanges(firstProject);
        await hubConnection.ListenForProjectChanges(secondProject);
        connection.ProjectSubscriptions.Clear();
        connection.ProjectSubscriptionAttempts.Clear();
        connection.SubscribeFailures.Add(firstProject);

        await connection.RaiseReconnected();

        connection.ProjectSubscriptionAttempts.Should().BeEquivalentTo([firstProject, secondProject]);//order is not guaranteed
        connection.ProjectSubscriptions.Should().NotContain(firstProject);
        connection.ProjectSubscriptions.Should().Contain(secondProject);
    }

    [Fact]
    public async Task ProjectUpdatedCallback_TriggersBackgroundSyncWithClientId()
    {
        var projectId = Guid.NewGuid();
        var clientId = Guid.NewGuid();
        var connection = new FakeSignalRConnection();
        var backgroundSync = new FakeBackgroundSyncService();
        await using var hubConnection = CreateHubConnection(
            new FakeSignalRConnectionFactory(connection),
            backgroundSyncService: backgroundSync);

        (await hubConnection.EnsureConnected()).Should().BeTrue();
        await connection.RaiseProjectUpdated(projectId, clientId);

        backgroundSync.ProjectIdTriggers.Should().ContainSingle()
            .Which.Should().Be((projectId, clientId));
    }

    [Fact]
    public async Task ProjectUpdatedCallback_WhenBackgroundSyncThrows_DoesNotThrow()
    {
        var connection = new FakeSignalRConnection();
        var backgroundSync = new FakeBackgroundSyncService
        {
            TriggerSyncException = new InvalidOperationException("sync service not running"),
        };
        await using var hubConnection = CreateHubConnection(
            new FakeSignalRConnectionFactory(connection),
            backgroundSyncService: backgroundSync);

        (await hubConnection.EnsureConnected()).Should().BeTrue();
        var act = () => connection.RaiseProjectUpdated(Guid.NewGuid(), Guid.NewGuid());

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task OnConnected_InvalidatesServerCachesAndTriggersCatchupSyncForRegisteredProjects()
    {
        var projectId = Guid.NewGuid();
        var connection = new FakeSignalRConnection();
        var cache = new MemoryCache(new MemoryCacheOptions());
        var backgroundSync = new FakeBackgroundSyncService();
        var projectListCacheKey = LexboxProjectService.ProjectListCacheKey(Server);
        var serverHealthCacheKey = CrdtHttpSyncService.HealthCacheKey(Server.Authority.Authority);
        cache.Set(projectListCacheKey, "stale projects");
        cache.Set(serverHealthCacheKey, false);
        await using var hubConnection = CreateHubConnection(
            new FakeSignalRConnectionFactory(connection),
            backgroundSyncService: backgroundSync,
            cache: cache);

        await hubConnection.ListenForProjectChanges(projectId);
        backgroundSync.ProjectIdTriggers.Clear();
        cache.Set(projectListCacheKey, "stale projects");
        cache.Set(serverHealthCacheKey, false);

        await connection.RaiseReconnected();

        cache.TryGetValue(projectListCacheKey, out _).Should().BeFalse();
        cache.TryGetValue(serverHealthCacheKey, out _).Should().BeFalse();
        backgroundSync.ProjectIdTriggers.Should().Contain((projectId, null));
    }

    [Fact]
    public async Task EnsureConnected_WhenAlreadyConnected_DoesNotTriggerCatchupSyncAgain()
    {
        var projectId = Guid.NewGuid();
        var connection = new FakeSignalRConnection { State = HubConnectionState.Connected };
        var cache = new MemoryCache(new MemoryCacheOptions());
        var backgroundSync = new FakeBackgroundSyncService();
        var projectListCacheKey = LexboxProjectService.ProjectListCacheKey(Server);
        var serverHealthCacheKey = "ServerHealth|" + Server.Authority.Authority;
        cache.Set(projectListCacheKey, "stale projects");
        cache.Set(serverHealthCacheKey, false);
        await using var hubConnection = CreateHubConnection(
            new FakeSignalRConnectionFactory { ThrowOnCreate = true },
            auth: new FakeHubConnectionAuth { ThrowOnUse = true },
            initialConnection: connection,
            backgroundSyncService: backgroundSync,
            cache: cache);

        await hubConnection.ListenForProjectChanges(projectId);
        backgroundSync.ProjectIdTriggers.Clear();
        cache.Set(projectListCacheKey, "stale projects");
        cache.Set(serverHealthCacheKey, false);

        var result = await hubConnection.EnsureConnected();

        result.Should().BeTrue();
        cache.TryGetValue(projectListCacheKey, out _).Should().BeTrue();
        cache.TryGetValue(serverHealthCacheKey, out _).Should().BeTrue();
        backgroundSync.ProjectIdTriggers.Should().BeEmpty();
    }

    [Fact]
    public async Task Closed_FromCurrentConnection_ClearsConnectionAndAllowsFutureReconnect()
    {
        var first = new FakeSignalRConnection();
        var replacement = new FakeSignalRConnection();
        var factory = new FakeSignalRConnectionFactory(first, replacement);
        await using var hubConnection = CreateHubConnection(factory);

        (await hubConnection.EnsureConnected()).Should().BeTrue();
        await first.RaiseClosed();
        hubConnection.IsConnected.Should().BeFalse();
        first.DisposeCalls.Should().Be(1);

        (await hubConnection.EnsureConnected()).Should().BeTrue();
        replacement.State.Should().Be(HubConnectionState.Connected);
        factory.CreateCalls.Should().Be(2);
    }

    [Fact]
    public async Task Closed_FromStaleConnection_DisposesOnlyStaleConnection()
    {
        var stale = new FakeSignalRConnection();
        var replacement = new FakeSignalRConnection();
        var factory = new FakeSignalRConnectionFactory(stale, replacement);
        await using var hubConnection = CreateHubConnection(factory);

        (await hubConnection.EnsureConnected()).Should().BeTrue();
        await hubConnection.OnAuthChanged();
        await stale.RaiseClosed();

        hubConnection.IsConnected.Should().BeTrue();
        stale.DisposeCalls.Should().BeGreaterThan(0);
        replacement.DisposeCalls.Should().Be(0);
    }

    [Fact]
    public async Task DisposeAsync_DisposesCurrentConnectionAndIsSafeWhenCalledAgain()
    {
        var connection = new FakeSignalRConnection { State = HubConnectionState.Connected };
        var hubConnection = CreateHubConnection(new FakeSignalRConnectionFactory(), initialConnection: connection);

        await hubConnection.DisposeAsync();
        await hubConnection.DisposeAsync();

        connection.DisposeCalls.Should().Be(1);
    }

    private static LexboxHubConnection CreateHubConnection(
        FakeSignalRConnectionFactory factory,
        FakeHubConnectionAuth? auth = null,
        FakeSignalRConnection? initialConnection = null,
        FakeBackgroundSyncService? backgroundSyncService = null,
        IMemoryCache? cache = null)
    {
        return new LexboxHubConnection(
            Server,
            NullLoggerFactory.Instance,
            httpMessageHandlerFactory: null!,
            clientFactory: null!,
            backgroundSyncService ?? new FakeBackgroundSyncService(),
            new FakeNetworkStatus(),
            cache ?? new MemoryCache(new MemoryCacheOptions()),
            NullLogger<LexboxHubConnection>.Instance,
            initialConnection,
            factory,
            auth ?? new FakeHubConnectionAuth { SignedIn = true });
    }

    private sealed class FakeHubConnectionAuth : ILexboxHubConnectionAuth
    {
        public bool SignedIn { get; set; }
        public bool ThrowOnUse { get; init; }
        public int IsSignedInCalls { get; private set; }

        public ValueTask<bool> IsSignedIn(LexboxServer server)
        {
            if (ThrowOnUse) throw new InvalidOperationException("Auth should not be consulted");
            IsSignedInCalls++;
            return ValueTask.FromResult(SignedIn);
        }

        public ValueTask<string?> GetCurrentToken(LexboxServer server)
        {
            if (ThrowOnUse) throw new InvalidOperationException("Auth should not be consulted");
            return ValueTask.FromResult<string?>("token");
        }
    }

    private sealed class FakeSignalRConnectionFactory : ILexboxSignalRConnectionFactory
    {
        private readonly Queue<FakeSignalRConnection> connections;
        public bool ThrowOnCreate { get; init; }
        public int CreateCalls { get; private set; }

        public FakeSignalRConnectionFactory(params FakeSignalRConnection[] connections)
        {
            this.connections = new Queue<FakeSignalRConnection>(connections);
        }

        public ILexboxSignalRConnection Create(LexboxServer server, Func<ValueTask<string?>> accessTokenProvider)
        {
            if (ThrowOnCreate) throw new InvalidOperationException("Connection should not be created");
            CreateCalls++;
            return connections.Dequeue();
        }
    }

    private sealed class FakeBackgroundSyncService : IBackgroundSyncService
    {
        public List<(Guid ProjectId, Guid? IgnoredClientId)> ProjectIdTriggers { get; } = [];
        public List<string> ProjectNameTriggers { get; } = [];
        public Exception? TriggerSyncException { get; set; }

        public void TriggerSync(Guid projectId, Guid? ignoredClientId = null)
        {
            if (TriggerSyncException is not null) throw TriggerSyncException;
            ProjectIdTriggers.Add((projectId, ignoredClientId));
        }

        public void TriggerSync(IProjectIdentifier project)
        {
            if (TriggerSyncException is not null) throw TriggerSyncException;
            ProjectNameTriggers.Add(project.Name);
        }

        public void TriggerSync(CrdtProject crdtProject)
        {
            if (TriggerSyncException is not null) throw TriggerSyncException;
            ProjectNameTriggers.Add(crdtProject.Name);
        }
    }

    private sealed class FakeSignalRConnection : ILexboxSignalRConnection
    {
        private Func<Guid, Guid?, Task>? projectUpdatedHandler;
        public HubConnectionState State { get; set; } = HubConnectionState.Disconnected;
        public Exception? StartException { get; set; }
        public Exception? SubscribeException { get; set; }
        public HashSet<Guid> SubscribeFailures { get; } = [];
        public int StartCalls { get; private set; }
        public int DisposeCalls { get; private set; }
        public List<Guid> ProjectSubscriptions { get; } = [];
        public List<Guid> ProjectSubscriptionAttempts { get; } = [];

        public event Func<Exception?, Task>? Reconnecting;
        public event Func<Exception?, Task>? Closed;
        public event Func<string?, Task>? Reconnected;

        public Task StartAsync(CancellationToken cancellationToken = default)
        {
            StartCalls++;
            if (StartException is not null) throw StartException;
            State = HubConnectionState.Connected;
            return Task.CompletedTask;
        }

        public Task ListenForProjectChanges(Guid projectId, CancellationToken cancellationToken = default)
        {
            ProjectSubscriptionAttempts.Add(projectId);
            if (SubscribeException is not null) throw SubscribeException;
            if (SubscribeFailures.Contains(projectId)) throw new InvalidOperationException("subscription failed");
            ProjectSubscriptions.Add(projectId);
            return Task.CompletedTask;
        }

        public IDisposable OnProjectUpdated(Func<Guid, Guid?, Task> handler)
        {
            projectUpdatedHandler = handler;
            return NullDisposable.Instance;
        }

        public ValueTask DisposeAsync()
        {
            DisposeCalls++;
            State = HubConnectionState.Disconnected;
            return ValueTask.CompletedTask;
        }

        public Task RaiseReconnecting(Exception? exception = null)
        {
            State = HubConnectionState.Reconnecting;
            return Reconnecting?.Invoke(exception) ?? Task.CompletedTask;
        }

        public Task RaiseClosed(Exception? exception = null)
        {
            State = HubConnectionState.Disconnected;
            return Closed?.Invoke(exception) ?? Task.CompletedTask;
        }

        public Task RaiseReconnected(string? connectionId = null)
        {
            State = HubConnectionState.Connected;
            return Reconnected?.Invoke(connectionId) ?? Task.CompletedTask;
        }

        public Task RaiseProjectUpdated(Guid projectId, Guid? clientId = null) =>
            projectUpdatedHandler?.Invoke(projectId, clientId) ?? Task.CompletedTask;
    }

    private sealed class FakeNetworkStatus : INetworkStatus
    {
        public bool IsOnline => true;
    }

    private sealed class NullDisposable : IDisposable
    {
        public static readonly NullDisposable Instance = new();
        public void Dispose()
        {
        }
    }
}
