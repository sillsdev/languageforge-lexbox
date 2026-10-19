using SIL.Harmony.Config;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using LcmCrdt;
using SIL.Harmony;
using SIL.Harmony.Changes;
using SIL.Harmony.Core;

namespace LcmDebugger;

internal class FakeCommit : Commit
{
    [SetsRequiredMembers]
    public FakeCommit(Guid id, HybridDateTime hybridDateTime) : base(id, "", NullParentHash, hybridDateTime)
    {
        HybridDateTime = hybridDateTime;
        SetParentHash(NullParentHash);
    }
}

/// <summary>
///
/// </summary>
/// <param name="commits"></param>
/// <param name="currentSyncState">when null it will just report to the client that it has the same sync state, that way the client doesn't try to push any changes</param>
public class FakeSyncSource(Commit[] commits, SyncState? currentSyncState = null) : ISyncable
{
    public static FakeSyncSource FromSingleChangeJson(
        [StringSyntax(StringSyntaxAttribute.Json)] string json,
        DateTimeOffset commitDate,
        JsonSerializerOptions? options = null)
    {
        var change = JsonSerializer.Deserialize<ChangeEntity<IChange>>(json, options);
        ArgumentNullException.ThrowIfNull(change);
        return new FakeSyncSource([new FakeCommit(change.CommitId, new HybridDateTime(commitDate, 0))
        {
            ClientId = Guid.NewGuid(),
            ChangeEntities = [change]
        }]);
    }

    public static FakeSyncSource FromJsonFile(string path, JsonSerializerOptions? options = null)
    {
        if (options is null)
        {
            var config = new HarmonyConfig();
            LcmCrdtKernel.ConfigureCrdt(config);
            // Full external options (resolver + Harmony's IChange converter); the resolver alone can't
            // deserialize the ChangeEntity<IChange> fields in the captured sync payload.
            options = config.MakeLcmCrdtExternalJsonOptions();
        }

        using var file = File.OpenRead(path);
        var changes = JsonSerializer.Deserialize<ChangesResult<Commit>>(file, options);
        ArgumentNullException.ThrowIfNull(changes);
        ArgumentNullException.ThrowIfNull(changes.MissingFromClient);
        ArgumentNullException.ThrowIfNull(changes.ServerSyncState);
        return new FakeSyncSource(changes.MissingFromClient, changes.ServerSyncState);
    }

    public Task AddRangeFromSync(IEnumerable<Commit> commits)
    {
        return Task.CompletedTask;
    }

    public Task<SyncState> GetSyncState()
    {
        return Task.FromResult(new SyncState([]));
    }

    public Task<ChangesResult<Commit>> GetChanges(SyncState otherHeads)
    {
        return Task.FromResult(new ChangesResult<Commit>(commits, currentSyncState ?? otherHeads));
    }

    public Task<SyncResults> SyncWith(ISyncable remoteModel)
    {
        throw new NotImplementedException();
    }

    public Task SyncMany(ISyncable[] remotes)
    {
        throw new NotImplementedException();
    }

    public ValueTask<bool> ShouldSync()
    {
        return new ValueTask<bool>(true);
    }
}
