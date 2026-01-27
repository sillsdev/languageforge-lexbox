using LcmCrdt.Utils;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Refit;
using SIL.Harmony;
using SIL.Harmony.Core;

namespace LcmCrdt.RemoteSync;

public class CrdtHttpSyncService(ILogger<CrdtHttpSyncService> logger, IRefitHttpServiceFactory refitFactory, IMemoryCache cache)
{
    private static readonly TimeSpan HealthyCacheTime = TimeSpan.FromMinutes(30);
    private bool? CachedIsHealthy(string authority)
    {
        return cache.Get<bool?>("ServerHealth|" + authority);
    }

    private void SetCachedIsHealthy(string authority, bool healthy)
    {
        cache.Set("ServerHealth|" + authority, healthy, DateTimeOffset.UtcNow + HealthyCacheTime);
    }


    //todo pull this out into a service wrapped around auth helpers so that any service making requests can use it
    internal async ValueTask<bool> ShouldSync(ISyncHttp syncHttp, string authority)
    {
        var isHealthy = CachedIsHealthy(authority);
        if (isHealthy is not null)
            return isHealthy.Value;
        try
        {
            var responseMessage = await syncHttp.HealthCheck();
            isHealthy = responseMessage.IsSuccessStatusCode;
            if (!isHealthy.Value)
            {
                logger.LogWarning("Health check failed for {Authority}, response status code {StatusCode}", authority, responseMessage.StatusCode);
            }
        }
        catch (HttpRequestException e)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug(e, "Failed to check health of remote server: {Authority}", authority);
            }
            else
            {
                logger.LogWarning("Failed to check health of remote server: {Authority}", authority);
            }

            isHealthy = false;
        }
        SetCachedIsHealthy(authority, isHealthy.Value);
        return isHealthy.Value;
    }

    /// <summary>
    /// Creates a Harmony sync client to represent a remote server
    /// </summary>
    /// <param name="project">project data, used to provide the projectId and clientId</param>
    /// <param name="client">should have the base url set to the remote server</param>
    /// <returns></returns>
    public ValueTask<ISyncable> CreateProjectSyncable(ProjectData project, HttpClient client)
    {
        var syncHttp = refitFactory.Service<ISyncHttp>(client);
        var crdtProjectSync = new CrdtProjectSync(syncHttp, project.Id, project.ClientId, this, client.BaseAddress?.Authority ?? string.Empty);
        return ValueTask.FromResult<ISyncable>(crdtProjectSync);
    }

    public virtual async ValueTask<bool> TestAuth(HttpClient client)
    {
        logger.LogInformation("Testing auth, client base url: {ClientBaseUrl}", client.BaseAddress);
        var syncable = await CreateProjectSyncable(new ProjectData("test", "test", Guid.Empty, null, Guid.Empty), client);
        return await syncable.ShouldSync();
    }
}

internal class CrdtProjectSync(ISyncHttp restSyncClient, Guid projectId, Guid clientId, CrdtHttpSyncService httpSyncService, string authority) : ISyncable
{
    public ValueTask<bool> ShouldSync()
    {
        return httpSyncService.ShouldSync(restSyncClient, authority);
    }

    async Task ISyncable.AddRangeFromSync(IEnumerable<Commit> commits)
    {
        await restSyncClient.AddRange(projectId, commits, clientId);
    }

    async Task<ChangesResult<Commit>> ISyncable.GetChanges(SyncState otherHeads)
    {
        var changesResponse = await restSyncClient.GetChanges(projectId, otherHeads);
        if (changesResponse.Error is not null)
        {
            // The inner exception is almost certainly more interesting than the wrapping ApiException
            // (e.g. a JsonException if trying to download a change object that is too new for this version of FieldWorks Lite)
            var error = changesResponse.Error?.InnerException ?? changesResponse.Error!;
            throw new CrdtSyncException("FieldWorks Lite is likely out of date. Failed to download dictionary changes.",
                CrdtSyncException.CrdtSyncStep.Download, error);
        }

        var changes = changesResponse.Content;
        ArgumentNullException.ThrowIfNull(changes);
        foreach (var commit in changes.MissingFromClient)
        {
            //ignore the sync date from the server, we only care about our sync date. Setting this to the correct value is handled in SyncService.UpdateSyncDate
            commit.SetSyncDate(null);
        }
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
    [Get("/api/crdt/checkConnection")]
    Task<HttpResponseMessage> HealthCheck();

    [Post("/api/crdt/{id}/add")]
    internal Task AddRange(Guid id, IEnumerable<Commit> commits, Guid? clientId);

    [Get("/api/crdt/{id}/get")]
    internal Task<SyncState> GetSyncState(Guid id);

    [Post("/api/crdt/{id}/changes")]
    internal Task<ApiResponse<ChangesResult<Commit>>> GetChanges(Guid id, SyncState otherHeads);
}
