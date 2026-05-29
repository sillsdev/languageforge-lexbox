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
            currentToken: "valid-token",
            exception: null);

        _cache.TryGetValue(CacheKey, out _).Should().BeTrue();
        stopCalled.Should().BeFalse();
    }

    [Fact]
    public async Task HandleReconnecting_WithNullToken_EvictsCacheAndStopsConnection()
    {
        _cache.Set(CacheKey, "cached-connection");
        var stopCalled = false;

        await LexboxProjectService.HandleReconnecting(
            CacheKey,
            _cache,
            NullLogger.Instance,
            () => { stopCalled = true; return Task.CompletedTask; },
            currentToken: null,
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
        var connection = new HubConnectionBuilder().WithUrl("http://localhost/test").Build();
        _cache.Set(CacheKey, connection);

        await LexboxProjectService.EvictAndStopIfCached(CacheKey, _cache, NullLogger.Instance);

        _cache.TryGetValue(CacheKey, out _).Should().BeFalse();
    }
}
