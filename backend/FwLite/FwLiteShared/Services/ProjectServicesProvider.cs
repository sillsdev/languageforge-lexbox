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
            await projectScope.CleanupAsync();
        }
    }

    [JSInvokable]
    public async Task DisposeService(DotNetObjectReference<IAsyncDisposable> service)
    {
        await service.Value.DisposeAsync();
    }

    [JSInvokable]
    public Task<string?> TryGetCrdtProjectName(string code)
    {
        var crdtProject = crdtProjectsService.GetProject(code);
        return Task.FromResult(crdtProject?.Data?.Name);
    }

    [JSInvokable]
    public Task<ProjectScope> OpenCrdtProject(string code)
    {
        return Task.Run(async () =>
        {
            var serviceScope = serviceProvider.CreateAsyncScope();
            ProjectScope? scope = null;
            try
            {
                var scopedServices = serviceScope.ServiceProvider;
                var project = crdtProjectsService.GetProject(code)
                              ?? throw new InvalidOperationException($"Crdt Project {code} not found");
                var server = lexboxProjectService.GetServer(project.Data);
                var currentProjectService = scopedServices.GetRequiredService<CurrentProjectService>();
                var projectData = await currentProjectService.SetupProjectContext(project);
                await scopedServices.GetRequiredService<SyncService>().SafeExecuteSync(true);
                await lexboxProjectService.ListenForProjectChanges(projectData, CancellationToken.None);
                var miniLcm = ActivatorUtilities.CreateInstance<MiniLcmJsInvokable>(scopedServices, project);
                scope = ProjectScope.Create(serviceScope, this, projectData.Name, miniLcm);
                scope.ProjectData = projectData;
                scope.Server = server;
                scope.SetCrdtServices(
                    ActivatorUtilities.CreateInstance<HistoryServiceJsInvokable>(scopedServices),
                    ActivatorUtilities.CreateInstance<SyncServiceJsInvokable>(scopedServices)
                );
                _projectScopes.TryAdd(scope, scope);
                return scope;
            }
            catch
            {
                if (scope is not null) await scope.CleanupAsync();
                else await serviceScope.DisposeAsync();
                throw;
            }
        });
    }

    [JSInvokable]
    public Task<ProjectScope> OpenFwDataProject(string projectName)
    {
        return Task.Run(async () =>
        {
            var serviceScope = serviceProvider.CreateAsyncScope();
            ProjectScope? scope = null;
            try
            {
                var scopedServices = serviceScope.ServiceProvider;
                if (FwDataProjectProvider is null)
                    throw new InvalidOperationException("FwData Project provider is not available");
                var project = FwDataProjectProvider.GetProject(projectName) ??
                              throw new InvalidOperationException($"FwData Project {projectName} not found");
                var miniLcm = ActivatorUtilities.CreateInstance<MiniLcmJsInvokable>(
                    scopedServices,
                    await FwDataProjectProvider.OpenProject(project, scopedServices),
                    project
                );
                scope = ProjectScope.Create(serviceScope, this, projectName, miniLcm);
                _projectScopes.TryAdd(scope, scope);
                return scope;
            }
            catch
            {
                if (scope is not null) await scope.CleanupAsync();
                else await serviceScope.DisposeAsync();
                throw;
            }
        });
    }
}

public class ProjectScope
{
    private static readonly ObjectFactory<ProjectScope> ProjectScopeFactory =
        ActivatorUtilities.CreateFactory<ProjectScope>(
        [
            typeof(AsyncServiceScope),
            typeof(ProjectServicesProvider),
            typeof(string),
            typeof(MiniLcmJsInvokable)
        ]);
    public static ProjectScope Create(
        AsyncServiceScope serviceScope,
        ProjectServicesProvider projectServicesProvider,
        string projectName,
        MiniLcmJsInvokable miniLcm)
    {
        return ProjectScopeFactory.Invoke(serviceScope.ServiceProvider,
        [
            serviceScope,
            projectServicesProvider,
            projectName,
            miniLcm
        ]);
    }

    public ProjectScope(AsyncServiceScope serviceScope,
        ProjectServicesProvider projectServicesProvider,
        string projectName,
        MiniLcmJsInvokable miniLcm,
        ILogger<ProjectScope> logger)
    {
        ProjectName = projectName;
        MiniLcm = DotNetObjectReference.Create(miniLcm);
        Cleanup = DotNetObjectReference.Create(Defer.Async(async () =>
        {
            logger.LogInformation("Disposing project scope {ProjectName}", projectName);
            projectServicesProvider._projectScopes.TryRemove(this, out _);
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

    public void SetCrdtServices(
        HistoryServiceJsInvokable historyService,
        SyncServiceJsInvokable syncService)
    {
        HistoryService = DotNetObjectReference.Create(historyService);
        SyncService = DotNetObjectReference.Create(syncService);
    }

    public ValueTask CleanupAsync()
    {
        return Cleanup?.Value.DisposeAsync() ?? ValueTask.CompletedTask;
    }

    public DotNetObjectReference<IAsyncDisposable>? Cleanup { get; set; }
    public string ProjectName { get; set; }
    public LexboxServer? Server { get; set; }
    public ProjectData? ProjectData { get; set; }
    public DotNetObjectReference<MiniLcmJsInvokable> MiniLcm { get; set; }
    public DotNetObjectReference<HistoryServiceJsInvokable>? HistoryService { get; set; }
    public DotNetObjectReference<SyncServiceJsInvokable>? SyncService { get; set; }
}
