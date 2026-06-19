using FwLiteShared.Auth;
using FwLiteShared.Projects;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;

namespace FwLiteShared.Tests.Projects;

public class LexboxHubConnectionTests
{
    private readonly MemoryCache _cache = new(new MemoryCacheOptions());
    private readonly LexboxHubConnectionRegistry _registry;
    private readonly LexboxProjectSubscriptionStarter _subscriptionStarter;

    public LexboxHubConnectionTests()
    {
        _registry = new LexboxHubConnectionRegistry(_cache, NullLogger<LexboxHubConnectionRegistry>.Instance);
        _subscriptionStarter = new LexboxProjectSubscriptionStarter(
            _registry,
            NullLogger<LexboxProjectSubscriptionStarter>.Instance);
    }

    [Fact]
    public async Task StartLexboxProjectChangeListener_ReusesCachedConnection_WithoutConsultingAuth()
    {
        // Guards the false-logout teardown: an auth check ahead of the cache lookup tore down a healthy,
        // auto-reconnecting connection whenever GetCurrentToken was transiently null (expired token + flaky
        // network) while the user was still signed in. Reuse must be independent of auth — encoded here by a
        // null clientFactory, so any auth access sneaking ahead of the cache lookup throws and fails the test.
        var server = new LexboxServer(new Uri("https://example.test/"), "Test");
        await using var hubConnection = new HubConnectionBuilder().WithUrl("http://localhost/test").Build();
        await using var connection = TestConnection(server, hubConnection);
        _registry.Set(server, connection);
        var listener = new LexboxProjectChangeListener(
            clientFactory: null!,
            logger: NullLogger<LexboxProjectChangeListener>.Instance,
            loggerFactory: null!,
            httpMessageHandlerFactory: null!,
            backgroundSyncService: null!,
            options: null!,
            cache: _cache,
            connectionRegistry: _registry,
            subscriptionStarter: _subscriptionStarter,
            networkStatus: null!);

        var result = await listener.StartLexboxProjectChangeListener(server, CancellationToken.None);

        result.Should().BeSameAs(connection);
        _registry.Get(server).Should().BeSameAs(connection, because: "the connection must not be evicted");
    }

    [Fact]
    public void Get_WhenNotCached_ReturnsNull()
    {
        var server = new LexboxServer(new Uri("https://example.test/"), "Test");

        _registry.Get(server).Should().BeNull();
    }

    [Fact]
    public async Task HandleReconnecting_WhenSignedIn_KeepsCacheAndDoesNotStop()
    {
        var server = new LexboxServer(new Uri("https://example.test/"), "Test");
        await using var hubConnection = new HubConnectionBuilder().WithUrl("http://localhost/test").Build();
        await using var connection = TestConnection(server, hubConnection);
        _registry.Set(server, connection);
        var stopCalled = false;

        await connection.HandleReconnecting(
            isSignedIn: true,
            exception: null,
            stopConnection: () => { stopCalled = true; return Task.CompletedTask; });

        _registry.Get(server).Should().BeSameAs(connection);
        stopCalled.Should().BeFalse();
    }

    [Fact]
    public async Task HandleReconnecting_WhenLoggedOut_EvictsCacheAndStopsConnection()
    {
        var server = new LexboxServer(new Uri("https://example.test/"), "Test");
        await using var hubConnection = new HubConnectionBuilder().WithUrl("http://localhost/test").Build();
        await using var connection = TestConnection(server, hubConnection);
        _registry.Set(server, connection);
        var stopCalled = false;

        await connection.HandleReconnecting(
            isSignedIn: false,
            exception: null,
            stopConnection: () => { stopCalled = true; return Task.CompletedTask; });

        _registry.Get(server).Should().BeNull();
        stopCalled.Should().BeTrue();
    }

    [Fact]
    public async Task HandleReconnecting_WhenLoggedOut_DoesNotEvictAReplacementConnection()
    {
        // A replacement can be cached between this connection entering Reconnecting and the handler running;
        // evicting it would orphan a live connection. The handler must still stop ITSELF (it's logged out).
        var server = new LexboxServer(new Uri("https://example.test/"), "Test");
        await using var replacementHubConnection = new HubConnectionBuilder().WithUrl("http://localhost/test").Build();
        await using var staleHubConnection = new HubConnectionBuilder().WithUrl("http://localhost/test").Build();
        await using var replacement = TestConnection(server, replacementHubConnection);
        await using var stale = TestConnection(server, staleHubConnection);
        _registry.Set(server, replacement);
        var stopCalled = false;

        await stale.HandleReconnecting(
            isSignedIn: false,
            exception: null,
            stopConnection: () => { stopCalled = true; return Task.CompletedTask; });

        _registry.Get(server).Should().BeSameAs(replacement);
        stopCalled.Should().BeTrue();
    }

    [Fact]
    public async Task EvictAndStopIfCached_WithNoCachedEntry_IsNoOp()
    {
        var server = new LexboxServer(new Uri("https://example.test/"), "Test");
        var act = () => _registry.EvictAndStopIfCached(server);

        await act.Should().NotThrowAsync();
        _registry.Get(server).Should().BeNull();
    }

    [Fact]
    public async Task EvictAndStopIfCached_WithCachedConnection_RemovesFromCache()
    {
        // Build an un-started HubConnection. StopAsync on an un-started connection is a no-op and
        // doesn't reach the network, so this stays a true unit test.
        var server = new LexboxServer(new Uri("https://example.test/"), "Test");
        await using var hubConnection = new HubConnectionBuilder().WithUrl("http://localhost/test").Build();
        await using var connection = TestConnection(server, hubConnection);
        _registry.Set(server, connection);

        await _registry.EvictAndStopIfCached(server);

        _registry.Get(server).Should().BeNull();
    }

    [Fact]
    public async Task EvictAndStopIfCached_WithMismatchedExpectedConnection_LeavesCacheAlone()
    {
        var server = new LexboxServer(new Uri("https://example.test/"), "Test");
        await using var cachedHubConnection = new HubConnectionBuilder().WithUrl("http://localhost/test").Build();
        await using var staleHubConnection = new HubConnectionBuilder().WithUrl("http://localhost/test").Build();
        await using var cached = TestConnection(server, cachedHubConnection);
        await using var stale = TestConnection(server, staleHubConnection);
        _registry.Set(server, cached);

        await _registry.EvictAndStopIfCached(server, expectedConnection: stale);

        _registry.Get(server).Should().BeSameAs(cached);
    }

    [Fact]
    public async Task EvictAndStopIfCached_WithMatchingExpectedConnection_Evicts()
    {
        var server = new LexboxServer(new Uri("https://example.test/"), "Test");
        await using var hubConnection = new HubConnectionBuilder().WithUrl("http://localhost/test").Build();
        await using var connection = TestConnection(server, hubConnection);
        _registry.Set(server, connection);

        await _registry.EvictAndStopIfCached(server, expectedConnection: connection);

        _registry.Get(server).Should().BeNull();
    }

    [Fact]
    public async Task RegisterForResubscribe_IsUnconditionalAndAccumulatesAcrossCalls()
    {
        // The subscription-ordering fix relies on registration being independent of connection state, so a
        // project subscribed during a reconnect window still survives to be resubscribed on Reconnected.
        await using var hubConnection = new HubConnectionBuilder().WithUrl("http://localhost/test").Build();
        await using var connection = TestConnection(hubConnection);
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();

        connection.RegisterForResubscribe(first);
        connection.RegisterForResubscribe(second);

        connection.IsRegisteredForResubscribe(first).Should().BeTrue();
        connection.IsRegisteredForResubscribe(second).Should().BeTrue();
    }

    private LexboxHubConnection TestConnection(HubConnection hubConnection) =>
        TestConnection(new LexboxServer(new Uri("https://example.test/"), "Test"), hubConnection);

    private LexboxHubConnection TestConnection(LexboxServer server, HubConnection hubConnection) =>
        new(
            server,
            hubConnection,
            _registry,
            backgroundSyncService: null!,
            isSignedIn: () => Task.FromResult(true),
            onConnected: _ => { },
            logger: NullLogger.Instance);
}
