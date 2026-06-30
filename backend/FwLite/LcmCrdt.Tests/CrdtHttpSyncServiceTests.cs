using System.Net;
using LcmCrdt.RemoteSync;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Refit;
using SIL.Harmony.Core;

namespace LcmCrdt.Tests;

public class CrdtHttpSyncServiceTests
{
    private const string Authority = "lexbox.org";

    // Only HealthCheck is exercised by ShouldSync; the sync methods are never called here.
    private sealed class StubSyncHttp(Func<HttpStatusCode> status) : ISyncHttp
    {
        public int HealthCheckCalls { get; private set; }

        public Task<HttpResponseMessage> HealthCheck()
        {
            HealthCheckCalls++;
            return Task.FromResult(new HttpResponseMessage(status()));
        }

        Task ISyncHttp.AddRange(Guid id, IEnumerable<Commit> commits, Guid? clientId) => throw new NotSupportedException();
        Task<SyncState> ISyncHttp.GetSyncState(Guid id) => throw new NotSupportedException();
        Task<ApiResponse<ChangesResult<Commit>>> ISyncHttp.GetChanges(Guid id, SyncState otherHeads) => throw new NotSupportedException();
    }

    private static CrdtHttpSyncService NewService(IMemoryCache cache) =>
        new(NullLogger<CrdtHttpSyncService>.Instance, refitFactory: null!, cache);

    [Fact]
    public async Task FailedHealthCheck_IsCached_UntilInvalidated()
    {
        var serverHealthy = false;
        var http = new StubSyncHttp(() => serverHealthy ? HttpStatusCode.OK : HttpStatusCode.ServiceUnavailable);
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var service = NewService(cache);

        (await service.ShouldSync(http, Authority)).Should().BeFalse();
        // The server recovers, but the failed verdict is cached, so a second call must not re-probe.
        serverHealthy = true;
        (await service.ShouldSync(http, Authority)).Should().BeFalse("a failed health check is cached");
        http.HealthCheckCalls.Should().Be(1, "the cached verdict short-circuits the probe");

        // A recovery signal drops the stale verdict so the next sync re-probes instead of being locked out.
        CrdtHttpSyncService.InvalidateServerHealth(cache, Authority);
        (await service.ShouldSync(http, Authority)).Should().BeTrue();
        http.HealthCheckCalls.Should().Be(2);
    }
}
