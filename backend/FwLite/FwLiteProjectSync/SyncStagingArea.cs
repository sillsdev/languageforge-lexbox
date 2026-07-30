using FwDataMiniLcmBridge;
using LcmCrdt;
using Microsoft.Extensions.DependencyInjection;
using MiniLcm;

namespace FwLiteProjectSync;

/// <summary>
/// A sync in progress. Everything written through <see cref="CrdtApi"/> lands in a copy of the project database
/// and only becomes real in <see cref="Commit"/>, together with the merge base that describes it. Disposing
/// without committing throws the whole attempt away, which is what makes an interrupted sync harmless.
/// </summary>
public sealed class SyncStagingArea : IAsyncDisposable
{
    private readonly SyncStagingService _service;
    private readonly TempCrdtProjectCopy _copy;
    private readonly FwDataProject _fwDataProject;
    private readonly SyncJournal _journal;
    private readonly Guid? _sourceHeadAtStaging;
    private bool _committed;
    private ProjectSnapshot? _newMergeBase;

    internal SyncStagingArea(
        SyncStagingService service,
        TempCrdtProjectCopy copy,
        FwDataProject fwDataProject,
        SyncJournal journal,
        Guid? sourceHeadAtStaging)
    {
        _service = service;
        _copy = copy;
        _fwDataProject = fwDataProject;
        _journal = journal;
        _sourceHeadAtStaging = sourceHeadAtStaging;
    }

    /// <summary>The CRDT side of the sync writes here. Unusable once <see cref="Commit"/> has run.</summary>
    public CrdtMiniLcmApi CrdtApi => _copy.Api;

    /// <summary>Services bound to the staged database rather than to the project's real one.</summary>
    public IServiceProvider Services => _copy.Services;

    /// <summary>
    /// Reads the merge base that goes with the staged database. Call after the sync has finished applying changes
    /// and before <see cref="Commit"/>.
    /// </summary>
    public async Task<ProjectSnapshot> PrepareMergeBase()
    {
        // Resolved from the staged scope so the base is read from, and stamped with, the staged database.
        var snapshotService = Services.GetRequiredService<ProjectSnapshotService>();
        _newMergeBase = await snapshotService.TakeMergeBase(CrdtApi);
        await ProjectSnapshotService.WriteSnapshotFile(_journal.StagedMergeBasePath, _newMergeBase);
        return _newMergeBase;
    }

    /// <summary>
    /// Moves the staged database and its merge base into place as one journalled step.
    /// </summary>
    public async Task Commit()
    {
        if (_newMergeBase is null)
            throw new InvalidOperationException($"Call {nameof(PrepareMergeBase)} before committing; a database without its merge base is exactly the state this class exists to prevent.");
        if (_committed) throw new InvalidOperationException("This sync has already been committed.");

        // The staged database is a copy taken at _sourceHeadAtStaging, so committing it discards anything else
        // that wrote to the real database in the meantime. Nothing should, and this makes that a checked fact.
        var sourceHeadNow = await _service.ReadSourceHeadCommitId();
        if (sourceHeadNow != _sourceHeadAtStaging)
        {
            throw new InvalidOperationException(
                $"The project's CRDT database moved from commit {_sourceHeadAtStaging} to {sourceHeadNow} while the sync was running, " +
                "so committing the sync would discard those commits.");
        }

        await _copy.CloseWithoutDeleting();
        await (_journal with { State = SyncJournalState.Committing }).Write(_fwDataProject);
        await _service.CompleteCommit(_journal, _fwDataProject);
        _committed = true;
    }

    public async ValueTask DisposeAsync()
    {
        if (_committed)
        {
            // Commit moved both files away and deleted the journal; the copy no longer owns anything.
            return;
        }

        await _copy.DisposeAsync();
        await _service.DiscardStagedFiles(_journal.StagedDbPath, _journal.StagedMergeBasePath);
        SyncJournal.Delete(_fwDataProject);
    }
}
