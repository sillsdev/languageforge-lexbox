using CrdtLib;
using CrdtLib.Db;
using Refit;

namespace LocalWebApp;

public class CrdtHttpSync(ISyncHttp syncHttp, ILogger<CrdtHttpSync> logger) : ISyncable
{
    private bool? _isHealthy;
    private DateTimeOffset _lastHealthCheck = DateTimeOffset.MinValue;

    public async ValueTask<bool> ShouldSync()
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
            logger.LogError(e, "Failed to check health of remote server");
            _isHealthy = false;
        }
        return _isHealthy.Value;
    }

    async Task ISyncable.AddRangeFromSync(IEnumerable<Commit> commits)
    {
        await syncHttp.AddRange(commits);
    }

    async Task<ChangesResult> ISyncable.GetChanges(SyncState otherHeads)
    {
        var changes = await syncHttp.GetChanges(otherHeads);
        ArgumentNullException.ThrowIfNull(changes);
        return changes;
    }

    async Task<SyncState> ISyncable.GetSyncState()
    {
        var syncState = await syncHttp.GetSyncState();
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
    [Get("/api/healthz")]
    Task<HttpResponseMessage> HealthCheck();

    [Post("/api/sync/add")]
    internal Task AddRange(IEnumerable<Commit> commits);

    [Get("/api/sync/get")]
    internal Task<SyncState> GetSyncState();

    [Post("/api/sync/changes")]
    internal Task<ChangesResult> GetChanges(SyncState otherHeads);
}
