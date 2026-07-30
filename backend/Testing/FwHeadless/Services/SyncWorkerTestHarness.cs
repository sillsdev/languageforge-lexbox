using System.Text.Json;
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
using MiniLcm.Models;
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
    PrepareMergeBase,
    HarmonySync
}

internal sealed class SyncWorkerTestHarness : IDisposable
{
    public Guid ProjectId { get; } = Guid.NewGuid();
    public string ProjectCode { get; } = "test-project";

    public List<SyncStep> Steps { get; } = [];

    /// <summary>The merge base the CRDT sync was actually handed, so tests can check which one won.</summary>
    public ProjectSnapshot? SyncedAgainstMergeBase { get; private set; }

    public Mock<ISendReceiveService> SendReceiveMock { get; } = new();
    public Mock<IProjectLookupService> ProjectLookupMock { get; } = new();
    public Mock<ISyncJobStatusService> SyncJobStatusMock { get; } = new();
    public Mock<IProjectMetadataService> MetadataServiceMock { get; } = new();

    public FwHeadlessConfig Config { get; }

    public string ProjectFolder => Config.GetProjectFolder(ProjectCode, ProjectId);
    public FwDataProject FwDataProject => Config.GetFwDataProject(ProjectCode, ProjectId);

    private bool _didCrdtSyncOrImport;
    private bool _createFwDataFileAfterClone = true;
    private Func<SyncResult>? _syncBehaviour;
    private string? _syncWritesPartOfSpeech;

    public string CrdtDbPath => Config.GetCrdtFile(ProjectCode, ProjectId);
    public string MergeBasePath => ProjectSnapshotService.SnapshotPath(FwDataProject);
    public string StagedCrdtDbPath => SyncStagingService.StagedDbPath(CrdtDbPath);
    public string StagedMergeBasePath => SyncStagingService.StagedMergeBasePath(FwDataProject);
    public string SyncJournalPath => SyncJournal.JournalPath(FwDataProject);

    public SyncWorkerTestHarness()
    {
        var projectStorageRoot = Path.Combine(Path.GetTempPath(), nameof(SyncWorkerTests), Guid.NewGuid().ToString());
        Directory.CreateDirectory(projectStorageRoot);

        Config = new FwHeadlessConfig
        {
            LexboxUrl = "https://test.lexbox.com/",
            LexboxUsername = "test",
            LexboxPassword = "test",
            ProjectStorageRoot = projectStorageRoot,
            MediaFileAuthority = "media.test"
        };

        SetupDefaultMocks();
    }

    public void Dispose()
    {
        if (Directory.Exists(Config.ProjectStorageRoot))
        {
            try { Directory.Delete(Config.ProjectStorageRoot, true); } catch { }
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
            .Setup(s => s.PendingCommitCountIncoming(It.IsAny<FwDataProject>(), ProjectCode))
            .ReturnsAsync(pendingCommitCount);
        SendReceiveMock
            .Setup(s => s.PendingCommitCountBothWays(It.IsAny<FwDataProject>(), ProjectCode))
            .ReturnsAsync(pendingCommitCount);
    }

    public void SetCloneResult(SendReceiveHelpers.LfMergeBridgeResult result)
    {
        SendReceiveMock
            .Setup(s => s.Clone(It.IsAny<FwDataProject>(), ProjectCode))
            .Callback(() =>
            {
                Steps.Add(Clone);
                if (_createFwDataFileAfterClone) EnsureFwDataFileExists();
            })
            .ReturnsAsync(result);
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

    /// <summary>
    /// Makes the mocked CRDT sync throw instead of returning, to stand in for a sync that dies part way through.
    /// Combines with <see cref="SetSyncWritesPartOfSpeech"/> to die after having written to the CRDT.
    /// </summary>
    public void SetSyncThrows(Exception exception)
    {
        _syncBehaviour = () => throw exception;
    }

    /// <summary>
    /// Makes the mocked CRDT sync write to the CRDT api it is handed, standing in for the fwdata-to-CRDT pass. This
    /// is what has to be either committed with a matching merge base or thrown away entirely.
    /// </summary>
    public void SetSyncWritesPartOfSpeech(string name)
    {
        _syncWritesPartOfSpeech = name;
    }

    public void WriteSyncJournal(SyncJournalState state)
    {
        Directory.CreateDirectory(ProjectFolder);
        File.WriteAllText(SyncJournalPath, JsonSerializer.Serialize(
            new SyncJournal(state, StagedCrdtDbPath, StagedMergeBasePath, CrdtDbPath, MergeBasePath)));
    }

    public async Task<SyncJobResult> RunAsync(
        SyncResult syncResult,
        bool authSuccess = true,
        bool snapshotExists = true,
        bool setupFwDataProject = true,
        bool createFwDataFileBeforeSync = true,
        bool createFwDataFileAfterClone = true,
        bool onlyHarmony = false,
        bool crdtProjectExists = true,
        bool leaveUnrecordedSyncCommit = false,
        bool leaveUnrecordedUserCommit = false)
    {
        _didCrdtSyncOrImport = false;
        _createFwDataFileAfterClone = createFwDataFileAfterClone;
        SyncedAgainstMergeBase = null;

        using var sp = BuildServiceProvider(syncResult, authSuccess);
        if (setupFwDataProject)
        {
            SetupFwDataProject(sp, createFwDataFileBeforeSync);
        }
        // A project that has synced before has a CRDT database. Create it up front rather than letting the worker
        // create an empty one: the sync that would populate it is mocked out here, and the merge base can't be read
        // from a project with no writing systems.
        var lastMergeBase = crdtProjectExists
            ? await SetupCrdtProject(sp, leaveUnrecordedSyncCommit, leaveUnrecordedUserCommit)
            : ProjectSnapshot.Empty;
        if (snapshotExists) WriteMergeBase(lastMergeBase);

        // After the arrange work, which uses the same spied services.
        Steps.Clear();
        await using var scope = sp.CreateAsyncScope();
        var worker = ActivatorUtilities.CreateInstance<SyncWorker>(scope.ServiceProvider, ProjectId);
        return await worker.ExecuteSync(CancellationToken.None, onlyHarmony);
    }

    /// <summary>
    /// Creates the CRDT database the worker will find, and returns the merge base a previous successful sync would
    /// have left behind for it. With <paramref name="leaveUnrecordedSyncCommit"/> the database gets one more
    /// sync-authored commit after that base, which is what an interrupted sync used to leave behind.
    /// <paramref name="leaveUnrecordedUserCommit"/> does the same with a person's commit, which is normal.
    /// </summary>
    private async Task<ProjectSnapshot> SetupCrdtProject(ServiceProvider sp, bool leaveUnrecordedSyncCommit, bool leaveUnrecordedUserCommit)
    {
        Directory.CreateDirectory(ProjectFolder);
        await using var scope = sp.CreateAsyncScope();
        var fwProjectId = scope.ServiceProvider.GetRequiredService<FwDataFactory>()
            .GetFwDataMiniLcmApi(FwDataProject, false).ProjectId;
        var crdtProject = await scope.ServiceProvider.GetRequiredService<CrdtProjectsService>()
            .CreateProject(new("crdt", "crdt", Id: ProjectId, Path: ProjectFolder, FwProjectId: fwProjectId));

        await using var projectScope = sp.CreateAsyncScope();
        var crdtApi = await projectScope.ServiceProvider.OpenCrdtProject(crdtProject);
        // Entry queries need a default vernacular writing system, so a project without one can't produce a merge base.
        await crdtApi.CreateWritingSystem(new WritingSystem
        {
            Id = Guid.NewGuid(),
            WsId = "en",
            Name = "English",
            Abbreviation = "en",
            Font = "Arial",
            Type = WritingSystemType.Vernacular
        });
        var mergeBase = await projectScope.ServiceProvider.GetRequiredService<ProjectSnapshotService>().TakeMergeBase(crdtApi);

        if (leaveUnrecordedSyncCommit)
        {
            await crdtApi.CreatePartOfSpeech(new PartOfSpeech { Id = Guid.NewGuid(), Name = { { "en", "Noun" } } });
        }

        if (leaveUnrecordedUserCommit)
        {
            var interceptor = projectScope.ServiceProvider.GetRequiredService<CommitMetadataInterceptor>();
            using (interceptor.Intercept(metadata => metadata.AuthorName = "A Person"))
            {
                await crdtApi.CreatePartOfSpeech(new PartOfSpeech { Id = Guid.NewGuid(), Name = { { "en", "Verb" } } });
            }
        }

        return mergeBase;
    }

    /// <summary>
    /// Writes a merge base straight to disk, so tests can set up the state a previous sync would have left.
    /// </summary>
    public void WriteMergeBase(ProjectSnapshot mergeBase)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(MergeBasePath)!);
        File.WriteAllText(MergeBasePath, JsonSerializer.Serialize(mergeBase));
    }

    public ProjectSnapshot? ReadMergeBase()
    {
        if (!File.Exists(MergeBasePath)) return null;
        return JsonSerializer.Deserialize<ProjectSnapshot>(File.ReadAllText(MergeBasePath));
    }

    /// <summary>
    /// Opens the project's real CRDT database from scratch, so tests read what is actually on disk after a run.
    /// </summary>
    public async Task<string[]> ReadCrdtPartsOfSpeech()
    {
        using var sp = BuildServiceProvider(new SyncResult(0, 0));
        await using var scope = sp.CreateAsyncScope();
        var api = await scope.ServiceProvider.OpenCrdtProject(new CrdtProject("crdt", CrdtDbPath));
        var partsOfSpeech = await api.GetPartsOfSpeech().ToArrayAsync();
        return [.. partsOfSpeech.Select(pos => pos.Name["en"])];
    }

    public void AssertNothingStaged()
    {
        File.Exists(StagedCrdtDbPath).Should().BeFalse("the staged CRDT database should be gone");
        File.Exists(StagedMergeBasePath).Should().BeFalse("the staged merge base should be gone");
        File.Exists(SyncJournalPath).Should().BeFalse("the sync journal should be gone");
    }

    private void SetupDefaultMocks()
    {
        SetProjectCode(ProjectCode);
        SetIsCrdtProject(false);
        SetSyncBlockedInfo(null);
        SetPendingCommitCount(1);
        SetCloneResult(new SendReceiveHelpers.LfMergeBridgeResult("success"));

        SendReceiveMock
            .Setup(s => s.SendReceive(It.IsAny<FwDataProject>(), ProjectCode, null))
            .Callback(RecordSendReceive)
            .ReturnsAsync(new SendReceiveHelpers.LfMergeBridgeResult("success"));
    }

    private void RecordSendReceive()
    {
        Steps.Add(_didCrdtSyncOrImport ? PostSendReceive : PreSendReceive);
    }

    private void EnsureFwDataFileExists()
    {
        EnsureFwDataFileExists(FwDataProject);
    }

    public void EnsureFwDataFileExists(FwDataProject fwDataProject)
    {
        if (File.Exists(fwDataProject.FilePath)) return;
        Directory.CreateDirectory(fwDataProject.ProjectFolder);
        File.WriteAllText(fwDataProject.FilePath, "<languageproject />");
    }

    private ServiceProvider BuildServiceProvider(
        SyncResult syncResult,
        bool authSuccess = true)
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
        services.Configure<LcmCrdtConfig>(c =>
        {
            c.ProjectPath = Config.ProjectStorageRoot;
            // Same as FwHeadless's appsettings: the stale-merge-base check needs to be able to recognise the sync's
            // own commits, which it does by this author name.
            c.DefaultAuthorForCommits = "FieldWorks";
        });

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
            null!,
            null!);

        syncService
            .Setup(s => s.Sync(It.IsAny<IMiniLcmApi>(), It.IsAny<FwDataMiniLcmApi>(), It.IsAny<ProjectSnapshot>(), false))
            .Returns(async (IMiniLcmApi crdtApi, FwDataMiniLcmApi _, ProjectSnapshot mergeBase, bool _) =>
            {
                _didCrdtSyncOrImport = true;
                SyncedAgainstMergeBase = mergeBase;
                Steps.Add(Sync);
                if (_syncWritesPartOfSpeech is not null)
                {
                    await crdtApi.CreatePartOfSpeech(new PartOfSpeech { Id = Guid.NewGuid(), Name = { { "en", _syncWritesPartOfSpeech } } });
                }
                return _syncBehaviour?.Invoke() ?? syncResult;
            });

        syncService
            .Setup(s => s.Import(It.IsAny<IMiniLcmApi>(), It.IsAny<FwDataMiniLcmApi>(), false))
            .Returns(() =>
            {
                _didCrdtSyncOrImport = true;
                Steps.Add(Import);
                return Task.FromResult(_syncBehaviour?.Invoke() ?? syncResult);
            });

        services.AddSingleton(syncService.Object);

        // The real snapshot and staging services, so these tests cover what actually reaches the disk. Only the
        // CRDT<->fwdata sync itself is mocked; that algorithm is tested in FwLiteProjectSync.Tests.
        services.AddScoped<ProjectSnapshotService>(sp => ActivatorUtilities.CreateInstance<SpyProjectSnapshotService>(sp, Steps));
        services.AddScoped<MergeBaseHealthService>();
        services.AddScoped<SyncStagingService>();

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
            EnsureFwDataFileExists();
        }
        sp.GetRequiredService<MockFwProjectLoader>().NewProject(FwDataProject, analysisWs: "en", vernacularWs: "fr");
    }

    private sealed class SpyProjectSnapshotService(
        List<SyncStep> steps,
        IOptions<SIL.Harmony.CrdtConfig> crdtConfig,
        CrdtHistoryHeadService historyHeadService)
        : ProjectSnapshotService(crdtConfig, historyHeadService)
    {
        public override Task<ProjectSnapshot?> GetProjectSnapshot(FwDataProject project)
        {
            steps.Add(GetSnapshot);
            return base.GetProjectSnapshot(project);
        }

        public override Task<ProjectSnapshot> TakeMergeBase(IMiniLcmReadApi crdtApi)
        {
            steps.Add(PrepareMergeBase);
            return base.TakeMergeBase(crdtApi);
        }
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
