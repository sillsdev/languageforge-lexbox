using Microsoft.Extensions.Logging;
using Refit;
using SIL.Harmony;
using SIL.Harmony.Core;

namespace LcmCrdt.RemoteSync;

public class CrdtHttpSyncService(ILogger<CrdtHttpSyncService> logger, RefitSettings refitSettings)
{
    //todo replace with a IMemoryCache check
    private bool? _isHealthy;
    private DateTimeOffset _lastHealthCheck = DateTimeOffset.MinValue;

    //todo pull this out into a service wrapped around auth helpers so that any service making requests can use it
    public async ValueTask<bool> ShouldSync(ISyncHttp syncHttp)
    {
        if (_isHealthy is not null && _lastHealthCheck + TimeSpan.FromMinutes(30) > DateTimeOffset.UtcNow)
            return _isHealthy.Value;
        try
        {
            var responseMessage = await syncHttp.HealthCheck();
            _isHealthy = responseMessage.IsSuccessStatusCode;
            if (!_isHealthy.Value)
            {
                logger.LogWarning("Health check failed, response status code {StatusCode}", responseMessage.StatusCode);
            }
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

    /// <summary>
    /// Creates a Harmony sync client to represent a remote server
    /// </summary>
    /// <param name="project">project data, used to provide the projectId and clientId</param>
    /// <param name="client">should have the base url set to the remote server</param>
    /// <returns></returns>
    public async ValueTask<ISyncable> CreateProjectSyncable(ProjectData project, HttpClient client)
    {
        return new CrdtProjectSync(RestService.For<ISyncHttp>(client, refitSettings), project.Id, project.ClientId, this);
    }

    public async ValueTask<bool> TestAuth(HttpClient client)
    {
        logger.LogInformation("Testing auth, client base url: {ClientBaseUrl}", client.BaseAddress);
        var syncable = await CreateProjectSyncable(new ProjectData("test", Guid.Empty, null, Guid.Empty), client);
        return await syncable.ShouldSync();
    }
}

internal class CrdtProjectSync(ISyncHttp restSyncClient, Guid projectId, Guid clientId, CrdtHttpSyncService httpSyncService) : ISyncable
{
    public ValueTask<bool> ShouldSync()
    {
        return httpSyncService.ShouldSync(restSyncClient);
    }

    async Task ISyncable.AddRangeFromSync(IEnumerable<Commit> commits)
    {
        await restSyncClient.AddRange(projectId, commits, clientId);
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

    [Post("/api/crdt/{id}/add")]
    internal Task AddRange(Guid id, IEnumerable<Commit> commits, Guid? clientId);

    [Get("/api/crdt/{id}/get")]
    internal Task<SyncState> GetSyncState(Guid id);

    [Post("/api/crdt/{id}/changes")]
    internal Task<ChangesResult<Commit>> GetChanges(Guid id, SyncState otherHeads);
}
