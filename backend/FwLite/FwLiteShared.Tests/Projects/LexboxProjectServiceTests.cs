using FwLiteShared.Auth;
using FwLiteShared.Projects;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;

namespace FwLiteShared.Tests.Projects;

public class LexboxProjectServiceTests
{
    private const string CacheKey = "test-cache-key";
    private readonly MemoryCache _cache = new(new MemoryCacheOptions());

    [Fact]
    public async Task StartLexboxProjectChangeListener_ReusesCachedConnection_WithoutConsultingAuth()
    {
        // Guards the false-logout teardown: an auth check ahead of the cache lookup tore down a healthy,
        // auto-reconnecting connection whenever GetCurrentToken was transiently null (expired token + flaky
        // network) while the user was still signed in. Reuse must be independent of auth — encoded here by a
        // null clientFactory, so any auth access sneaking ahead of the cache lookup throws and fails the test.
        var server = new LexboxServer(new Uri("https://example.test/"), "Test");
        var cacheKey = LexboxProjectService.HubConnectionCacheKey(server);
        await using var connection = new HubConnectionBuilder().WithUrl("http://localhost/test").Build();
        _cache.Set(cacheKey, connection);
        var service = new LexboxProjectService(
            clientFactory: null!,
            logger: NullLogger<LexboxProjectService>.Instance,
            loggerFactory: null!,
            httpMessageHandlerFactory: null!,
            backgroundSyncService: null!,
            options: null!,
            cache: _cache,
            networkStatus: null!);

        var result = await service.StartLexboxProjectChangeListener(server, CancellationToken.None);

        result.Should().BeSameAs(connection);
        _cache.TryGetValue(cacheKey, out HubConnection? cached).Should().BeTrue("the connection must not be evicted");
        cached.Should().BeSameAs(connection);
    }

    [Fact]
    public async Task HandleReconnecting_WhenSignedIn_KeepsCacheAndDoesNotStop()
    {
        var connection = new object();
        _cache.Set(CacheKey, connection);
        var stopCalled = false;

        await LexboxProjectService.HandleReconnecting(
            CacheKey,
            _cache,
            NullLogger.Instance,
            connection,
            () => { stopCalled = true; return Task.CompletedTask; },
            isSignedIn: true,
            exception: null);

        _cache.TryGetValue(CacheKey, out _).Should().BeTrue();
        stopCalled.Should().BeFalse();
    }

    [Fact]
    public async Task HandleReconnecting_WhenLoggedOut_EvictsCacheAndStopsConnection()
    {
        var connection = new object();
        _cache.Set(CacheKey, connection);
        var stopCalled = false;

        await LexboxProjectService.HandleReconnecting(
            CacheKey,
            _cache,
            NullLogger.Instance,
            connection,
            () => { stopCalled = true; return Task.CompletedTask; },
            isSignedIn: false,
            exception: null);

        _cache.TryGetValue(CacheKey, out _).Should().BeFalse();
        stopCalled.Should().BeTrue();
    }

    [Fact]
    public async Task HandleReconnecting_WhenLoggedOut_DoesNotEvictAReplacementConnection()
    {
        // A replacement can be cached between this connection entering Reconnecting and the handler running;
        // evicting it would orphan a live connection. The handler must still stop ITSELF (it's logged out).
        var replacement = new object();
        _cache.Set(CacheKey, replacement);
        var stopCalled = false;

        await LexboxProjectService.HandleReconnecting(
            CacheKey,
            _cache,
            NullLogger.Instance,
            connection: new object(),
            () => { stopCalled = true; return Task.CompletedTask; },
            isSignedIn: false,
            exception: null);

        _cache.TryGetValue(CacheKey, out object? cached).Should().BeTrue();
        cached.Should().BeSameAs(replacement);
        stopCalled.Should().BeTrue();
    }

    [Fact]
    public async Task EvictAndStopIfCached_WithNoCachedEntry_IsNoOp()
    {
        var act = () => LexboxProjectService.EvictAndStopIfCached(CacheKey, _cache, NullLogger.Instance);

        await act.Should().NotThrowAsync();
        _cache.TryGetValue(CacheKey, out _).Should().BeFalse();
    }

    [Fact]
    public async Task EvictAndStopIfCached_WithCachedConnection_RemovesFromCache()
    {
        // Build an un-started HubConnection. StopAsync on an un-started connection is a no-op and
        // doesn't reach the network, so this stays a true unit test.
        await using var connection = new HubConnectionBuilder().WithUrl("http://localhost/test").Build();
        _cache.Set(CacheKey, connection);

        await LexboxProjectService.EvictAndStopIfCached(CacheKey, _cache, NullLogger.Instance);

        _cache.TryGetValue(CacheKey, out _).Should().BeFalse();
    }

    [Fact]
    public async Task EvictAndStopIfCached_WithMismatchedExpectedConnection_LeavesCacheAlone()
    {
        await using var cached = new HubConnectionBuilder().WithUrl("http://localhost/test").Build();
        await using var stale = new HubConnectionBuilder().WithUrl("http://localhost/test").Build();
        _cache.Set(CacheKey, cached);

        await LexboxProjectService.EvictAndStopIfCached(CacheKey, _cache, NullLogger.Instance, expectedConnection: stale);

        _cache.TryGetValue(CacheKey, out HubConnection? remaining).Should().BeTrue();
        remaining.Should().BeSameAs(cached);
    }

    [Fact]
    public async Task EvictAndStopIfCached_WithMatchingExpectedConnection_Evicts()
    {
        await using var connection = new HubConnectionBuilder().WithUrl("http://localhost/test").Build();
        _cache.Set(CacheKey, connection);

        await LexboxProjectService.EvictAndStopIfCached(CacheKey, _cache, NullLogger.Instance, expectedConnection: connection);

        _cache.TryGetValue(CacheKey, out _).Should().BeFalse();
    }

    [Fact]
    public async Task RegisterForResubscribe_RecordsProjectId()
    {
        await using var connection = new HubConnectionBuilder().WithUrl("http://localhost/test").Build();
        var projectId = Guid.NewGuid();

        var subscribed = LexboxProjectService.RegisterForResubscribe(connection, projectId);

        subscribed.Should().Contain(projectId);
    }

    [Fact]
    public async Task RegisterForResubscribe_IsUnconditionalAndAccumulatesAcrossCalls()
    {
        // The subscription-ordering fix relies on registration being independent of connection state, so a
        // project subscribed during a reconnect window still survives to be resubscribed on Reconnected.
        await using var connection = new HubConnectionBuilder().WithUrl("http://localhost/test").Build();
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();

        LexboxProjectService.RegisterForResubscribe(connection, first);
        var subscribed = LexboxProjectService.RegisterForResubscribe(connection, second);

        subscribed.Should().Contain(first);
        subscribed.Should().Contain(second);
    }

    [Fact]
    public async Task IsRegisteredForResubscribe_ReflectsRegistration()
    {
        await using var connection = new HubConnectionBuilder().WithUrl("http://localhost/test").Build();
        var registered = Guid.NewGuid();
        var unregistered = Guid.NewGuid();

        LexboxProjectService.RegisterForResubscribe(connection, registered);

        LexboxProjectService.IsRegisteredForResubscribe(connection, registered).Should().BeTrue();
        LexboxProjectService.IsRegisteredForResubscribe(connection, unregistered).Should().BeFalse();
    }

    [Fact]
    public async Task IsRegisteredForResubscribe_WithUnknownConnection_IsFalse()
    {
        await using var connection = new HubConnectionBuilder().WithUrl("http://localhost/test").Build();

        LexboxProjectService.IsRegisteredForResubscribe(connection, Guid.NewGuid()).Should().BeFalse();
    }

    [Fact]
    public async Task ResubscribeRegisteredProjects_WithNoRegistrations_IsNoOp()
    {
        await using var connection = new HubConnectionBuilder().WithUrl("http://localhost/test").Build();

        var act = () => LexboxProjectService.ResubscribeRegisteredProjects(connection);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ResubscribeRegisteredProjects_SwallowsPerProjectSendFailures()
    {
        // SendAsync on an un-started connection throws for every project; none of those failures may escape,
        // since the revive path calls this where a throw would fail the caller (e.g. project-open).
        await using var connection = new HubConnectionBuilder().WithUrl("http://localhost/test").Build();
        LexboxProjectService.RegisterForResubscribe(connection, Guid.NewGuid());
        LexboxProjectService.RegisterForResubscribe(connection, Guid.NewGuid());

        var act = () => LexboxProjectService.ResubscribeRegisteredProjects(connection);

        await act.Should().NotThrowAsync();
    }
}
