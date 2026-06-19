using FwLiteShared.Auth;
using FwLiteShared.Projects;
using FwLiteShared.Services;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using MiniLcm.Push;

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
        stale.DisposeCalls.Should().Be(2);
        replacement.DisposeCalls.Should().Be(0);
    }

    private static LexboxHubConnection CreateHubConnection(
        FakeSignalRConnectionFactory factory,
        FakeHubConnectionAuth? auth = null,
        FakeSignalRConnection? initialConnection = null)
    {
        return new LexboxHubConnection(
            Server,
            NullLoggerFactory.Instance,
            httpMessageHandlerFactory: null!,
            clientFactory: null!,
            backgroundSyncService: null!,
            new FakeNetworkStatus(),
            new MemoryCache(new MemoryCacheOptions()),
            NullLogger.Instance,
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

    private sealed class FakeSignalRConnection : ILexboxSignalRConnection
    {
        private Func<Guid, Guid?, Task>? projectUpdatedHandler;
        public HubConnectionState State { get; set; } = HubConnectionState.Disconnected;
        public Exception? StartException { get; init; }
        public int DisposeCalls { get; private set; }
        public List<Guid> ProjectSubscriptions { get; } = [];

        public event Func<Exception?, Task>? Reconnecting;
        public event Func<Exception?, Task>? Closed;
        public event Func<string?, Task>? Reconnected;

        public Task StartAsync(CancellationToken cancellationToken = default)
        {
            if (StartException is not null) throw StartException;
            State = HubConnectionState.Connected;
            return Task.CompletedTask;
        }

        public Task SendAsync(string methodName, Guid projectId, CancellationToken cancellationToken = default)
        {
            methodName.Should().Be(nameof(IProjectChangeHubServer.ListenForProjectChanges));
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
