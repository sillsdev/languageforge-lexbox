using System.Collections.Concurrent;
using FwLiteShared.Auth;
using FwLiteShared.Projects;
using FwLiteShared.Sync;
using LcmCrdt;
using LexCore.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using MiniLcm.Models;
using MiniLcm.Project;

namespace FwLiteShared.Services;

//this service is special, it is scoped, but it should not inject any scoped project services
public class ProjectServicesProvider(
    CrdtProjectsService crdtProjectsService,
    IServiceProvider serviceProvider,
    LexboxProjectService lexboxProjectService,
    IEnumerable<IProjectProvider> projectProviders,
    ILogger<ProjectServicesProvider> logger
): IAsyncDisposable
{
    private IProjectProvider? FwDataProjectProvider =>
        projectProviders.FirstOrDefault(p => p.DataFormat == ProjectDataFormat.FwData);
    internal readonly ConcurrentDictionary<ProjectScope, ProjectScope> _projectScopes = new();
    public async ValueTask DisposeAsync()
    {
        foreach (var projectScope in _projectScopes.Values)
        {
            await (projectScope.Cleanup?.Value.DisposeAsync() ?? ValueTask.CompletedTask);
        }
    }

    [JSInvokable]
    public async Task DisposeService(DotNetObjectReference<IAsyncDisposable> service)
    {
        await service.Value.DisposeAsync();
    }

    [JSInvokable]
    public async Task<ProjectData> GetCrdtProjectData(string code)
    {
        await crdtProjectsService.EnsureProjectDataCacheIsLoaded();
        var crdtProject = crdtProjectsService.GetProject(code)
            ?? throw new InvalidOperationException($"Crdt Project {code} not found");
        return crdtProject.Data ?? throw new InvalidOperationException($"Project data for {crdtProject.Name} not found");
    }

    [JSInvokable]
    public async Task<ProjectScope> OpenCrdtProject(string code)
    {
        var serviceScope = serviceProvider.CreateAsyncScope();
        var scopedServices = serviceScope.ServiceProvider;
        var project = crdtProjectsService.GetProject(code)
            ?? throw new InvalidOperationException($"Crdt Project {code} not found");
        var server = lexboxProjectService.GetServer(project.Data);
        var currentProjectService = scopedServices.GetRequiredService<CurrentProjectService>();
        var projectData = await currentProjectService.SetupProjectContext(project);
        await scopedServices.GetRequiredService<SyncService>().SafeExecuteSync(true);
        await lexboxProjectService.ListenForProjectChanges(projectData, CancellationToken.None);
        var miniLcm = ActivatorUtilities.CreateInstance<MiniLcmJsInvokable>(scopedServices, project);
        var scope = new ProjectScope(Defer.Async(() =>
        {
            logger.LogInformation("Disposing project scope {ProjectName}", projectData.Name);
            return Task.CompletedTask;
        }), serviceScope, this, projectData.Name, miniLcm,
                ActivatorUtilities.CreateInstance<HistoryServiceJsInvokable>(scopedServices),
                ActivatorUtilities.CreateInstance<SyncServiceJsInvokable>(scopedServices))
        {
            ProjectData = projectData,
            Server = server
        };
        _projectScopes.TryAdd(scope, scope);
        return scope;
    }

    [JSInvokable]
    public async Task<ProjectScope> OpenFwDataProject(string projectName)
    {
        var serviceScope = serviceProvider.CreateAsyncScope();
        var scopedServices = serviceScope.ServiceProvider;
        if (FwDataProjectProvider is null)
            throw new InvalidOperationException("FwData Project provider is not available");
        var project = FwDataProjectProvider.GetProject(projectName) ??
                      throw new InvalidOperationException($"FwData Project {projectName} not found");
        var miniLcm = ActivatorUtilities.CreateInstance<MiniLcmJsInvokable>(scopedServices,
            await FwDataProjectProvider.OpenProject(project, scopedServices),
            project);
        var scope = new ProjectScope(Defer.Async(() =>
        {
            logger.LogInformation("Disposing fwdata project scope {ProjectName}", projectName);
            return Task.CompletedTask;
        }), serviceScope, this, projectName, miniLcm, null, null);
        _projectScopes.TryAdd(scope, scope);
        return scope;
    }
}

public class ProjectScope
{
    public ProjectScope(IAsyncDisposable cleanup,
        AsyncServiceScope serviceScope,
        ProjectServicesProvider projectServicesProvider,
        string projectName,
        MiniLcmJsInvokable miniLcm,
        HistoryServiceJsInvokable? historyService,
        SyncServiceJsInvokable? syncService)
    {
        ProjectName = projectName;
        MiniLcm = DotNetObjectReference.Create(miniLcm);
        HistoryService = historyService is null ? null : DotNetObjectReference.Create(historyService);
        SyncService = syncService is null ? null : DotNetObjectReference.Create(syncService);
        Cleanup = DotNetObjectReference.Create(Defer.Async(async () =>
        {
            projectServicesProvider._projectScopes.TryRemove(this, out _);
            await cleanup.DisposeAsync();
            if (HistoryService is not null)
            {
                HistoryService.Dispose();
            }
            if (SyncService is not null)
            {
                SyncService.Dispose();
            }

            MiniLcm.Value.Dispose();
            MiniLcm.Dispose();
            await serviceScope.DisposeAsync();
            //cleanup the dotnet object reference, this will not trigger this callback again
            Cleanup?.Dispose();
            Cleanup = null;
        }));
    }

    public DotNetObjectReference<IAsyncDisposable>? Cleanup { get; set; }
    public string ProjectName { get; set; }
    public LexboxServer? Server { get; set; }
    public ProjectData? ProjectData { get; init; }
    public DotNetObjectReference<MiniLcmJsInvokable> MiniLcm { get; set; }
    public DotNetObjectReference<HistoryServiceJsInvokable>? HistoryService { get; set; }
    public DotNetObjectReference<SyncServiceJsInvokable>? SyncService { get; set; }
}
