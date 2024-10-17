using FwDataMiniLcmBridge;
using FwDataMiniLcmBridge.Api;
using FwDataMiniLcmBridge.LcmUtils;
using FwDataMiniLcmBridge.Tests.Fixtures;
using LcmCrdt;
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
    private readonly string _projectName;
    private bool _crdtEnabled = true;
    private bool _fwDataEnabled = true;

    public static SyncFixture Create(string projectName) => new(projectName);

    private SyncFixture(string projectName)
    {
        _projectName = projectName;
        var crdtServices = new ServiceCollection()
            .AddLcmCrdtClient()
            .AddTestFwDataBridge()
            .AddFwLiteProjectSync()
            .Configure<FwDataBridgeConfig>(c => c.ProjectsFolder = Path.Combine(".", _projectName, "FwData"))
            .Configure<LcmCrdtConfig>(c => c.ProjectPath = Path.Combine(".", _projectName, "LcmCrdt"))
            .AddLogging(builder => builder.AddDebug())
            .BuildServiceProvider();
        _services = crdtServices.CreateAsyncScope();
    }

    public SyncFixture(): this("sena-3")
    {
    }

    public async Task InitializeAsync()
    {
        Guid projectGuid = Guid.NewGuid();
        if (_fwDataEnabled)
        {
            var projectsFolder = _services.ServiceProvider.GetRequiredService<IOptions<FwDataBridgeConfig>>().Value
                .ProjectsFolder;
            if (Path.Exists(projectsFolder)) Directory.Delete(projectsFolder, true);
            Directory.CreateDirectory(projectsFolder);
            var lcmCache = _services.ServiceProvider.GetRequiredService<IProjectLoader>()
                .NewProject($"{_projectName}.fwdata", "en", "fr");
            projectGuid = lcmCache.LanguageProject.Guid;
            FwDataApi = _services.ServiceProvider.GetRequiredService<FwDataFactory>()
                .GetFwDataMiniLcmApi(_projectName, false);
        }

        if (_crdtEnabled)
        {
            var crdtProjectsFolder = _services.ServiceProvider.GetRequiredService<IOptions<LcmCrdtConfig>>().Value.ProjectPath;
            if (Path.Exists(crdtProjectsFolder)) Directory.Delete(crdtProjectsFolder, true);
            Directory.CreateDirectory(crdtProjectsFolder);
            var crdtProject = await _services.ServiceProvider.GetRequiredService<ProjectsService>()
                .CreateProject(new(_projectName, projectGuid));
            _services.ServiceProvider.GetRequiredService<ProjectContext>().Project = crdtProject;
            CrdtApi = _services.ServiceProvider.GetRequiredService<IMiniLcmApi>();
        }
    }

    public async Task DisposeAsync()
    {
        await _services.DisposeAsync();
    }

    public IMiniLcmApi CrdtApi { get; set; } = null!;
    public FwDataMiniLcmApi FwDataApi { get; set; } = null!;

    public void DisableCrdt()
    {
        _crdtEnabled = false;
    }

    public void DisableFwData()
    {
        _fwDataEnabled = false;
    }
}

public class MockProjectContext(CrdtProject project) : ProjectContext
{
    public override CrdtProject? Project { get; set; } = project;
}
