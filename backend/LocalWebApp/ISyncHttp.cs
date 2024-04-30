using CrdtLib;
using CrdtLib.Db;
using Refit;

namespace LocalWebApp;

public interface ISyncHttp : ISyncable
{
    [Post("/api/sync/add")]
    internal Task AddRange(IEnumerable<Commit> commits);

    async Task ISyncable.AddRangeFromSync(IEnumerable<Commit> commits)
    {
        await AddRange(commits);
    }

    [Get("/api/sync/get")]
    internal new Task<SyncState> GetSyncState();

    async Task<SyncState> ISyncable.GetSyncState()
    {
        var syncState = await GetSyncState();
        ArgumentNullException.ThrowIfNull(syncState);
        return syncState;
    }

    [Post("/api/sync/changes")]
    internal new Task<ChangesResult> GetChanges(SyncState otherHeads);

    async Task<ChangesResult> ISyncable.GetChanges(SyncState otherHeads)
    {
        var changes = await GetChanges(otherHeads);
        ArgumentNullException.ThrowIfNull(changes);
        return changes;
    }

    async Task<SyncResults> ISyncable.SyncWith(ISyncable remoteModel)
    {
        return await remoteModel.SyncWith(this);
    }

    async Task ISyncable.SyncMany(ISyncable[] remotes)
    {
        throw new NotSupportedException();
    }
}
