using FluentAssertions;
using FwDataMiniLcmBridge;
using FwDataMiniLcmBridge.Api;
using FwDataMiniLcmBridge.Tests.Fixtures;
using FwHeadless;
using FwHeadless.Media;
using FwHeadless.Services;
using FwLiteProjectSync;
using LcmCrdt;
using LcmCrdt.RemoteSync;
using LexCore.Sync;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MiniLcm;
using Moq;

namespace Testing.FwHeadless.Services;

public class SyncWorkerTests : IDisposable, IAsyncDisposable
{
    private readonly Guid _projectId = Guid.NewGuid();
    private const string ProjectCode = "test-project";
    private readonly List<string> _callSequence = [];

    private readonly Mock<ISendReceiveService> _mockSendReceive = new();
    private readonly Mock<IProjectLookupService> _mockProjectLookup = new();
    private readonly Mock<ISyncJobStatusService> _mockSyncJobStatus = new();
    private readonly Mock<IProjectMetadataService> _mockMetadataService = new();

    private readonly FwHeadlessConfig _config;
    private readonly string _tempDir;
    private FwDataProject FwDataProject => _config.GetFwDataProject(ProjectCode, _projectId);
    private IServiceScope? _workerScope;

    public SyncWorkerTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "SyncWorkerTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);

        _config = new FwHeadlessConfig
        {
            LexboxUrl = "https://lexbox.test/",
            LexboxUsername = "test",
            LexboxPassword = "test",
            ProjectStorageRoot = _tempDir,
            MediaFileAuthority = "media.test"
        };

        SetupDefaultMocks();
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            try { Directory.Delete(_tempDir, true); } catch { }
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_workerScope is not null)
        {
            if (_workerScope is IAsyncDisposable asyncScope)
            {
                await asyncScope.DisposeAsync();
            }
            else
            {
                _workerScope.Dispose();
            }
            _workerScope = null;
        }
    }

    private void SetupDefaultMocks()
    {
        _mockProjectLookup
            .Setup(s => s.GetProjectCode(_projectId))
            .ReturnsAsync(ProjectCode);

        _mockProjectLookup
            .Setup(s => s.IsCrdtProject(_projectId))
            .ReturnsAsync(false);

        _mockMetadataService
            .Setup(s => s.GetSyncBlockInfoAsync(_projectId))
            .ReturnsAsync((SyncBlockInfo?)null);

        _mockSendReceive
            .Setup(s => s.PendingCommitCount(It.IsAny<FwDataProject>(), ProjectCode))
            .ReturnsAsync(1);

        _mockSendReceive
            .Setup(s => s.Clone(It.IsAny<FwDataProject>(), ProjectCode))
            .Callback(() => _callSequence.Add(nameof(ISendReceiveService.Clone)))
            .ReturnsAsync(new SendReceiveHelpers.LfMergeBridgeResult("success"));

        _mockSendReceive
            .Setup(s => s.SendReceive(It.IsAny<FwDataProject>(), ProjectCode, null))
            .Callback(() => _callSequence.Add(nameof(ISendReceiveService.SendReceive)))
            .ReturnsAsync(new SendReceiveHelpers.LfMergeBridgeResult("success"));
    }

    private ServiceProvider BuildServiceProvider(
        SyncResult syncResult,
        bool authSuccess = true)
    {
        var services = new ServiceCollection();

        services.AddLogging();

        // Config
        services.AddSingleton<IOptions<FwHeadlessConfig>>(Options.Create(_config));

        // Required by SyncWorker ctor
        services.AddSingleton<ILogger<SyncWorker>>(NullLogger<SyncWorker>.Instance);
        services.AddSingleton(_mockSendReceive.Object);
        services.AddSingleton(_mockProjectLookup.Object);
        services.AddSingleton(_mockSyncJobStatus.Object);
        services.AddSingleton(_mockMetadataService.Object);

        // We don't want to hit EF/db in these tests.
        var mediaFileService = new Mock<MediaFileService>(MockBehavior.Strict, null!, Options.Create(_config), _mockSendReceive.Object);
        mediaFileService
            .Setup(s => s.SyncMediaFiles(It.IsAny<SIL.LCModel.LcmCache>()))
            .Callback(() => _callSequence.Add(nameof(MediaFileService.SyncMediaFiles)))
            .ReturnsAsync(new MediaFileService.MediaFileSyncResult([], []));
        mediaFileService
            .Setup(s => s.SyncMediaFiles(_projectId, It.IsAny<LcmCrdt.MediaServer.LcmMediaService>()))
            .Callback(() => _callSequence.Add(nameof(MediaFileService.SyncMediaFiles)))
            .Returns(Task.CompletedTask);
        services.AddSingleton(mediaFileService.Object);

        // NOTE: register HttpClientFactory and a mocked CrdtHttpSyncService after
        // AddLcmCrdtClientCore() so the test mock does not get overridden by
        // the real registration inside that helper.

        // Real FwDataFactory + in-memory project loader
        services.AddMemoryCache();
        services.AddTestFwDataBridge(mockProjectLoader: true);

        // LCM/CRDT stack is needed because SyncWorker calls services.OpenCrdtProject(...)
        services.AddLcmCrdtClientCore();
        services.Configure<LcmCrdtConfig>(c => c.ProjectPath = _tempDir);

        // Http auth check - register after AddLcmCrdtClientCore so our mock
        // isn't overridden by real registrations in that helper.
        var httpClientFactory = new Mock<IHttpClientFactory>(MockBehavior.Strict);
        httpClientFactory
            .Setup(f => f.CreateClient(FwHeadlessKernel.LexboxHttpClientName))
            .Returns(new HttpClient { BaseAddress = new Uri(_config.LexboxUrl) });
        services.AddSingleton(httpClientFactory.Object);

        var crdtHttpSyncService = new Mock<CrdtHttpSyncService>(MockBehavior.Strict, NullLogger<CrdtHttpSyncService>.Instance, new Mock<IRefitHttpServiceFactory>().Object, new Microsoft.Extensions.Caching.Memory.MemoryCache(new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions()));
        crdtHttpSyncService
            .Setup(s => s.TestAuth(It.IsAny<HttpClient>()))
            .Callback(() => _callSequence.Add(nameof(CrdtHttpSyncService.TestAuth)))
            .ReturnsAsync(authSuccess);
        services.AddSingleton(crdtHttpSyncService.Object);

        // Avoid pulling in the full media-sync stack; SyncWorker only needs an instance to pass through.
        services.AddSingleton<LcmCrdt.MediaServer.LcmMediaService>(_ =>
            new Mock<LcmCrdt.MediaServer.LcmMediaService>(MockBehavior.Loose, null!, null!, Options.Create(new SIL.Harmony.CrdtConfig()), null!, null!, NullLogger<LcmCrdt.MediaServer.LcmMediaService>.Instance).Object);

        // Use the default CrdtProjectsService registration from AddLcmCrdtClientCore

        // Mock sync service to avoid heavy sync.
        var syncService = new Mock<CrdtFwdataProjectSyncService>(MockBehavior.Strict, null!, NullLogger<CrdtFwdataProjectSyncService>.Instance, Options.Create(new SIL.Harmony.CrdtConfig()), null!, null!);
        
        // Mock GetProjectSnapshot
        syncService
            .Setup(s => s.GetProjectSnapshot(It.IsAny<FwDataProject>()))
            .Callback(() => _callSequence.Add(nameof(CrdtFwdataProjectSyncService.GetProjectSnapshot)))
            .ReturnsAsync((ProjectSnapshot?)null);

        // Update Sync expectation to accept ProjectSnapshot?
        syncService
            .Setup(s => s.Sync(It.IsAny<IMiniLcmApi>(), It.IsAny<FwDataMiniLcmApi>(), It.IsAny<ProjectSnapshot?>(), false))
            .Callback(() => _callSequence.Add(nameof(CrdtFwdataProjectSyncService.Sync)))
            .ReturnsAsync(syncResult);
        
        syncService
            .Setup(s => s.RegenerateProjectSnapshot(It.IsAny<IMiniLcmApi>(), It.IsAny<FwDataProject>(), false))
            .Callback(() => _callSequence.Add(nameof(CrdtFwdataProjectSyncService.RegenerateProjectSnapshot)))
            .Returns(Task.CompletedTask);
        services.AddSingleton(syncService.Object);

        // Spy harmony sync so we don't talk to a real LexBox server.
        services.AddSingleton<CrdtSyncService>(sp => new SpyCrdtSyncService(_callSequence));

        // Remaining deps needed by SyncWorker's constructor
        services.AddSingleton<LogSanitizerService>();
        services.AddSingleton<SafeLoggingProgress>();

        return services.BuildServiceProvider(validateScopes: true);
    }

    private static int IndexOfNth(IReadOnlyList<string> list, string value, int occurrence)
    {
        var count = 0;
        for (var i = 0; i < list.Count; i++)
        {
            if (list[i] != value) continue;
            count++;
            if (count == occurrence) return i;
        }
        return -1;
    }

    private SyncWorker CreateWorker(ServiceProvider sp)
    {
        // Create a scope so scoped services (LCM/CRDT) resolve correctly when
        // SyncWorker calls services.OpenCrdtProject(...).
        _workerScope?.Dispose();
        _workerScope = sp.CreateScope();
        return ActivatorUtilities.CreateInstance<SyncWorker>(_workerScope.ServiceProvider, _projectId);
    }

    [Fact]
    public async Task ExecuteSync_SuccessWithCrdtAndFwChanges_RegeneratesSnapshotAfterSendReceive()
    {
        var syncResult = new SyncResult(CrdtChanges: 5, FwdataChanges: 3);

        using var sp = BuildServiceProvider(syncResult);

        // Ensure the FW "project" exists so SetupFwData chooses Send/Receive not Clone
        Directory.CreateDirectory(FwDataProject.ProjectFolder);
        File.WriteAllText(FwDataProject.FilePath, "dummy");

        // Provide an in-memory FW cache for the loader
        sp.GetRequiredService<MockFwProjectLoader>().NewProject(FwDataProject, analysisWs: "en", vernacularWs: "fr");

        var worker = CreateWorker(sp);
        var result = await worker.ExecuteSync(CancellationToken.None);

        result.Status.Should().Be(SyncJobStatusEnum.Success);

        // Verify order (using nameof markers):
        // - Pre sync S/R (or Clone)
        // - CrdtFwdataProjectSyncService.Sync
        // - Post sync S/R
        // - RegenerateProjectSnapshot
        // - CrdtSyncService.SyncHarmonyProject
        _callSequence.Should().Contain(nameof(CrdtHttpSyncService.TestAuth));
        _callSequence.Should().Contain(nameof(CrdtFwdataProjectSyncService.GetProjectSnapshot));
        _callSequence.Should().Contain(nameof(CrdtFwdataProjectSyncService.Sync));
        _callSequence.Should().Contain(nameof(CrdtFwdataProjectSyncService.RegenerateProjectSnapshot));
        _callSequence.Should().Contain(nameof(CrdtSyncService.SyncHarmonyProject));

        var preSr = IndexOfNth(_callSequence, nameof(ISendReceiveService.SendReceive), 1);
        var getSnapshot = IndexOfNth(_callSequence, nameof(CrdtFwdataProjectSyncService.GetProjectSnapshot), 1);
        var sync = IndexOfNth(_callSequence, nameof(CrdtFwdataProjectSyncService.Sync), 1);
        var postSr = IndexOfNth(_callSequence, nameof(ISendReceiveService.SendReceive), 2);
        var regen = IndexOfNth(_callSequence, nameof(CrdtFwdataProjectSyncService.RegenerateProjectSnapshot), 1);
        var harmony = IndexOfNth(_callSequence, nameof(CrdtSyncService.SyncHarmonyProject), 1);

        preSr.Should().BeGreaterThanOrEqualTo(0);
        getSnapshot.Should().BeGreaterThan(preSr);
        sync.Should().BeGreaterThan(getSnapshot);
        postSr.Should().BeGreaterThan(sync);
        regen.Should().BeGreaterThan(postSr);
        harmony.Should().BeGreaterThan(regen);
    }

    [Fact]
    public async Task ExecuteSync_CrdtChangesNoFwChanges_RegeneratesSnapshotWithoutPostSendReceive()
    {
        var syncResult = new SyncResult(CrdtChanges: 5, FwdataChanges: 0);

        using var sp = BuildServiceProvider(syncResult);
        Directory.CreateDirectory(FwDataProject.ProjectFolder);
        File.WriteAllText(FwDataProject.FilePath, "dummy");
        sp.GetRequiredService<MockFwProjectLoader>().NewProject(FwDataProject, analysisWs: "en", vernacularWs: "fr");

        var worker = CreateWorker(sp);
        var result = await worker.ExecuteSync(CancellationToken.None);

        result.Status.Should().Be(SyncJobStatusEnum.Success);

        // Post-CRDT S/R should not be called when there are no FW changes
        IndexOfNth(_callSequence, nameof(ISendReceiveService.SendReceive), 2).Should().Be(-1);
        _callSequence.Should().Contain(nameof(CrdtFwdataProjectSyncService.RegenerateProjectSnapshot));
    }

    [Fact]
    public async Task ExecuteSync_NoCrdtChanges_DoesNotRegenerateSnapshot()
    {
        var syncResult = new SyncResult(CrdtChanges: 0, FwdataChanges: 0);

        using var sp = BuildServiceProvider(syncResult);
        Directory.CreateDirectory(FwDataProject.ProjectFolder);
        File.WriteAllText(FwDataProject.FilePath, "dummy");
        sp.GetRequiredService<MockFwProjectLoader>().NewProject(FwDataProject, analysisWs: "en", vernacularWs: "fr");

        var worker = CreateWorker(sp);
        var result = await worker.ExecuteSync(CancellationToken.None);

        result.Status.Should().Be(SyncJobStatusEnum.Success);

        // Snapshot regeneration should be skipped when there are no CRDT changes
        _callSequence.Should().NotContain(nameof(CrdtFwdataProjectSyncService.RegenerateProjectSnapshot));
    }

    [Fact]
    public async Task ExecuteSync_PostSendReceiveFails_DoesNotRegenerateSnapshot()
    {
        var syncResult = new SyncResult(CrdtChanges: 5, FwdataChanges: 3);

        _mockSendReceive
            .SetupSequence(s => s.SendReceive(It.IsAny<FwDataProject>(), ProjectCode, null))
            .ReturnsAsync(new SendReceiveHelpers.LfMergeBridgeResult("success"))
            .ReturnsAsync(new SendReceiveHelpers.LfMergeBridgeResult("Some error occurred", ProgressHelper.CreateErrorProgress()));

        using var sp = BuildServiceProvider(syncResult);
        Directory.CreateDirectory(FwDataProject.ProjectFolder);
        File.WriteAllText(FwDataProject.FilePath, "dummy");
        sp.GetRequiredService<MockFwProjectLoader>().NewProject(FwDataProject, analysisWs: "en", vernacularWs: "fr");

        var worker = CreateWorker(sp);
        var result = await worker.ExecuteSync(CancellationToken.None);

        result.Status.Should().Be(SyncJobStatusEnum.SendReceiveFailed);
        _callSequence.Should().NotContain(nameof(CrdtFwdataProjectSyncService.RegenerateProjectSnapshot));
    }

    [Fact]
    public async Task ExecuteSync_PostSendReceiveRollback_BlocksProjectAndDoesNotRegenerateSnapshot()
    {
        var syncResult = new SyncResult(CrdtChanges: 5, FwdataChanges: 3);

        _mockSendReceive
            .SetupSequence(s => s.SendReceive(It.IsAny<FwDataProject>(), ProjectCode, null))
            .ReturnsAsync(new SendReceiveHelpers.LfMergeBridgeResult("success"))
            .ReturnsAsync(new SendReceiveHelpers.LfMergeBridgeResult("Rolling back... validation failed", ProgressHelper.CreateErrorProgress()));

        using var sp = BuildServiceProvider(syncResult);
        Directory.CreateDirectory(FwDataProject.ProjectFolder);
        File.WriteAllText(FwDataProject.FilePath, "dummy");
        sp.GetRequiredService<MockFwProjectLoader>().NewProject(FwDataProject, analysisWs: "en", vernacularWs: "fr");

        var worker = CreateWorker(sp);
        var result = await worker.ExecuteSync(CancellationToken.None);

        result.Status.Should().Be(SyncJobStatusEnum.SyncBlocked);
        result.Error.Should().Contain("rollback");

        _mockMetadataService.Verify(
            s => s.BlockFromSyncAsync(_projectId, It.Is<string>(msg => msg.Contains("Rollback"))),
            Times.Once);

        _callSequence.Should().NotContain(nameof(CrdtFwdataProjectSyncService.RegenerateProjectSnapshot));
    }

    [Fact]
    public async Task ExecuteSync_PreSendReceiveRollback_BlocksProjectAndDoesNotSync()
    {
        // Force SetupFwData's pre-S/R to fail with rollback output
        _mockSendReceive
            .Setup(s => s.PendingCommitCount(It.IsAny<FwDataProject>(), ProjectCode))
            .ReturnsAsync(1);
        _mockSendReceive
            .Setup(s => s.SendReceive(It.IsAny<FwDataProject>(), ProjectCode, null))
            .ReturnsAsync(new SendReceiveHelpers.LfMergeBridgeResult("Rolling back... validation error", ProgressHelper.CreateErrorProgress()));

        var syncResult = new SyncResult(CrdtChanges: 0, FwdataChanges: 0);
        using var sp = BuildServiceProvider(syncResult);
        Directory.CreateDirectory(FwDataProject.ProjectFolder);
        File.WriteAllText(FwDataProject.FilePath, "dummy");
        sp.GetRequiredService<MockFwProjectLoader>().NewProject(FwDataProject, analysisWs: "en", vernacularWs: "fr");

        var worker = CreateWorker(sp);
        var result = await worker.ExecuteSync(CancellationToken.None);

        result.Status.Should().Be(SyncJobStatusEnum.SyncBlocked);
        result.Error.Should().Contain("rollback");

        _mockMetadataService.Verify(
            s => s.BlockFromSyncAsync(_projectId, It.Is<string>(msg => msg.Contains("Rollback"))),
            Times.Once);

        _callSequence.Should().NotContain(nameof(CrdtFwdataProjectSyncService.Sync));
        _callSequence.Should().NotContain(nameof(CrdtFwdataProjectSyncService.RegenerateProjectSnapshot));
    }

    [Fact]
    public async Task ExecuteSync_PreSendReceiveFailsWithoutRollback_ReturnsSendReceiveFailed()
    {
        _mockSendReceive
            .Setup(s => s.PendingCommitCount(It.IsAny<FwDataProject>(), ProjectCode))
            .ReturnsAsync(1);
        _mockSendReceive
            .Setup(s => s.SendReceive(It.IsAny<FwDataProject>(), ProjectCode, null))
            .ReturnsAsync(new SendReceiveHelpers.LfMergeBridgeResult("network error", ProgressHelper.CreateErrorProgress()));

        var syncResult = new SyncResult(CrdtChanges: 0, FwdataChanges: 0);
        using var sp = BuildServiceProvider(syncResult);
        Directory.CreateDirectory(FwDataProject.ProjectFolder);
        File.WriteAllText(FwDataProject.FilePath, "dummy");
        sp.GetRequiredService<MockFwProjectLoader>().NewProject(FwDataProject, analysisWs: "en", vernacularWs: "fr");

        var worker = CreateWorker(sp);
        var result = await worker.ExecuteSync(CancellationToken.None);

        result.Status.Should().Be(SyncJobStatusEnum.SendReceiveFailed);

        _mockMetadataService.Verify(
            s => s.BlockFromSyncAsync(It.IsAny<Guid>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteSync_ProjectNotFound_ReturnsProjectNotFound()
    {
        _mockProjectLookup
            .Setup(s => s.GetProjectCode(_projectId))
            .ReturnsAsync((string?)null);

        var syncResult = new SyncResult(CrdtChanges: 0, FwdataChanges: 0);
        using var sp = BuildServiceProvider(syncResult);

        var worker = CreateWorker(sp);
        var result = await worker.ExecuteSync(CancellationToken.None);

        result.Status.Should().Be(SyncJobStatusEnum.ProjectNotFound);
    }

    [Fact]
    public async Task ExecuteSync_AuthFails_ReturnsUnableToAuthenticate()
    {
        var syncResult = new SyncResult(CrdtChanges: 0, FwdataChanges: 0);
        using var sp = BuildServiceProvider(syncResult, authSuccess: false);

        var worker = CreateWorker(sp);
        var result = await worker.ExecuteSync(CancellationToken.None);

        result.Status.Should().Be(SyncJobStatusEnum.UnableToAuthenticate);
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

internal sealed class SpyCrdtSyncService(List<string> callSequence)
    : CrdtSyncService(null!, new Mock<IHttpClientFactory>().Object, null!, null!, NullLogger<CrdtSyncService>.Instance)
{
    public override Task SyncHarmonyProject()
    {
        callSequence.Add(nameof(CrdtSyncService.SyncHarmonyProject));
        return Task.CompletedTask;
    }
}

