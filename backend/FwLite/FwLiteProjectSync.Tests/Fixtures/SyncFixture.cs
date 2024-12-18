﻿using System.Runtime.CompilerServices;
using FwDataMiniLcmBridge;
using FwDataMiniLcmBridge.Api;
using FwDataMiniLcmBridge.LcmUtils;
using LcmCrdt;
using LexCore.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FwLiteProjectSync.Tests.Fixtures;

public class SyncFixture : IAsyncLifetime
{
    private readonly AsyncServiceScope _services;

    public CrdtFwdataProjectSyncService SyncService =>
        _services.ServiceProvider.GetRequiredService<CrdtFwdataProjectSyncService>();
    public IServiceProvider Services => _services.ServiceProvider;
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

    public async Task InitializeAsync()
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
        _services.ServiceProvider.GetRequiredService<IProjectLoader>()
            .NewProject(new FwDataProject(_projectName, projectsFolder), "en", "en");
        FwDataApi = _services.ServiceProvider.GetRequiredService<FwDataFactory>().GetFwDataMiniLcmApi(_projectName, false);

        var crdtProjectsFolder =
            _services.ServiceProvider.GetRequiredService<IOptions<LcmCrdtConfig>>().Value.ProjectPath;
        Directory.CreateDirectory(crdtProjectsFolder);
        var crdtProject = await _services.ServiceProvider.GetRequiredService<CrdtProjectsService>()
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
