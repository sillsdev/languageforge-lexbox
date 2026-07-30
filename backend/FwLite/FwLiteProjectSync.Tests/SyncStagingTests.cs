using FwLiteProjectSync.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using MiniLcm;

namespace FwLiteProjectSync.Tests;

/// <summary>
/// The staging area's job is that the CRDT database and the merge base that describes it move together or not at all.
/// A database ahead of its merge base makes the next sync push the difference into fwdata, which is how a stale base
/// loses a FieldWorks user's edits; a merge base ahead of its database hides real fwdata changes instead. See
/// docs/sync-atomicity/README.md.
/// </summary>
public class SyncStagingTests : IAsyncLifetime
{
    private const string WrittenBySync = "written by the sync";

    private CrdtOnlyProjectFixture _fixture = null!;
    private SyncStagingService _stagingService = null!;

    public async Task InitializeAsync()
    {
        _fixture = await CrdtOnlyProjectFixture.Create(nameof(SyncStagingTests));
        _stagingService = _fixture.Services.GetRequiredService<SyncStagingService>();
    }

    public async Task DisposeAsync() => await _fixture.DisposeAsync();

    private string MergeBasePath => ProjectSnapshotService.SnapshotPath(_fixture.FwDataProject);
    private string StagedDbPath => SyncStagingService.StagedDbPath(_fixture.CrdtProject.DbPath);
    private string StagedMergeBasePath => SyncStagingService.StagedMergeBasePath(_fixture.FwDataProject);
    private string JournalPath => SyncJournal.JournalPath(_fixture.FwDataProject);

    private async Task<string[]> PartsOfSpeechOnDisk()
    {
        var api = await _fixture.OpenFresh();
        var partsOfSpeech = await api.GetPartsOfSpeech().ToArrayAsync();
        return [.. partsOfSpeech.Select(pos => pos.Name["en"])];
    }

    private void AssertNothingStaged()
    {
        File.Exists(StagedDbPath).Should().BeFalse();
        File.Exists(StagedMergeBasePath).Should().BeFalse();
        File.Exists(JournalPath).Should().BeFalse();
    }

    [Fact]
    public async Task StagedWritesAreInvisibleUntilCommitted()
    {
        await using var staged = await _stagingService.Stage(_fixture.CrdtProject, _fixture.FwDataProject);
        await _fixture.CreatePartOfSpeech(staged.CrdtApi, WrittenBySync, apiServices: staged.Services);

        (await PartsOfSpeechOnDisk()).Should().NotContain(WrittenBySync);
        File.Exists(StagedDbPath).Should().BeTrue("the sync writes to a copy");
        File.Exists(JournalPath).Should().BeTrue("a staged sync must be recognisable after a crash");
    }

    [Fact]
    public async Task Commit_MovesTheDatabaseAndItsMergeBaseIntoPlaceTogether()
    {
        await using (var staged = await _stagingService.Stage(_fixture.CrdtProject, _fixture.FwDataProject))
        {
            await _fixture.CreatePartOfSpeech(staged.CrdtApi, WrittenBySync, apiServices: staged.Services);
            await staged.PrepareMergeBase();
            await staged.Commit();
        }

        (await PartsOfSpeechOnDisk()).Should().Contain(WrittenBySync);
        var mergeBase = await _fixture.Services.GetRequiredService<ProjectSnapshotService>().ReadSnapshotFile(MergeBasePath);
        mergeBase!.PartsOfSpeech.Should().Contain(pos => pos.Name["en"] == WrittenBySync,
            "the committed merge base has to describe the database it was committed with");
        AssertNothingStaged();
    }

    [Fact]
    public async Task DisposeWithoutCommitting_ThrowsTheWholeAttemptAway()
    {
        var mergeBaseBefore = File.Exists(MergeBasePath) ? await File.ReadAllTextAsync(MergeBasePath) : null;

        await using (var staged = await _stagingService.Stage(_fixture.CrdtProject, _fixture.FwDataProject))
        {
            await _fixture.CreatePartOfSpeech(staged.CrdtApi, WrittenBySync, apiServices: staged.Services);
            await staged.PrepareMergeBase();
        }

        (await PartsOfSpeechOnDisk()).Should().NotContain(WrittenBySync);
        (File.Exists(MergeBasePath) ? await File.ReadAllTextAsync(MergeBasePath) : null).Should().Be(mergeBaseBefore);
        AssertNothingStaged();
    }

    [Fact]
    public async Task Commit_RefusesWhenSomethingElseWroteToTheProjectMeanwhile()
    {
        await using var staged = await _stagingService.Stage(_fixture.CrdtProject, _fixture.FwDataProject);
        await staged.PrepareMergeBase();
        // Committing swaps the whole database file, so a commit that arrived after the copy was taken would vanish.
        await _fixture.CreatePartOfSpeech(_fixture.CrdtApi, "written straight to the project");

        Func<Task> act = () => staged.Commit();

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*while the sync was running*");
        (await PartsOfSpeechOnDisk()).Should().Contain("written straight to the project");
    }

    [Fact]
    public async Task Commit_WithoutAMergeBase_Refuses()
    {
        await using var staged = await _stagingService.Stage(_fixture.CrdtProject, _fixture.FwDataProject);

        Func<Task> act = () => staged.Commit();

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*PrepareMergeBase*");
    }

    [Fact]
    public async Task Recover_StagedJournal_DeletesTheStagedFiles()
    {
        await new SyncJournal(SyncJournalState.Staged, StagedDbPath, StagedMergeBasePath, _fixture.CrdtProject.DbPath, MergeBasePath)
            .Write(_fixture.FwDataProject);
        await File.WriteAllTextAsync(StagedDbPath, "a sync that never reached its commit point");
        await File.WriteAllTextAsync(StagedMergeBasePath, "a merge base that was never committed");

        var action = await _stagingService.RecoverInterruptedSync(_fixture.FwDataProject, _fixture.CrdtProject.DbPath);

        action.Should().Be(SyncRecoveryAction.DiscardedStagedSync);
        AssertNothingStaged();
    }

    [Fact]
    public async Task Recover_CommittingJournal_FinishesTheMoves()
    {
        var stagedMergeBase = ProjectSnapshot.Empty with
        {
            Provenance = new SnapshotProvenance(Guid.NewGuid(), DateTimeOffset.UtcNow)
        };
        await ProjectSnapshotService.SaveProjectSnapshot(_fixture.FwDataProject, ProjectSnapshot.Empty);
        await new SyncJournal(SyncJournalState.Committing, StagedDbPath, StagedMergeBasePath, _fixture.CrdtProject.DbPath, MergeBasePath)
            .Write(_fixture.FwDataProject);
        await ProjectSnapshotService.WriteSnapshotFile(StagedMergeBasePath, stagedMergeBase);

        var action = await _stagingService.RecoverInterruptedSync(_fixture.FwDataProject, _fixture.CrdtProject.DbPath);

        action.Should().Be(SyncRecoveryAction.CompletedInterruptedCommit);
        var mergeBase = await _fixture.Services.GetRequiredService<ProjectSnapshotService>().ReadSnapshotFile(MergeBasePath);
        mergeBase!.Provenance!.CrdtCommitId.Should().Be(stagedMergeBase.Provenance!.CrdtCommitId,
            "half a commit leaves the database ahead of the merge base, so replay has to finish it");
        AssertNothingStaged();
    }

    [Fact]
    public async Task Recover_StagedFilesWithNoJournal_SweepsThemAnyway()
    {
        // A crash between clearing old staged files and writing the journal.
        await File.WriteAllTextAsync(StagedDbPath, "an orphan");

        var action = await _stagingService.RecoverInterruptedSync(_fixture.FwDataProject, _fixture.CrdtProject.DbPath);

        action.Should().Be(SyncRecoveryAction.Nothing);
        AssertNothingStaged();
    }

    [Fact]
    public async Task Recover_NothingToRecover_DoesNothing()
    {
        var action = await _stagingService.RecoverInterruptedSync(_fixture.FwDataProject, _fixture.CrdtProject.DbPath);

        action.Should().Be(SyncRecoveryAction.Nothing);
    }
}
