using FwLiteShared.Sync;
using LcmCrdt.LocalSync;
using SIL.Harmony;

namespace FwLiteShared.LocalSync;

/// <summary>
/// The result of a local sync attempt. <see cref="SyncResults"/> is null when the identity check refused the sync.
/// </summary>
public record LocalSyncResult(IdentityComparison Comparison, SyncResults? SyncResults)
{
    public bool Synced => SyncResults?.IsSynced == true;
}

/// <summary>
/// Syncs the current project with another copy of it without Lexbox, such as a sync folder.
/// Unlike <see cref="SyncService"/> it needs no sign-in, and it never updates the Lexbox sync date, because
/// commits exchanged locally still have to reach Lexbox.
/// </summary>
public class LocalSyncService(
    DataModel dataModel,
    ProjectIdentityService projectIdentityService,
    SyncResultsProcessor syncResultsProcessor)
{
    /// <param name="remote">The other copy.</param>
    /// <param name="remoteIdentity">The other copy's identity, as it reported it (for a sync folder, its manifest).</param>
    public async Task<LocalSyncResult> SyncWith(ISyncable remote, ProjectIdentity remoteIdentity)
    {
        var comparison = ProjectIdentity.Compare(await projectIdentityService.GetIdentity(), remoteIdentity);
        if (comparison != IdentityComparison.Match) return new LocalSyncResult(comparison, null);

        var syncResults = await dataModel.SyncWith(remote);
        if (syncResults.IsSynced)
        {
            await syncResultsProcessor.ApplyCommentReadStatus(syncResults);
            await syncResultsProcessor.SendNotifications(syncResults);
        }
        return new LocalSyncResult(comparison, syncResults);
    }

    /// <summary>
    /// Fills a project created moments ago for this purpose (it has no commits yet) from another copy. There's no
    /// identity check because there's nothing local to protect; a project with any commits is refused.
    /// </summary>
    public async Task<SyncResults> Download(ISyncable remote)
    {
        if ((await projectIdentityService.GetIdentity()).RootCommitId is not null)
            throw new InvalidOperationException("Can only download into a project that has no commits yet");
        var syncResults = await dataModel.SyncWith(remote);
        if (syncResults.IsSynced)
        {
            await syncResultsProcessor.ApplyCommentReadStatus(syncResults);
            await syncResultsProcessor.SendNotifications(syncResults);
        }
        return syncResults;
    }
}
