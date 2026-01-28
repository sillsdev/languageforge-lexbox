using System.Runtime.CompilerServices;
using FwDataMiniLcmBridge;
using FwDataMiniLcmBridge.Api;
using FwDataMiniLcmBridge.LcmUtils;
using LcmCrdt;
using LexCore.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MiniLcm.Models;

namespace FwLiteProjectSync.Tests.Fixtures;

public class ExtraWritingSystemsSyncFixture : SyncFixture
{
    private static readonly string[] ExtraVernacularWritingSystems = ["es", "fr"];
    // "en", "es", "fr" = sorted alphabetically.
    // Otherwise, headwords would differ between fwdata and crdt.
    // See: https://github.com/sillsdev/languageforge-lexbox/issues/1284
    public static readonly string[] VernacularWritingSystems = [
        DefaultVernacularWritingSystem,
        .. ExtraVernacularWritingSystems,
    ];

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();

        foreach (var ws in ExtraVernacularWritingSystems)
        {
            await FwDataApi.CreateWritingSystem(new WritingSystem
            {
                Id = Guid.NewGuid(),
                WsId = ws,
                Name = ws,
                Abbreviation = ws,
                Font = "Arial",
                Type = WritingSystemType.Vernacular
            });
        }

        // Crdt data doesn't strictly require writing-systems to exist in order for them to be used.
        // However, a (default) vernacular writing system is required in order to query for entries.
        // This is not part of SyncFixture, because our core sync integration tests benefit from having a CRDT project that's as empty as possible.
        var firstVernacularWs = (await FwDataApi.GetWritingSystems()).Vernacular.First();
        await CrdtApi.CreateWritingSystem(firstVernacularWs);
    }
}

public class SyncFixture : IAsyncLifetime
{
    private readonly AsyncServiceScope _services;

    public CrdtFwdataProjectSyncService SyncService =>
        _services.ServiceProvider.GetRequiredService<CrdtFwdataProjectSyncService>();
    public IServiceProvider Services => _services.ServiceProvider;
    protected static readonly string DefaultVernacularWritingSystem = "en";
    private readonly string _projectName;
    private readonly string _projectFolder;
    private readonly IDisposable _cleanup;
    private static readonly Lock _preCleanupLock = new();
    private static readonly HashSet<string> _preCleanupDone = [];

    public static SyncFixture Create([CallerMemberName] string projectName = "", [CallerMemberName] string projectFolder = "") => new(projectName, projectFolder);

    private SyncFixture(string projectName, string projectFolder)
    {
        _projectName = projectName;
        _projectFolder = projectFolder;
        var crdtServices = new ServiceCollection()
            .AddSyncServices(projectFolder);
        var rootServiceProvider = crdtServices.BuildServiceProvider();
        _cleanup = Defer.Action(() => rootServiceProvider.Dispose());
        _services = rootServiceProvider.CreateAsyncScope();
    }

    public SyncFixture() : this("sena-3_" + Guid.NewGuid().ToString().Split("-")[0], "FwLiteSyncFixture")
    {
    }

    public virtual async Task InitializeAsync()
    {
        lock (_preCleanupLock)
        {
            if (!_preCleanupDone.Contains(_projectFolder))
            {
                _preCleanupDone.Add(_projectFolder);
                if (Path.Exists(_projectFolder))
                {
                    Directory.Delete(_projectFolder, true);
                }
            }
        }

        var projectsFolder = _services.ServiceProvider.GetRequiredService<IOptions<FwDataBridgeConfig>>().Value
            .ProjectsFolder;
        Directory.CreateDirectory(projectsFolder);
        var fwDataProject = new FwDataProject(_projectName, projectsFolder);
        _services.ServiceProvider.GetRequiredService<IProjectLoader>()
            .NewProject(fwDataProject, "en", DefaultVernacularWritingSystem);
        FwDataApi = _services.ServiceProvider.GetRequiredService<FwDataFactory>().GetFwDataMiniLcmApi(fwDataProject, false);

        var crdtProjectsFolder =
            _services.ServiceProvider.GetRequiredService<IOptions<LcmCrdtConfig>>().Value.ProjectPath;
        Directory.CreateDirectory(crdtProjectsFolder);
        var crdtProject = await _services.ServiceProvider.GetRequiredService<CrdtProjectsService>()
            .CreateProject(new(_projectName, _projectName, FwProjectId: FwDataApi.ProjectId, SeedNewProjectData: false));
        CrdtApi = (CrdtMiniLcmApi)await _services.ServiceProvider.OpenCrdtProject(crdtProject);
    }

    public async Task DisposeAsync()
    {
        await _services.DisposeAsync();
        _cleanup.Dispose();
    }

    public CrdtMiniLcmApi CrdtApi { get; set; } = null!;
    public FwDataMiniLcmApi FwDataApi { get; set; } = null!;

    public void DeleteSyncSnapshot()
    {
        var snapshotPath = ProjectSnapshotService.SnapshotPath(FwDataApi.Project);
        if (File.Exists(snapshotPath)) File.Delete(snapshotPath);
    }
}
