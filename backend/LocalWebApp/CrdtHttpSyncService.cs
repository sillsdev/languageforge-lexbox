using Crdt.Core;
using Crdt;
using Crdt.Db;
using LcmCrdt;
using Refit;

namespace LocalWebApp;

public class CrdtHttpSyncService(IHttpClientFactory clientFactory, ILogger<CrdtHttpSyncService> logger, RefitSettings refitSettings)
{
    //todo replace with a IMemoryCache check
    private bool? _isHealthy;
    private DateTimeOffset _lastHealthCheck = DateTimeOffset.MinValue;

    public async ValueTask<bool> ShouldSync(ISyncHttp syncHttp)
    {
        if (_isHealthy is not null && _lastHealthCheck + TimeSpan.FromMinutes(30) > DateTimeOffset.UtcNow)
            return _isHealthy.Value;
        try
        {
            var responseMessage = await syncHttp.HealthCheck();
            _isHealthy = responseMessage.IsSuccessStatusCode;
            _lastHealthCheck = responseMessage.Headers.Date ?? DateTimeOffset.UtcNow;
        }
        catch (HttpRequestException e)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug(e, "Failed to check health of remote server");
            }
            else
            {
                logger.LogWarning("Failed to check health of remote server");
            }

            _isHealthy = false;
            _lastHealthCheck = DateTimeOffset.UtcNow;
        }

        return _isHealthy.Value;
    }

    public ISyncHttp CreateSyncHttp(string originDomain)
    {
        var uri = new Uri(originDomain);
        var httpClient = clientFactory.CreateClient(uri.Host);
        httpClient.BaseAddress = uri;
        return RestService.For<ISyncHttp>(httpClient, refitSettings);
    }

    public ISyncable CreateProjectSyncable(ProjectData project)
    {
        if (string.IsNullOrEmpty(project.OriginDomain)) return NullSyncable.Instance;
        return new CrdtProjectSync(CreateSyncHttp(project.OriginDomain), project.Id, project.OriginDomain, this);
    }
}

public class CrdtProjectSync(ISyncHttp restSyncClient, Guid projectId, string originDomain, CrdtHttpSyncService httpSyncService) : ISyncable
{
    public ValueTask<bool> ShouldSync()
    {
        return httpSyncService.ShouldSync(restSyncClient);
    }

    async Task ISyncable.AddRangeFromSync(IEnumerable<Commit> commits)
    {
        await restSyncClient.AddRange(projectId, commits);
    }

    async Task<ChangesResult<Commit>> ISyncable.GetChanges(SyncState otherHeads)
    {
        var changes = await restSyncClient.GetChanges(projectId, otherHeads);
        ArgumentNullException.ThrowIfNull(changes);
        return changes;
    }

    async Task<SyncState> ISyncable.GetSyncState()
    {
        var syncState = await restSyncClient.GetSyncState(projectId);
        ArgumentNullException.ThrowIfNull(syncState);
        return syncState;
    }

    async Task<SyncResults> ISyncable.SyncWith(ISyncable remoteModel)
    {
        return await remoteModel.SyncWith(this);
    }

    Task ISyncable.SyncMany(ISyncable[] remotes)
    {
        throw new NotSupportedException();
    }
}

public interface ISyncHttp
{
    [Get("/api/AuthTesting/requires-auth")]
    Task<HttpResponseMessage> HealthCheck();

    [Post("/api/sync/{id}/add")]
    internal Task AddRange(Guid id, IEnumerable<Commit> commits);

    [Get("/api/sync/{id}/get")]
    internal Task<SyncState> GetSyncState(Guid id);

    [Post("/api/sync/{id}/changes")]
    internal Task<ChangesResult<Commit>> GetChanges(Guid id, SyncState otherHeads);
}
