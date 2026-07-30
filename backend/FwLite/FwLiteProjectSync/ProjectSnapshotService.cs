using System.Text.Json;
using FwDataMiniLcmBridge;
using LcmCrdt;
using Microsoft.Extensions.Options;
using MiniLcm;
using SIL.Harmony;

namespace FwLiteProjectSync;

public class ProjectSnapshotService(IOptions<CrdtConfig> crdtConfig, CrdtHistoryHeadService historyHeadService)
{
    public virtual async Task<ProjectSnapshot?> GetProjectSnapshot(FwDataProject project)
    {
        return await ReadSnapshotFile(SnapshotPath(project));
    }

    public virtual async Task<ProjectSnapshot?> ReadSnapshotFile(string snapshotPath)
    {
        if (!File.Exists(snapshotPath)) return null;
        await using var file = File.OpenRead(snapshotPath);
        // crdtConfig's options are fine for reading even though they "exclude" [MiniLcmInternal] members:
        // the modifier only nulls the getter (the write side), so deserialization still populates those
        // members (Order, entity Ids) via their setters. See SaveProjectSnapshot for why they're written.
        return await JsonSerializer.DeserializeAsync<ProjectSnapshot>(file, crdtConfig.Value.JsonSerializerOptions);
    }

    public virtual async Task RegenerateProjectSnapshot(IMiniLcmReadApi crdtApi, FwDataProject project, bool keepBackup)
    {
        await SaveProjectSnapshot(project, await TakeMergeBase(crdtApi), keepBackup);
    }

    /// <summary>
    /// Reads the CRDT's current state as a merge base, stamped with the commit it describes. Resolve this
    /// service from the same scope as <paramref name="crdtApi"/>, or the stamp will name another database's head.
    /// </summary>
    public virtual async Task<ProjectSnapshot> TakeMergeBase(IMiniLcmReadApi crdtApi)
    {
        if (crdtApi is not CrdtMiniLcmApi)
            throw new InvalidOperationException("CrdtApi must be of type CrdtMiniLcmApi to regenerate project snapshot.");
        var snapshot = await crdtApi.TakeProjectSnapshot();
        // Head read after the contents, so the stamp can only ever name a commit the contents already include.
        // The other way round it could name an older commit and make a good base look stale.
        return snapshot with
        {
            Provenance = new SnapshotProvenance(await historyHeadService.GetHeadCommitId(), DateTimeOffset.UtcNow)
        };
    }

    public async Task<bool> RegenerateProjectSnapshotAtCommit(SnapshotAtCommitService snapshotService, FwDataProject project, Guid commitId,
        bool preserveAllFieldWorksCommits = false)
    {
        var snapshot = await snapshotService.GetProjectSnapshotAtCommit(commitId, preserveAllFieldWorksCommits);
        if (snapshot is null) return false;
        // The snapshot describes the project as of commitId by construction, so that's what it's stamped with.
        snapshot = snapshot with { Provenance = new SnapshotProvenance(commitId, DateTimeOffset.UtcNow) };
        await SaveProjectSnapshot(project, snapshot, keepBackup: true);
        return true;
    }

    internal static async Task SaveProjectSnapshot(FwDataProject project, ProjectSnapshot projectSnapshot, bool keepBackup = false)
    {
        var snapshotPath = SnapshotPath(project);

        // Snapshot backups are only for explicit/manual recovery (e.g., an admin regenerating a snapshot to repair a project),
        // not for routine sync operations.
        if (keepBackup && File.Exists(snapshotPath))
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var backupPath = Path.Combine(
                Path.GetDirectoryName(snapshotPath)!,
                $"{Path.GetFileNameWithoutExtension(snapshotPath)}_backup_{timestamp}.json");
            File.Copy(snapshotPath, backupPath);
        }

        await WriteSnapshotFile(snapshotPath, projectSnapshot);
    }

    internal static async Task WriteSnapshotFile(string snapshotPath, ProjectSnapshot projectSnapshot)
    {
        await using var file = File.Create(snapshotPath);
        // Serialize with default options, not crdtConfig's: the CRDT options hide [MiniLcmInternal] members
        // (the internal Order values and entity Ids) — that's the API's presentation view, which omits
        // bookkeeping callers don't need. The snapshot is a stored record, so we keep the full object graph.
        // The sync diff itself keys off business fields and list order, not these, so this is about a
        // lossless, stable on-disk record (pinned by ProjectSnapshotSerializationTests), not diff correctness.
        await JsonSerializer.SerializeAsync(file, projectSnapshot);
    }

    public static string SnapshotPath(FwDataProject project)
    {
        var projectPath = project.ProjectsPath;
        var snapshotPath = Path.Combine(projectPath, $"{project.Name}_snapshot.json");
        return snapshotPath;
    }

    public static bool HasSyncedSuccessfully(FwDataProject project)
    {
        var snapshotPath = SnapshotPath(project);
        if (!File.Exists(snapshotPath)) return false;
        return new FileInfo(snapshotPath).Length > 0;
    }
}
