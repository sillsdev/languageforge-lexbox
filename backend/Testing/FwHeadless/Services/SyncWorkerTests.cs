using FwHeadless.Services;
using LexCore.Sync;
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
            MediaSyncCrdt,
            GetSnapshot,
            Sync,
            PostSendReceive,
            RegenerateSnapshot,
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
            MediaSyncCrdt,
            GetSnapshot,
            Sync,
            RegenerateSnapshot,
            HarmonySync);
    }

    [Fact]
    public async Task ExecuteSync_NoCrdtChanges_DoesNotRegenerateSnapshot()
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
            MediaSyncCrdt,
            GetSnapshot,
            Sync,
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
            MediaSyncCrdt,
            GetSnapshot,
            Sync,
            PostSendReceive);
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
    public async Task ExecuteSync_ProjectBlocked_AfterAuth_DoesNoWrites()
    {
        using var h = new SyncWorkerTestHarness();
        h.SetSyncBlockedInfo(new SyncBlockedInfo { IsBlocked = true, Reason = "maintenance" });
        Directory.Exists(h.ProjectFolder).Should().BeFalse();

        var result = await h.RunAsync(new SyncResult(CrdtChanges: 0, FwdataChanges: 0), setupFwDataProject: false);

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
        var result = await h.RunAsync(syncResult, setupFwDataProject: false);

        result.Status.Should().Be(SyncJobStatusEnum.ProjectNotFound);
        h.Steps.Should().BeEmpty();
    }

    [Fact]
    public async Task ExecuteSync_AuthFails_ReturnsUnableToAuthenticate()
    {
        using var h = new SyncWorkerTestHarness();
        var syncResult = new SyncResult(CrdtChanges: 0, FwdataChanges: 0);

        var result = await h.RunAsync(syncResult, authSuccess: false, setupFwDataProject: false);

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

        result.Status.Should().Be(SyncJobStatusEnum.Success);
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
            RegenerateSnapshot,
            HarmonySync);
    }

    [Fact]
    public async Task ExecuteSync_FwDataFileMissing_ClonesProject()
    {
        using var h = new SyncWorkerTestHarness();
        var syncResult = new SyncResult(CrdtChanges: 0, FwdataChanges: 0);

        var result = await h.RunAsync(syncResult, createFwDataFile: false);

        result.Status.Should().Be(SyncJobStatusEnum.Success);
        h.Steps.Should().Equal(
            TestAuth,
            CheckBlocked,
            Clone,
            MediaSyncFwData,
            MediaSyncCrdt,
            GetSnapshot,
            Sync,
            HarmonySync);
    }

    [Fact]
    public async Task ExecuteSync_CrdtProjectMissingFile_Throws()
    {
        using var h = new SyncWorkerTestHarness();
        h.SetIsCrdtProject(true);

        Func<Task> act = () => h.RunAsync(new SyncResult(CrdtChanges: 0, FwdataChanges: 0));

        await act.Should().ThrowAsync<InvalidOperationException>();
        h.Steps.Should().Equal(
            TestAuth,
            CheckBlocked,
            PreSendReceive,
            MediaSyncFwData);
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

