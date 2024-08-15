using FwDataMiniLcmBridge;
using FwDataMiniLcmBridge.Api;
using FwDataMiniLcmBridge.LcmUtils;
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

    public SyncFixture()
    {
        var crdtServices = new ServiceCollection()
            .AddLcmCrdtClient()
            .AddFwDataBridge()
            .AddFwLiteProjectSync()
            .AddSingleton<MockFwProjectLoader>()
            .AddSingleton<IProjectLoader>(sp => sp.GetRequiredService<MockFwProjectLoader>())
            .AddSingleton<FieldWorksProjectList, MockFwProjectList>()
            .Configure<FwDataBridgeConfig>(c => c.ProjectsFolder = Path.Combine(".", "FwData"))
            .AddLogging(builder => builder.AddDebug())
            .Configure<LcmCrdtConfig>(c => c.ProjectPath = Path.Combine(".", "LcmCrdt"))
            .BuildServiceProvider();
        _services = crdtServices.CreateAsyncScope();
    }

    public async Task InitializeAsync()
    {
        var projectsFolder = _services.ServiceProvider.GetRequiredService<IOptions<FwDataBridgeConfig>>().Value
            .ProjectsFolder;
        if (Path.Exists(projectsFolder)) Directory.Delete(projectsFolder, true);
        Directory.CreateDirectory(projectsFolder);
        var lcmCache = _services.ServiceProvider.GetRequiredService<IProjectLoader>()
            .NewProject("sena-3.fwdata", "en", "fr");
        var projectGuid = lcmCache.LanguageProject.Guid;
        FwDataApi = _services.ServiceProvider.GetRequiredService<FwDataFactory>().GetFwDataMiniLcmApi("sena-3", false);

        var crdtProjectsFolder =
            _services.ServiceProvider.GetRequiredService<IOptions<LcmCrdtConfig>>().Value.ProjectPath;
        if (Path.Exists(crdtProjectsFolder)) Directory.Delete(crdtProjectsFolder, true);
        Directory.CreateDirectory(crdtProjectsFolder);
        var crdtProject = await _services.ServiceProvider.GetRequiredService<ProjectsService>()
            .CreateProject("sena-3", projectGuid);
        _services.ServiceProvider.GetRequiredService<ProjectContext>().Project = crdtProject;
        CrdtApi = _services.ServiceProvider.GetRequiredService<ILexboxApi>();
    }

    public async Task DisposeAsync()
    {
        await _services.DisposeAsync();
    }

    public ILexboxApi CrdtApi { get; set; } = null!;
    public FwDataMiniLcmApi FwDataApi { get; set; } = null!;
}

public class MockProjectContext(CrdtProject project) : ProjectContext
{
    public override CrdtProject? Project { get; set; } = project;
}
