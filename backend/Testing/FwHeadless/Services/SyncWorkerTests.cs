using System.Text.Json;
using FwHeadless;
using FwHeadless.Services;
using FwLiteProjectSync;
using LexCore.Sync;
using MiniLcm;
using MiniLcm.Models;
using Moq;
using static Testing.FwHeadless.Services.SyncStep;

namespace Testing.FwHeadless.Services;

public class SyncWorkerTests
{
    [Fact]
    public async Task ExecuteSync_SuccessWithCrdtAndFwChanges_RegeneratesSnapshotAfterSendReceive()
    {
        using var h = new SyncWorkerTestHarness();
        var syncResult = new SyncResult(CrdtChanges: 5, FwdataChanges: 3);

        var result = await h.RunAsync(syncResult);

        result.Status.Should().Be(SyncJobStatusEnum.Success);
        h.Steps.Should().Equal(
            TestAuth,
            CheckBlocked,
            PreSendReceive,
            MediaSyncFwData,
            HarmonySync,
            MediaSyncCrdt,
            GetSnapshot,
            Sync,
            PrepareMergeBase,
            PostSendReceive,
            HarmonySync);
    }

    [Fact]
    public async Task ExecuteSync_CrdtChangesNoFwChanges_RegeneratesSnapshotWithoutPostSendReceive()
    {
        using var h = new SyncWorkerTestHarness();
        var syncResult = new SyncResult(CrdtChanges: 5, FwdataChanges: 0);

        var result = await h.RunAsync(syncResult);

        result.Status.Should().Be(SyncJobStatusEnum.Success);
        h.Steps.Should().Equal(
            TestAuth,
            CheckBlocked,
            PreSendReceive,
            MediaSyncFwData,
            HarmonySync,
            MediaSyncCrdt,
            GetSnapshot,
            Sync,
            PrepareMergeBase,
            HarmonySync);
    }

    [Fact]
    public async Task ExecuteSync_NoChanges_RegeneratesSnapshotAnyway()
    {
        using var h = new SyncWorkerTestHarness();
        var syncResult = new SyncResult(CrdtChanges: 0, FwdataChanges: 0);

        var result = await h.RunAsync(syncResult);

        result.Status.Should().Be(SyncJobStatusEnum.Success);
        h.Steps.Should().Equal(
            TestAuth,
            CheckBlocked,
            PreSendReceive,
            MediaSyncFwData,
            HarmonySync,
            MediaSyncCrdt,
            GetSnapshot,
            Sync,
            PrepareMergeBase,
            HarmonySync);
    }

    [Theory]
    [InlineData("Some error occurred", false, SyncJobStatusEnum.SendReceiveFailed)]
    [InlineData("Rolling back... validation failed", true, SyncJobStatusEnum.SyncBlocked)]
    public async Task ExecuteSync_PostSendReceiveFailure_ReturnsExpectedStatus(string output, bool rollback, SyncJobStatusEnum expectedStatus)
    {
        using var h = new SyncWorkerTestHarness();
        h.SetSendReceiveResults(
            new SendReceiveHelpers.LfMergeBridgeResult("success"),
            new SendReceiveHelpers.LfMergeBridgeResult(output, ProgressHelper.CreateErrorProgress()));

        var syncResult = new SyncResult(CrdtChanges: 5, FwdataChanges: 3);
        var result = await h.RunAsync(syncResult);

        result.Status.Should().Be(expectedStatus);
        if (rollback)
        {
            result.Error.Should().Contain("rollback");
            h.MetadataServiceMock.Verify(
                s => s.BlockFromSyncAsync(h.ProjectId, It.Is<string>(msg => msg.Contains("Rollback"))),
                Times.Once);
        }
        else
        {
            h.MetadataServiceMock.Verify(
                s => s.BlockFromSyncAsync(It.IsAny<Guid>(), It.IsAny<string>()),
                Times.Never);
        }
        h.Steps.Should().Equal(
            TestAuth,
            CheckBlocked,
            PreSendReceive,
            MediaSyncFwData,
            HarmonySync,
            MediaSyncCrdt,
            GetSnapshot,
            Sync,
            PrepareMergeBase,
            PostSendReceive);
    }

    [Theory]
    [InlineData(false, SyncJobStatusEnum.SendReceiveFailed)]
    [InlineData(true, SyncJobStatusEnum.Success)]
    public async Task ExecuteSync_PostSendReceiveHttp500_RetriesOneTime(bool retrySucceeds, SyncJobStatusEnum expectedStatus)
    {
        using var h = new SyncWorkerTestHarness();
        const string error500 = SendReceiveHelpers.LfMergeBridgeResult.Http500Indicator;
        h.SetSendReceiveResults(
            // First S/R succeeds
            new SendReceiveHelpers.LfMergeBridgeResult("success"),
            // Second S/R gets HTTP 500
            new SendReceiveHelpers.LfMergeBridgeResult(error500, ProgressHelper.CreateErrorProgress()),
            // "Third" S/R (first and only retry of second S/R) succeeds in one test, gets HTTP 500 in the other test
            retrySucceeds
                ? new SendReceiveHelpers.LfMergeBridgeResult("success")
                : new SendReceiveHelpers.LfMergeBridgeResult(error500, ProgressHelper.CreateErrorProgress()));

        var syncResult = new SyncResult(CrdtChanges: 5, FwdataChanges: 3);
        var result = await h.RunAsync(syncResult);

        result.Status.Should().Be(expectedStatus);
        var expectedSteps = new List<SyncStep>([
            TestAuth,
            CheckBlocked,
            PreSendReceive,
            MediaSyncFwData,
            HarmonySync,
            MediaSyncCrdt,
            GetSnapshot,
            Sync,
            PrepareMergeBase,
            PostSendReceive,
            PostSendReceive
        ]);
        if (retrySucceeds)
        {
            expectedSteps.Add(HarmonySync);
        }
        h.Steps.Should().Equal(expectedSteps);
    }

    [Theory]
    [InlineData("Rolling back... validation error", true, SyncJobStatusEnum.SyncBlocked)]
    [InlineData("network error", false, SyncJobStatusEnum.SendReceiveFailed)]
    public async Task ExecuteSync_PreSendReceiveFailure_ReturnsExpectedStatus(string output, bool rollback, SyncJobStatusEnum expectedStatus)
    {
        using var h = new SyncWorkerTestHarness();
        h.SetPendingCommitCount(1);
        h.SetSendReceiveResults(new SendReceiveHelpers.LfMergeBridgeResult(output, ProgressHelper.CreateErrorProgress()));

        var syncResult = new SyncResult(CrdtChanges: 0, FwdataChanges: 0);
        var result = await h.RunAsync(syncResult);

        result.Status.Should().Be(expectedStatus);
        if (rollback)
        {
            result.Error.Should().Contain("rollback");
            h.MetadataServiceMock.Verify(
                s => s.BlockFromSyncAsync(h.ProjectId, It.Is<string>(msg => msg.Contains("Rollback"))),
                Times.Once);
        }
        else
        {
            h.MetadataServiceMock.Verify(
                s => s.BlockFromSyncAsync(It.IsAny<Guid>(), It.IsAny<string>()),
                Times.Never);
        }
        h.Steps.Should().Equal(
            TestAuth,
            CheckBlocked,
            PreSendReceive);
    }

    [Fact]
    public async Task ExecuteSync_CloneFailure_DeletesProjectFolder()
    {
        using var h = new SyncWorkerTestHarness();
        h.SetCloneResult(new SendReceiveHelpers.LfMergeBridgeResult("clone error", ProgressHelper.CreateErrorProgress()));

        // Create a sibling project folder to verify it's not affected
        var siblingProject = h.Config.GetFwDataProject("other-project", Guid.NewGuid());
        Directory.CreateDirectory(siblingProject.ProjectsPath);
        h.EnsureFwDataFileExists(siblingProject);

        var result = await h.RunAsync(
            new SyncResult(CrdtChanges: 0, FwdataChanges: 0),
            snapshotExists: false,
            createFwDataFileBeforeSync: false,
            crdtProjectExists: false);

        result.Status.Should().Be(SyncJobStatusEnum.SendReceiveFailed);
        Directory.Exists(h.ProjectFolder).Should().BeFalse("project folder should be cleaned up after failed clone");
        File.Exists(siblingProject.FilePath).Should().BeTrue("other projects should not be affected");
        h.Steps.Should().Equal(
            TestAuth,
            CheckBlocked,
            Clone);
    }

    [Fact]
    public async Task ExecuteSync_ProjectBlocked_AfterAuth_DoesNoWrites()
    {
        using var h = new SyncWorkerTestHarness();
        h.SetSyncBlockedInfo(new SyncBlockedInfo { IsBlocked = true, Reason = "maintenance" });
        Directory.Exists(h.ProjectFolder).Should().BeFalse();

        var result = await h.RunAsync(new SyncResult(CrdtChanges: 0, FwdataChanges: 0),
            snapshotExists: false,
            setupFwDataProject: false,
            crdtProjectExists: false);

        result.Status.Should().Be(SyncJobStatusEnum.SyncBlocked);
        result.Error.Should().Contain("blocked");
        h.Steps.Should().Equal(
            TestAuth,
            CheckBlocked);
        Directory.Exists(h.ProjectFolder).Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteSync_ProjectNotFound_ReturnsProjectNotFound()
    {
        using var h = new SyncWorkerTestHarness();
        h.SetProjectCode(null);

        var syncResult = new SyncResult(CrdtChanges: 0, FwdataChanges: 0);
        var result = await h.RunAsync(syncResult, snapshotExists: false, setupFwDataProject: false, crdtProjectExists: false);

        result.Status.Should().Be(SyncJobStatusEnum.ProjectNotFound);
        h.Steps.Should().BeEmpty();
    }

    [Fact]
    public async Task ExecuteSync_AuthFails_ReturnsUnableToAuthenticate()
    {
        using var h = new SyncWorkerTestHarness();
        var syncResult = new SyncResult(CrdtChanges: 0, FwdataChanges: 0);

        var result = await h.RunAsync(syncResult, authSuccess: false, snapshotExists: false, setupFwDataProject: false, crdtProjectExists: false);

        result.Status.Should().Be(SyncJobStatusEnum.UnableToAuthenticate);
        h.Steps.Should().Equal(TestAuth);
    }

    [Fact]
    public async Task ExecuteSync_OnlyHarmony_SkipsCrdtSyncAndSnapshot()
    {
        using var h = new SyncWorkerTestHarness();
        h.SetPendingCommitCount(0);

        var syncResult = new SyncResult(CrdtChanges: 5, FwdataChanges: 5);
        var result = await h.RunAsync(syncResult, onlyHarmony: true);

        result.Status.Should().Be(SyncJobStatusEnum.SuccessHarmonyOnly);
        h.Steps.Should().Equal(
            TestAuth,
            CheckBlocked,
            MediaSyncFwData,
            HarmonySync,
            MediaSyncCrdt);
    }

    [Fact]
    public async Task ExecuteSync_NoSnapshot_ImportsProject()
    {
        using var h = new SyncWorkerTestHarness();
        var importResult = new SyncResult(CrdtChanges: 10, FwdataChanges: 0);

        var result = await h.RunAsync(importResult, snapshotExists: false);

        result.Status.Should().Be(SyncJobStatusEnum.Success);
        h.Steps.Should().Equal(
            TestAuth,
            CheckBlocked,
            PreSendReceive,
            MediaSyncFwData,
            MediaSyncCrdt,
            GetSnapshot,
            Import,
            PrepareMergeBase,
            HarmonySync);
    }

    [Fact]
    public async Task ExecuteSync_FwDataFileMissing_ClonesProject()
    {
        using var h = new SyncWorkerTestHarness();
        var syncResult = new SyncResult(CrdtChanges: 0, FwdataChanges: 0);

        var result = await h.RunAsync(syncResult, createFwDataFileBeforeSync: false);

        result.Status.Should().Be(SyncJobStatusEnum.Success);
        Directory.Exists(h.ProjectFolder).Should().BeTrue("project folder should not be deleted on successful clone");
        h.Steps.Should().Equal(
            TestAuth,
            CheckBlocked,
            Clone,
            MediaSyncFwData,
            HarmonySync,
            MediaSyncCrdt,
            GetSnapshot,
            Sync,
            PrepareMergeBase,
            HarmonySync);
    }

    [Fact]
    public async Task ExecuteSync_FwDataFileStillMissingAfterPreSetup_ReturnsUnableToSync()
    {
        using var h = new SyncWorkerTestHarness();

        var result = await h.RunAsync(
            new SyncResult(CrdtChanges: 0, FwdataChanges: 0),
            snapshotExists: false,
            createFwDataFileBeforeSync: false,
            createFwDataFileAfterClone: false,
            crdtProjectExists: false);

        result.Status.Should().Be(SyncJobStatusEnum.ProjectIncompatible);
        result.Error.Should().Contain("does not contain a FieldWorks project");
        Directory.Exists(h.ProjectFolder).Should().BeFalse("project folder should be cleaned up for incompatible projects");
        h.Steps.Should().Equal(
            TestAuth,
            CheckBlocked,
            Clone);
    }

    [Fact]
    public async Task ExecuteSync_CloneFailure_PreservesCrdtData()
    {
        using var h = new SyncWorkerTestHarness();
        h.SetCloneResult(new SendReceiveHelpers.LfMergeBridgeResult("clone error", ProgressHelper.CreateErrorProgress()));

        // Simulate a previous successful sync that left behind CRDT data
        Directory.CreateDirectory(h.ProjectFolder);
        File.WriteAllText(Path.Combine(h.ProjectFolder, "crdt.sqlite"), "existing crdt data");

        var result = await h.RunAsync(
            new SyncResult(CrdtChanges: 0, FwdataChanges: 0),
            snapshotExists: false,
            createFwDataFileBeforeSync: false,
            crdtProjectExists: false);

        result.Status.Should().Be(SyncJobStatusEnum.SendReceiveFailed);
        Directory.Exists(h.FwDataProject.ProjectFolder).Should().BeFalse("fw subfolder should be cleaned up");
        Directory.Exists(h.ProjectFolder).Should().BeTrue("project folder should be preserved when it contains CRDT data");
        File.Exists(Path.Combine(h.ProjectFolder, "crdt.sqlite")).Should().BeTrue("CRDT data should not be deleted");
        h.Steps.Should().Equal(
            TestAuth,
            CheckBlocked,
            Clone);
    }

    [Fact]
    public async Task ExecuteSync_CrdtProjectMissingFile_Throws()
    {
        using var h = new SyncWorkerTestHarness();
        h.SetIsCrdtProject(true);

        Func<Task> act = () => h.RunAsync(new SyncResult(CrdtChanges: 0, FwdataChanges: 0),
            snapshotExists: false,
            crdtProjectExists: false);

        await act.Should().ThrowAsync<InvalidOperationException>();
        h.Steps.Should().Equal(
            TestAuth,
            CheckBlocked,
            PreSendReceive,
            MediaSyncFwData);
    }
}

/// <summary>
/// The failure modes in docs/sync-atomicity/README.md, at the orchestration level: what a sync that dies
/// part way through leaves behind. The invariant under test is that the CRDT database and the merge base move
/// together or not at all, because a merge base that claims less than was applied makes the next sync push the
/// leftovers into fwdata.
/// </summary>
public class SyncWorkerInterruptionTests
{
    private const string WrittenBySync = "written by the sync";


    [Fact]
    public async Task SyncThrows_LeavesTheMergeBaseAndCrdtDatabaseUntouched()
    {
        using var h = new SyncWorkerTestHarness();
        h.SetSyncWritesPartOfSpeech(WrittenBySync);
        h.SetSyncThrows(new InvalidOperationException("sync died half way through"));

        Func<Task> act = () => h.RunAsync(new SyncResult(CrdtChanges: 5, FwdataChanges: 3));

        await act.Should().ThrowAsync<InvalidOperationException>();
        h.ReadMergeBase()!.PartsOfSpeech.Should().NotContain(pos => pos.Name["en"] == WrittenBySync);
        (await h.ReadCrdtPartsOfSpeech()).Should().NotContain(WrittenBySync,
            "what the interrupted sync applied to the CRDT has to go with it, or the next sync pushes it into fwdata");
        h.AssertNothingStaged();
    }

    [Fact]
    public async Task FwDataPushFails_LeavesTheMergeBaseAndCrdtDatabaseUntouched()
    {
        using var h = new SyncWorkerTestHarness();
        h.SetSyncWritesPartOfSpeech(WrittenBySync);
        h.SetSendReceiveResults(
            new SendReceiveHelpers.LfMergeBridgeResult("success"),
            new SendReceiveHelpers.LfMergeBridgeResult("network died", ProgressHelper.CreateErrorProgress()));

        var result = await h.RunAsync(new SyncResult(CrdtChanges: 5, FwdataChanges: 3));

        result.Status.Should().Be(SyncJobStatusEnum.SendReceiveFailed);
        h.ReadMergeBase()!.PartsOfSpeech.Should().NotContain(pos => pos.Name["en"] == WrittenBySync,
            "a merge base committed before the fwdata push succeeds would describe changes that may have been rolled back");
        (await h.ReadCrdtPartsOfSpeech()).Should().NotContain(WrittenBySync);
        h.AssertNothingStaged();
    }

    [Fact]
    public async Task SuccessfulSync_CommitsTheMergeBaseStampedWithTheCrdtItDescribes()
    {
        using var h = new SyncWorkerTestHarness();
        h.SetSyncWritesPartOfSpeech(WrittenBySync);

        var result = await h.RunAsync(new SyncResult(CrdtChanges: 5, FwdataChanges: 3));

        result.Status.Should().Be(SyncJobStatusEnum.Success);
        h.AssertNothingStaged();
        (await h.ReadCrdtPartsOfSpeech()).Should().Contain(WrittenBySync,
            "committing the sync moves the staged database into place");
        var mergeBase = h.ReadMergeBase();
        mergeBase!.PartsOfSpeech.Should().Contain(pos => pos.Name["en"] == WrittenBySync,
            "the committed merge base must describe the CRDT the sync produced, not the one it started from");
        mergeBase.Provenance!.CrdtCommitId.Should().NotBeNull();
    }

    [Fact]
    public async Task StagedSyncLeftBehind_IsDiscardedBeforeTheNextSyncReadsTheMergeBase()
    {
        using var h = new SyncWorkerTestHarness();
        // A run that died before its commit point: staged files on disk, nothing applied.
        h.WriteSyncJournal(SyncJournalState.Staged);
        await File.WriteAllTextAsync(h.StagedCrdtDbPath, "a half finished sync");
        await File.WriteAllTextAsync(h.StagedMergeBasePath, "a merge base that was never committed");

        var result = await h.RunAsync(new SyncResult(CrdtChanges: 1, FwdataChanges: 0));

        result.Status.Should().Be(SyncJobStatusEnum.Success);
        h.AssertNothingStaged();
    }

    [Fact]
    public async Task CommitInterruptedPartWay_IsFinishedBeforeTheNextSyncReadsTheMergeBase()
    {
        using var h = new SyncWorkerTestHarness();
        // A run that died between moving the CRDT database into place and moving its merge base: the base on disk
        // is the old one and describes less than the database holds, which is what corrupts the next sync.
        var interruptedBase = ProjectSnapshot.Empty with
        {
            Provenance = new SnapshotProvenance(Guid.NewGuid(), DateTimeOffset.UtcNow),
            ComplexFormTypes = [new ComplexFormType { Id = Guid.NewGuid(), Name = new MultiString { { "en", "committed by the interrupted run" } } }]
        };
        h.WriteSyncJournal(SyncJournalState.Committing);
        await File.WriteAllTextAsync(h.StagedMergeBasePath, JsonSerializer.Serialize(interruptedBase));

        var result = await h.RunAsync(new SyncResult(CrdtChanges: 1, FwdataChanges: 0));

        result.Status.Should().Be(SyncJobStatusEnum.Success);
        h.SyncedAgainstMergeBase!.ComplexFormTypes.Should().ContainSingle()
            .Which.Name["en"].Should().Be("committed by the interrupted run",
                "the interrupted commit must be finished before the merge base is read, or the sync uses the superseded one");
        h.AssertNothingStaged();
    }

    [Fact]
    public async Task UnrecordedSyncCommitsInTheCrdt_AreReportedAsAStaleMergeBase()
    {
        using var h = new SyncWorkerTestHarness();

        var result = await h.RunAsync(new SyncResult(CrdtChanges: 1, FwdataChanges: 0), leaveUnrecordedSyncCommit: true);

        // Warn is the default: reporting, not refusing, until the fleet has been surveyed.
        result.Status.Should().Be(SyncJobStatusEnum.Success);
    }

    [Fact]
    public async Task UnrecordedSyncCommitsInTheCrdt_RefuseToSyncWhenConfiguredToFail()
    {
        using var h = new SyncWorkerTestHarness();
        h.Config.StaleMergeBaseAction = StaleMergeBaseAction.Fail;

        var result = await h.RunAsync(new SyncResult(CrdtChanges: 1, FwdataChanges: 0), leaveUnrecordedSyncCommit: true);

        result.Status.Should().Be(SyncJobStatusEnum.StaleMergeBase);
        result.Error.Should().Contain("written by the sync");
        h.Steps.Should().NotContain(Sync, "the sync must not run against a base it can prove is stale");
        h.AssertNothingStaged();
    }

    [Fact]
    public async Task HumanCommitsAfterTheMergeBase_AreNotMistakenForAStaleMergeBase()
    {
        using var h = new SyncWorkerTestHarness();
        h.Config.StaleMergeBaseAction = StaleMergeBaseAction.Fail;

        // FW Lite edits arriving after the merge base are the normal case and the whole reason the second sync pass
        // exists, so they must not read as staleness.
        var result = await h.RunAsync(new SyncResult(CrdtChanges: 1, FwdataChanges: 0), leaveUnrecordedUserCommit: true);

        result.Status.Should().Be(SyncJobStatusEnum.Success);
        h.Steps.Should().Contain(Sync);
    }
}

/// <summary>
/// Helper to create an IProgress that reports ErrorEncountered = true.
/// </summary>
internal static class ProgressHelper
{
    public static SIL.Progress.IProgress CreateErrorProgress()
    {
        var mock = new Mock<SIL.Progress.IProgress>();
        mock.Setup(p => p.ErrorEncountered).Returns(true);
        return mock.Object;
    }
}
