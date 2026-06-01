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
    public async Task HandleReconnecting_WithValidToken_KeepsCacheAndDoesNotStop()
    {
        _cache.Set(CacheKey, "cached-connection");
        var stopCalled = false;

        await LexboxProjectService.HandleReconnecting(
            CacheKey,
            _cache,
            NullLogger.Instance,
            () => { stopCalled = true; return Task.CompletedTask; },
            hasValidToken: true,
            exception: null);

        _cache.TryGetValue(CacheKey, out _).Should().BeTrue();
        stopCalled.Should().BeFalse();
    }

    [Fact]
    public async Task HandleReconnecting_WithNoToken_EvictsCacheAndStopsConnection()
    {
        _cache.Set(CacheKey, "cached-connection");
        var stopCalled = false;

        await LexboxProjectService.HandleReconnecting(
            CacheKey,
            _cache,
            NullLogger.Instance,
            () => { stopCalled = true; return Task.CompletedTask; },
            hasValidToken: false,
            exception: null);

        _cache.TryGetValue(CacheKey, out _).Should().BeFalse();
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
}
