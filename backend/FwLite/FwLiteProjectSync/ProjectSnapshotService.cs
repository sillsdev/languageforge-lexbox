using System.Text.Json;
using FwDataMiniLcmBridge;
using LcmCrdt;
using Microsoft.Extensions.Options;
using MiniLcm;
using SIL.Harmony;

namespace FwLiteProjectSync;

public class ProjectSnapshotService(IOptions<CrdtConfig> crdtConfig)
{
    public virtual async Task<ProjectSnapshot?> GetProjectSnapshot(FwDataProject project)
    {
        var snapshotPath = SnapshotPath(project);
        if (!File.Exists(snapshotPath)) return null;
        await using var file = File.OpenRead(snapshotPath);
        // crdtConfig's options are fine for reading even though they "exclude" [MiniLcmInternal] members:
        // the modifier only nulls the getter (the write side), so deserialization still populates those
        // members (Order, entity Ids) via their setters. See SaveProjectSnapshot for why they're written.
        return await JsonSerializer.DeserializeAsync<ProjectSnapshot>(file, crdtConfig.Value.JsonSerializerOptions);
    }

    public virtual async Task RegenerateProjectSnapshot(IMiniLcmReadApi crdtApi, FwDataProject project, bool keepBackup)
    {
        if (crdtApi is not CrdtMiniLcmApi)
            throw new InvalidOperationException("CrdtApi must be of type CrdtMiniLcmApi to regenerate project snapshot.");
        await SaveProjectSnapshot(project, await crdtApi.TakeProjectSnapshot(), keepBackup);
    }

    public async Task<bool> RegenerateProjectSnapshotAtCommit(SnapshotAtCommitService snapshotService, FwDataProject project, Guid commitId,
        bool preserveAllFieldWorksCommits = false)
    {
        var snapshot = await snapshotService.GetProjectSnapshotAtCommit(commitId, preserveAllFieldWorksCommits);
        if (snapshot is null) return false;
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

        await using var file = File.Create(snapshotPath);
        // Serialize with default options, not crdtConfig's: the CRDT options hide [MiniLcmInternal] members
        // (the internal Order values and entity Ids) — that's the API's presentation view, which omits
        // bookkeeping callers don't need. The snapshot is a stored record, so we keep the full object graph.
        // The sync diff itself keys off business fields and list order, not these, so this is about a
        // lossless, stable on-disk record (pinned by ProjectSnapshotSerializationTests), not diff correctness.
        await JsonSerializer.SerializeAsync(file, projectSnapshot);
    }

    internal static string SnapshotPath(FwDataProject project)
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
