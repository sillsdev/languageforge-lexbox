using FwLiteShared.Projects;
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
}
