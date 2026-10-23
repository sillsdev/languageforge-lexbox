using FwLiteShared.Services;
using LcmCrdt;
using LcmCrdt.LocalSync;
using LcmCrdt.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SIL.Harmony;
using SIL.Harmony.Config;
using SIL.Harmony.Core;

namespace FwLiteShared.LocalSync;

/// <param name="FolderState">What the folder held before the sync; for another project, its manifest (to offer a switch).</param>
/// <param name="Sync">Null when the folder wasn't a sync folder at all.</param>
/// <param name="MediaSent">Media files copied to the folder.</param>
/// <param name="MediaReceived">Media files copied from the folder.</param>
/// <param name="MediaFailed">Media files that couldn't be copied either way; they're retried on the next sync.</param>
public record FolderSyncResult(SyncFolderState FolderState, LocalSyncResult? Sync, int MediaSent = 0, int MediaReceived = 0, int MediaFailed = 0)
{
    public bool Synced => Sync?.Synced == true;
}

/// <summary>
/// Syncs the current project with a sync folder (e.g. on a USB drive). The folder's commit log is Harmony's
/// <see cref="JsonSyncable"/>, so syncing is two-way: the folder accumulates every copy's changes over many trips.
/// </summary>
public class FolderSyncService(
    CurrentProjectService currentProjectService,
    ProjectIdentityService projectIdentityService,
    LocalSyncService localSyncService,
    MediaCopier mediaCopier,
    IPreferencesService preferences,
    IOptions<HarmonyConfig> harmonyConfig,
    ILogger<FolderSyncService> logger)
{
    public async Task<FolderSyncResult> SyncWithFolder(string path)
    {
        path = Path.GetFullPath(path);
        var state = SyncFolder.Inspect(path);
        if (state is SyncFolderState.NotASyncFolder or SyncFolderState.NewerFormat) return new FolderSyncResult(state, null);

        var localIdentity = await projectIdentityService.GetIdentity();
        if (state is SyncFolderState.Empty)
        {
            // claim the folder before writing commits, so an export interrupted part way can be resumed
            WriteManifest(path, localIdentity);
        }

        var folder = OpenFolder(path);
        var folderIdentity = state is SyncFolderState.HoldsProject { Manifest: var manifest }
            ? new ProjectIdentity(manifest.ProjectId,
                manifest.ProjectId == localIdentity.ProjectId ? await FolderLineage(folder, localIdentity) : null,
                manifest.WritingSystemIds.ToHashSet())
            : localIdentity;

        var result = await localSyncService.SyncWith(folder, folderIdentity);
        if (result.Synced)
        {
            // after the commits, so this device knows every resource the folder's files belong to; copy everything
            // both ways because the drive leaves, so a file left on it can't be fetched later
            var media = new FolderMediaStore(path);
            var sent = await mediaCopier.Push(media);
            var received = await mediaCopier.PullAll(media);
            WriteManifest(path, await projectIdentityService.GetIdentity());
            preferences.Set(RememberedFolderKey(localIdentity.ProjectId), path);
            return new FolderSyncResult(state, result, sent.Copied, received.Copied, sent.Failed + received.Failed);
        }
        return new FolderSyncResult(state, result);
    }

    /// <summary>
    /// The commits actually in the folder decide its history, not what the manifest claims. The folder shares our
    /// history if it has no commits yet, or holds our root commit, or its earliest commit is one we already have
    /// (an export of ours that was interrupted before the root commit was written). Returns our root commit when
    /// the history is shared, otherwise the folder's own earliest commit, which then fails the identity check.
    /// </summary>
    private async Task<Guid?> FolderLineage(ISyncable folder, ProjectIdentity localIdentity)
    {
        var folderCommits = (await folder.GetChanges(new SyncState([]))).MissingFromClient;
        var earliest = folderCommits.DefaultOrder().FirstOrDefault();
        if (earliest is null
            || folderCommits.Any(c => c.Id == localIdentity.RootCommitId)
            || await projectIdentityService.HasCommit(earliest.Id))
            return localIdentity.RootCommitId;
        return earliest.Id;
    }

    private void WriteManifest(string path, ProjectIdentity identity)
    {
        var project = currentProjectService.ProjectData;
        SyncFolder.WriteManifest(path, new SyncFolderManifest(project.Id, project.Code, project.Name,
            project.OriginDomain, [.. identity.WritingSystemIds]));
    }

    /// <summary>
    /// Fills the current project, created moments ago from the folder's manifest, with the folder's commits.
    /// </summary>
    public async Task<SyncResults> DownloadFromFolder(string path)
    {
        path = Path.GetFullPath(path);
        var syncResults = await localSyncService.Download(OpenFolder(path));
        if (syncResults.IsSynced)
        {
            await mediaCopier.PullAll(new FolderMediaStore(path));
            preferences.Set(RememberedFolderKey(currentProjectService.ProjectData.Id), path);
        }
        return syncResults;
    }

    private FolderSyncable OpenFolder(string path) =>
        new(new JsonSyncable(new DirectoryInfo(path), harmonyConfig.Value.JsonSerializerOptions, logger));

    /// <summary>The last folder this project synced with on this device, for a one-click "sync again".</summary>
    public string? GetRememberedFolder() => preferences.Get(RememberedFolderKey(currentProjectService.ProjectData.Id));

    private static string RememberedFolderKey(Guid projectId) => $"syncFolder.{projectId}";

    /// <summary>
    /// The folder's commit log, with each commit's Lexbox sync date cleared on the way in: the date records when
    /// another device uploaded it, and keeping it would make this device report a Lexbox sync it never did
    /// (the same as <c>CrdtProjectSync.GetChanges</c> does for commits from a server).
    /// </summary>
    private class FolderSyncable(JsonSyncable folder) : ISyncable
    {
        public async Task<ChangesResult<Commit>> GetChanges(SyncState otherHeads)
        {
            var changes = await folder.GetChanges(otherHeads);
            foreach (var commit in changes.MissingFromClient) commit.SetSyncDate(null);
            return changes;
        }

        public async Task AddRangeFromSync(IEnumerable<Commit> commits) => await folder.AddRangeFromSync(commits);
        public async Task<SyncState> GetSyncState() => await folder.GetSyncState();
        public async Task<SyncResults> SyncWith(ISyncable remoteModel) => await remoteModel.SyncWith(this);
        public Task SyncMany(ISyncable[] remotes) => throw new NotSupportedException();
        public ValueTask<bool> ShouldSync() => folder.ShouldSync();
    }
}
