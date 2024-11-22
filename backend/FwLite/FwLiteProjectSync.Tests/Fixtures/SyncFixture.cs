﻿using System.Runtime.CompilerServices;
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
    public IServiceProvider Services => _services.ServiceProvider;
    private readonly string _projectName;
    private readonly MockProjectContext _projectContext = new(null);

    public static SyncFixture Create([CallerMemberName] string projectName = "") => new(projectName);

    public static SyncFixture Create(Action<IServiceCollection> extraServiceConfiguration, [CallerMemberName] string projectName = "") => new(projectName, extraServiceConfiguration);

    private SyncFixture(string projectName, Action<IServiceCollection>? extraServiceConfiguration = null)
    {
        _projectName = projectName;
        var crdtServices = new ServiceCollection()
            .AddLcmCrdtClient()
            .AddSingleton<ProjectContext>(_projectContext)
            .AddTestFwDataBridge()
            .AddFwLiteProjectSync()
            .Configure<FwDataBridgeConfig>(c => c.ProjectsFolder = Path.Combine(".", _projectName, "FwData"))
            .Configure<LcmCrdtConfig>(c => c.ProjectPath = Path.Combine(".", _projectName, "LcmCrdt"))
            .AddLogging(builder => builder.AddDebug());
        if (extraServiceConfiguration is not null) extraServiceConfiguration(crdtServices);
        _services = crdtServices.BuildServiceProvider().CreateAsyncScope();
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
            .NewProject(new FwDataProject(_projectName, projectsFolder), "en", "fr");
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
        // CAUTION: Do not assume that InitializeAsync() has been called
        await _services.DisposeAsync();
    }

    public CrdtMiniLcmApi CrdtApi { get; set; } = null!;
    public FwDataMiniLcmApi FwDataApi { get; set; } = null!;
}

public class MockProjectContext(CrdtProject? project) : ProjectContext
{
    public override CrdtProject? Project { get; set; } = project;
}
