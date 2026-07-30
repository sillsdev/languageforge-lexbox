using System.Text.Json;
using FwDataMiniLcmBridge;

namespace FwLiteProjectSync;

public enum SyncJournalState
{
    /// <summary>
    /// Staged files exist and nothing has been moved into place. Recovery is to delete them: the sync that wrote
    /// them never reached its commit point, so none of its work counts.
    /// </summary>
    Staged,

    /// <summary>
    /// The staged files were being moved into place. Recovery is to redo the moves, because both intermediate
    /// states are wrong: db-without-base means the base claims less than was applied, base-without-db means it
    /// claims more. See docs/sync-atomicity/README.md.
    /// </summary>
    Committing,
}

/// <summary>
/// Write-ahead record of a sync's staged files, so an interrupted commit can be finished or discarded rather
/// than leaving the CRDT and its merge base disagreeing.
/// </summary>
public record SyncJournal(
    SyncJournalState State,
    string StagedDbPath,
    string StagedMergeBasePath,
    string TargetDbPath,
    string TargetMergeBasePath)
{
    public static string JournalPath(FwDataProject project)
    {
        return Path.Combine(project.ProjectsPath, $"{project.Name}_sync-journal.json");
    }

    public static async Task<SyncJournal?> Read(FwDataProject project)
    {
        var path = JournalPath(project);
        if (!File.Exists(path)) return null;
        await using var file = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<SyncJournal>(file);
    }

    public async Task Write(FwDataProject project)
    {
        await using var file = File.Create(JournalPath(project));
        await JsonSerializer.SerializeAsync(file, this);
        // The journal only helps if it reaches the disk before the moves it describes.
        file.Flush(flushToDisk: true);
    }

    public static void Delete(FwDataProject project)
    {
        var path = JournalPath(project);
        if (File.Exists(path)) File.Delete(path);
    }
}
