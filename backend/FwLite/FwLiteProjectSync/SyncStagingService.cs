using FwDataMiniLcmBridge;
using LcmCrdt;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace FwLiteProjectSync;

public enum SyncRecoveryAction
{
    /// <summary>No interrupted sync to clean up after.</summary>
    Nothing,

    /// <summary>An earlier sync died before its commit point. Its work is gone; nothing had been applied.</summary>
    DiscardedStagedSync,

    /// <summary>An earlier sync died mid-commit. Its work is now applied, both halves of it.</summary>
    CompletedInterruptedCommit,
}

/// <summary>
/// Runs the CRDT half of a sync against a copy of the project database, so the database and the merge base can be
/// updated as one journalled step at the end. See docs/sync-atomicity/README.md for why the alternative
/// (write as you go, rewrite the base afterwards) loses data whenever a sync is interrupted.
/// Resolve from a scope bound to the project being synced.
/// </summary>
public class SyncStagingService(
    CrdtProjectsService crdtProjectsService,
    CrdtHistoryHeadService sourceHistory,
    ILogger<SyncStagingService> logger)
{
    /// <summary>
    /// Finishes or discards an interrupted sync. Must run before <see cref="Stage"/>, and before anything reads
    /// the merge base, since a pending commit means the merge base on disk is not the one the last sync produced.
    /// </summary>
    public async Task<SyncRecoveryAction> RecoverInterruptedSync(FwDataProject fwDataProject, string crdtDbPath)
    {
        var journal = await SyncJournal.Read(fwDataProject);
        if (journal is null)
        {
            // A crash between clearing old staged files and writing the journal leaves no journal, so sweep the
            // deterministic staged paths anyway rather than staging on top of them.
            await DiscardStagedFiles(StagedDbPath(crdtDbPath), StagedMergeBasePath(fwDataProject));
            return SyncRecoveryAction.Nothing;
        }

        switch (journal.State)
        {
            case SyncJournalState.Staged:
                logger.LogWarning("Discarding a sync that was interrupted before its commit point; nothing had been applied");
                await DiscardStagedFiles(journal.StagedDbPath, journal.StagedMergeBasePath);
                SyncJournal.Delete(fwDataProject);
                return SyncRecoveryAction.DiscardedStagedSync;

            case SyncJournalState.Committing:
                logger.LogWarning("Completing a sync commit that was interrupted part way through");
                await CompleteCommit(journal, fwDataProject);
                return SyncRecoveryAction.CompletedInterruptedCommit;

            default:
                throw new InvalidOperationException($"Unknown sync journal state {journal.State}");
        }
    }

    public async Task<SyncStagingArea> Stage(CrdtProject crdtProject, FwDataProject fwDataProject)
    {
        var stagedDbPath = StagedDbPath(crdtProject.DbPath);
        var stagedMergeBasePath = StagedMergeBasePath(fwDataProject);
        await DiscardStagedFiles(stagedDbPath, stagedMergeBasePath);

        var journal = new SyncJournal(SyncJournalState.Staged,
            stagedDbPath,
            stagedMergeBasePath,
            crdtProject.DbPath,
            ProjectSnapshotService.SnapshotPath(fwDataProject));
        // Journal first: a staged file we don't know about is worse than a journal describing files that
        // aren't there yet, because recovery for the latter is "delete nothing and carry on".
        await journal.Write(fwDataProject);

        try
        {
            var headAtStaging = await sourceHistory.GetHeadCommitId();
            logger.LogInformation("Staging the CRDT side of the sync at {StagedDbPath} (source head {HeadAtStaging})",
                Path.GetFileName(stagedDbPath), headAtStaging);
            var copy = await crdtProjectsService.OpenProjectCopy(crdtProject, stagedDbPath);
            return new SyncStagingArea(this, copy, fwDataProject, journal, headAtStaging);
        }
        catch
        {
            // Recovery would handle this journal, but it would report an interrupted sync that never started.
            SyncJournal.Delete(fwDataProject);
            throw;
        }
    }

    internal async Task<Guid?> ReadSourceHeadCommitId() => await sourceHistory.GetHeadCommitId();

    internal async Task CompleteCommit(SyncJournal journal, FwDataProject fwDataProject)
    {
        // Redo from the top: each move is skipped when its source is already gone, so replaying from any
        // interruption point lands on the same end state.
        if (File.Exists(journal.StagedDbPath))
        {
            ClearConnectionPool(journal.TargetDbPath);
            ClearConnectionPool(journal.StagedDbPath);
            // The replaced database's journal/wal sidecars describe the old file and would corrupt the new one.
            DeleteSqliteSidecars(journal.TargetDbPath);
            await MoveWithRetry(journal.StagedDbPath, journal.TargetDbPath);
        }

        if (File.Exists(journal.StagedMergeBasePath))
        {
            await MoveWithRetry(journal.StagedMergeBasePath, journal.TargetMergeBasePath);
        }

        SyncJournal.Delete(fwDataProject);
        logger.LogInformation("Sync commit complete: CRDT database and merge base are both at the new state");
    }

    internal async Task DiscardStagedFiles(string stagedDbPath, string stagedMergeBasePath)
    {
        if (File.Exists(stagedDbPath))
        {
            logger.LogInformation("Deleting staged CRDT database {StagedDbPath}", Path.GetFileName(stagedDbPath));
            await crdtProjectsService.DeleteDatabaseFile(stagedDbPath);
        }

        if (File.Exists(stagedMergeBasePath)) File.Delete(stagedMergeBasePath);
    }

    public static string StagedDbPath(string crdtDbPath) => crdtDbPath + ".staging";

    public static string StagedMergeBasePath(FwDataProject project) =>
        ProjectSnapshotService.SnapshotPath(project) + ".staging";

    private static void ClearConnectionPool(string dbPath)
    {
        try
        {
            var connectionString = new SqliteConnectionStringBuilder { DataSource = dbPath }.ConnectionString;
            using var connection = new SqliteConnection(connectionString);
            SqliteConnection.ClearPool(connection);
        }
        catch
        {
            // Best effort; the retry loop in MoveWithRetry covers a pooled connection we failed to close here.
        }
    }

    private static void DeleteSqliteSidecars(string dbPath)
    {
        foreach (var suffix in new[] { "-wal", "-shm", "-journal" })
        {
            var sidecar = dbPath + suffix;
            if (File.Exists(sidecar)) File.Delete(sidecar);
        }
    }

    private async Task MoveWithRetry(string source, string destination)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                File.Move(source, destination, overwrite: true);
                return;
            }
            catch (IOException) when (attempt < 10)
            {
                logger.LogWarning("Could not move {Source} into place yet (attempt {Attempt}), retrying",
                    Path.GetFileName(source), attempt);
                await Task.Delay(500);
            }
        }
    }
}
