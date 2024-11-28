using System.Runtime.CompilerServices;
using FwDataMiniLcmBridge;
using FwDataMiniLcmBridge.Api;
using FwDataMiniLcmBridge.LcmUtils;
using FwDataMiniLcmBridge.Tests.Fixtures;
using LcmCrdt;
using LexCore.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MiniLcm;

namespace FwLiteProjectSync.Tests.Fixtures;

public class SyncFixture : IAsyncLifetime
{
    private readonly AsyncServiceScope _services;

    public CrdtFwdataProjectSyncService SyncService =>
        _services.ServiceProvider.GetRequiredService<CrdtFwdataProjectSyncService>();
    public IServiceProvider Services => _services.ServiceProvider;
    private readonly string _projectName;
    private readonly IDisposable _cleanup;

    public static SyncFixture Create([CallerMemberName] string projectName = "") => new(projectName);

    private SyncFixture(string projectName)
    {
        _projectName = projectName;
        var crdtServices = new ServiceCollection()
            .AddSyncServices(_projectName);
        var rootServiceProvider = crdtServices.BuildServiceProvider();
        _cleanup = Defer.Action(() => rootServiceProvider.Dispose());
        _services = rootServiceProvider.CreateAsyncScope();
    }

    public SyncFixture(): this("sena-3_" + Guid.NewGuid().ToString("N"))
    {
    }

    public async Task InitializeAsync()
    {
        var projectsFolder = _services.ServiceProvider.GetRequiredService<IOptions<FwDataBridgeConfig>>().Value
            .ProjectsFolder;
        if (Path.Exists(projectsFolder)) Directory.Delete(projectsFolder, true);
        Directory.CreateDirectory(projectsFolder);
        _services.ServiceProvider.GetRequiredService<IProjectLoader>()
            .NewProject(new FwDataProject(_projectName, projectsFolder), "en", "en");
        FwDataApi = _services.ServiceProvider.GetRequiredService<FwDataFactory>().GetFwDataMiniLcmApi(_projectName, false);

        var crdtProjectsFolder =
            _services.ServiceProvider.GetRequiredService<IOptions<LcmCrdtConfig>>().Value.ProjectPath;
        if (Path.Exists(crdtProjectsFolder)) Directory.Delete(crdtProjectsFolder, true);
        Directory.CreateDirectory(crdtProjectsFolder);
        var crdtProject = await _services.ServiceProvider.GetRequiredService<ProjectsService>()
            .CreateProject(new(_projectName, FwProjectId: FwDataApi.ProjectId, SeedNewProjectData: false));
        CrdtApi = (CrdtMiniLcmApi) await _services.ServiceProvider.OpenCrdtProject(crdtProject);
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
        var snapshotPath = CrdtFwdataProjectSyncService.SnapshotPath(FwDataApi.Project);
        if (File.Exists(snapshotPath)) File.Delete(snapshotPath);
    }
}
