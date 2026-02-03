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
using static Testing.FwHeadless.Services.SyncStep;

namespace Testing.FwHeadless.Services;

internal enum SyncStep
{
    TestAuth,
    CheckBlocked,
    Clone,
    PreSendReceive,
    PostSendReceive,
    MediaSyncFwData,
    MediaSyncCrdt,
    GetSnapshot,
    Sync,
    Import,
    RegenerateSnapshot,
    HarmonySync
}

internal sealed class SyncWorkerTestHarness : IDisposable
{
    public Guid ProjectId { get; } = Guid.NewGuid();
    public string ProjectCode { get; } = "test-project";

    public List<SyncStep> Steps { get; } = [];

    public Mock<ISendReceiveService> SendReceiveMock { get; } = new();
    public Mock<IProjectLookupService> ProjectLookupMock { get; } = new();
    public Mock<ISyncJobStatusService> SyncJobStatusMock { get; } = new();
    public Mock<IProjectMetadataService> MetadataServiceMock { get; } = new();

    public FwHeadlessConfig Config { get; }
    public string TempDir { get; }

    public string ProjectFolder => Config.GetProjectFolder(ProjectCode, ProjectId);
    public FwDataProject FwDataProject => Config.GetFwDataProject(ProjectCode, ProjectId);

    private bool _didCrdtSyncOrImport;

    public SyncWorkerTestHarness()
    {
        TempDir = Path.Combine(Path.GetTempPath(), nameof(SyncWorkerTests), Guid.NewGuid().ToString());
        Directory.CreateDirectory(TempDir);

        Config = new FwHeadlessConfig
        {
            LexboxUrl = "https://lexbox.test/",
            LexboxUsername = "test",
            LexboxPassword = "test",
            ProjectStorageRoot = TempDir,
            MediaFileAuthority = "media.test"
        };

        SetupDefaultMocks();
    }

    public void Dispose()
    {
        if (Directory.Exists(TempDir))
        {
            try { Directory.Delete(TempDir, true); } catch { }
        }
    }

    public void SetProjectCode(string? projectCode)
    {
        ProjectLookupMock
            .Setup(s => s.GetProjectCode(ProjectId))
            .ReturnsAsync(projectCode);
    }

    public void SetIsCrdtProject(bool isCrdtProject)
    {
        ProjectLookupMock
            .Setup(s => s.IsCrdtProject(ProjectId))
            .ReturnsAsync(isCrdtProject);
    }

    public void SetSyncBlockedInfo(SyncBlockedInfo? blockInfo)
    {
        MetadataServiceMock
            .Setup(s => s.GetSyncBlockedInfoAsync(ProjectId))
            .Callback(() => Steps.Add(CheckBlocked))
            .ReturnsAsync(blockInfo);
    }

    public void SetPendingCommitCount(int pendingCommitCount)
    {
        SendReceiveMock
            .Setup(s => s.PendingCommitCount(It.IsAny<FwDataProject>(), ProjectCode))
            .ReturnsAsync(pendingCommitCount);
    }

    public void SetSendReceiveResults(params SendReceiveHelpers.LfMergeBridgeResult[] results)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(results.Length);

        var seq = SendReceiveMock.SetupSequence(s => s.SendReceive(It.IsAny<FwDataProject>(), ProjectCode, null));
        foreach (var result in results)
        {
            var captured = result;
            seq = seq.Returns(() =>
            {
                RecordSendReceive();
                return Task.FromResult(captured);
            });
        }
    }

    public async Task<SyncJobResult> RunAsync(
        SyncResult syncResult,
        bool authSuccess = true,
        bool snapshotExists = true,
        bool setupFwDataProject = true,
        bool createFwDataFile = true,
        bool onlyHarmony = false)
    {
        Steps.Clear();
        _didCrdtSyncOrImport = false;

        using var sp = BuildServiceProvider(syncResult, authSuccess, snapshotExists);
        if (setupFwDataProject)
        {
            SetupFwDataProject(sp, createFwDataFile);
        }

        await using var scope = sp.CreateAsyncScope();
        var worker = ActivatorUtilities.CreateInstance<SyncWorker>(scope.ServiceProvider, ProjectId);
        return await worker.ExecuteSync(CancellationToken.None, onlyHarmony);
    }

    private void SetupDefaultMocks()
    {
        SetProjectCode(ProjectCode);
        SetIsCrdtProject(false);
        SetSyncBlockedInfo(null);
        SetPendingCommitCount(1);

        SendReceiveMock
            .Setup(s => s.Clone(It.IsAny<FwDataProject>(), ProjectCode))
            .Callback(() => Steps.Add(Clone))
            .ReturnsAsync(new SendReceiveHelpers.LfMergeBridgeResult("success"));

        SendReceiveMock
            .Setup(s => s.SendReceive(It.IsAny<FwDataProject>(), ProjectCode, null))
            .Callback(RecordSendReceive)
            .ReturnsAsync(new SendReceiveHelpers.LfMergeBridgeResult("success"));
    }

    private void RecordSendReceive()
    {
        Steps.Add(_didCrdtSyncOrImport ? PostSendReceive : PreSendReceive);
    }

    private ServiceProvider BuildServiceProvider(
        SyncResult syncResult,
        bool authSuccess = true,
        bool snapshotExists = true)
    {
        var services = new ServiceCollection();

        services.AddLogging();

        services.AddSingleton(Options.Create(Config));

        services.AddSingleton<ILogger<SyncWorker>>(NullLogger<SyncWorker>.Instance);
        services.AddSingleton(SendReceiveMock.Object);
        services.AddSingleton(ProjectLookupMock.Object);
        services.AddSingleton(SyncJobStatusMock.Object);
        services.AddSingleton(MetadataServiceMock.Object);

        // Mock media sync; the real one pulls in extra infrastructure we don't want in unit tests.
        var mediaFileService = new Mock<MediaFileService>(MockBehavior.Strict, null!, Options.Create(Config), SendReceiveMock.Object);
        mediaFileService
            .Setup(s => s.SyncMediaFiles(It.IsAny<SIL.LCModel.LcmCache>()))
            .Callback(() => Steps.Add(MediaSyncFwData))
            .ReturnsAsync(new MediaFileService.MediaFileSyncResult([], []));
        mediaFileService
            .Setup(s => s.SyncMediaFiles(ProjectId, It.IsAny<LcmCrdt.MediaServer.LcmMediaService>()))
            .Callback(() => Steps.Add(MediaSyncCrdt))
            .Returns(Task.CompletedTask);
        services.AddSingleton(mediaFileService.Object);

        services.AddMemoryCache();
        services.AddTestFwDataBridge(mockProjectLoader: true);

        // SyncWorker needs the CRDT registrations (and OpenCrdtProject extension).
        services.AddLcmCrdtClientCore();
        services.Configure<LcmCrdtConfig>(c => c.ProjectPath = TempDir);

        // Register after AddLcmCrdtClientCore so our mocks win over any defaults.
        var httpClientFactory = new Mock<IHttpClientFactory>(MockBehavior.Strict);
        httpClientFactory
            .Setup(f => f.CreateClient(FwHeadlessKernel.LexboxHttpClientName))
            .Returns(new HttpClient { BaseAddress = new Uri(Config.LexboxUrl) });
        services.AddSingleton(httpClientFactory.Object);

        var crdtHttpSyncService = new Mock<CrdtHttpSyncService>(MockBehavior.Strict, NullLogger<CrdtHttpSyncService>.Instance, new Mock<IRefitHttpServiceFactory>().Object, new Microsoft.Extensions.Caching.Memory.MemoryCache(new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions()));
        crdtHttpSyncService
            .Setup(s => s.TestAuth(It.IsAny<HttpClient>()))
            .Callback(() => Steps.Add(TestAuth))
            .ReturnsAsync(authSuccess);
        services.AddSingleton(crdtHttpSyncService.Object);

        // SyncWorker only passes this through to MediaFileService.
        services.AddSingleton(_ =>
            new Mock<LcmCrdt.MediaServer.LcmMediaService>(MockBehavior.Loose, null!, null!, Options.Create(new SIL.Harmony.CrdtConfig()), null!, null!, NullLogger<LcmCrdt.MediaServer.LcmMediaService>.Instance).Object);

        var syncService = new Mock<CrdtFwdataProjectSyncService>(
            MockBehavior.Strict,
            null!,
            NullLogger<CrdtFwdataProjectSyncService>.Instance,
            null!,
            null!);

        syncService
            .Setup(s => s.Sync(It.IsAny<IMiniLcmApi>(), It.IsAny<FwDataMiniLcmApi>(), It.IsAny<ProjectSnapshot>(), false))
            .Callback(() =>
            {
                _didCrdtSyncOrImport = true;
                Steps.Add(Sync);
            })
            .ReturnsAsync(syncResult);

        syncService
            .Setup(s => s.Import(It.IsAny<IMiniLcmApi>(), It.IsAny<FwDataMiniLcmApi>(), false))
            .Callback(() =>
            {
                _didCrdtSyncOrImport = true;
                Steps.Add(Import);
            })
            .ReturnsAsync(syncResult);

        services.AddSingleton(syncService.Object);

        var snapshotService = new Mock<ProjectSnapshotService>(MockBehavior.Strict, Options.Create(new SIL.Harmony.CrdtConfig()));
        snapshotService
            .Setup(s => s.GetProjectSnapshot(It.IsAny<FwDataProject>()))
            .Callback(() => Steps.Add(GetSnapshot))
            .ReturnsAsync(snapshotExists ? ProjectSnapshot.Empty : null);
        snapshotService
            .Setup(s => s.RegenerateProjectSnapshot(It.IsAny<IMiniLcmReadApi>(), It.IsAny<FwDataProject>(), false))
            .Callback(() => Steps.Add(RegenerateSnapshot))
            .Returns(Task.CompletedTask);
        services.AddSingleton(snapshotService.Object);

        services.AddSingleton<CrdtSyncService>(_ => new SpyCrdtSyncService(Steps));

        services.AddSingleton<LogSanitizerService>();
        services.AddSingleton<SafeLoggingProgress>();

        return services.BuildServiceProvider(validateScopes: true);
    }

    private void SetupFwDataProject(ServiceProvider sp, bool createFile = true)
    {
        Directory.CreateDirectory(FwDataProject.ProjectFolder);
        if (createFile)
        {
            File.WriteAllText(FwDataProject.FilePath, "dummy");
        }
        sp.GetRequiredService<MockFwProjectLoader>().NewProject(FwDataProject, analysisWs: "en", vernacularWs: "fr");
    }

    private sealed class SpyCrdtSyncService(List<SyncStep> steps)
        : CrdtSyncService(null!, new Mock<IHttpClientFactory>().Object, null!, null!, NullLogger<CrdtSyncService>.Instance)
    {
        public override Task SyncHarmonyProject()
        {
            steps.Add(HarmonySync);
            return Task.CompletedTask;
        }
    }
}
