using FwLiteShared.Projects;
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
    IServiceProvider scopedServices,
    LexboxProjectService lexboxProjectService,
    ChangeEventBus changeEventBus,
    IEnumerable<IProjectProvider> projectProviders,
    IJSRuntime jsRuntime,
    ILogger<ProjectServicesProvider> logger
) : IAsyncDisposable
{
    private IProjectProvider? FwDataProjectProvider =>
        projectProviders.FirstOrDefault(p => p.DataFormat == ProjectDataFormat.FwData);
    private readonly ConcurrentWeakDictionary<string, ProjectScope> _projectScopes = new();
    //handles cleanup of project scopes which didn't get cleaned up by the js code, maybe because the user closed the tab
    //this will get executed when the blazor circuit is disposed
    public async ValueTask DisposeAsync()
    {
        foreach (var scope in _projectScopes.Values)
        {
            await (scope.Cleanup?.Value.DisposeAsync() ?? ValueTask.CompletedTask);
        }
    }

    [JSInvokable]
    public async Task DisposeService(DotNetObjectReference<IAsyncDisposable> service)
    {
        await service.Value.DisposeAsync();
    }

    [JSInvokable]
    public async Task<ProjectScope> OpenCrdtProject(string projectName)
    {
        var project = crdtProjectsService.GetProject(projectName) ??
                      throw new InvalidOperationException($"Crdt Project {projectName} not found");
        var currentProjectService = scopedServices.GetRequiredService<CurrentProjectService>();
        var projectData = await currentProjectService.SetupProjectContext(project);
        await lexboxProjectService.ListenForProjectChanges(projectData, CancellationToken.None);
        var entryUpdatedSubscription = changeEventBus.OnProjectEntryUpdated(project).Subscribe(entry =>
        {
            _ = jsRuntime.DurableInvokeVoidAsync("notifyEntryUpdated", projectName, entry);
        });
        var miniLcm = ActivatorUtilities.CreateInstance<MiniLcmJsInvokable>(scopedServices, project);
        var historyService = scopedServices.GetRequiredService<HistoryService>();
        var scope = new ProjectScope(Defer.Async(() =>
        {
            logger.LogInformation("Disposing project scope {ProjectName}", projectName);
            currentProjectService.ClearProjectContext();
            entryUpdatedSubscription.Dispose();
            _projectScopes.Remove(projectName);
            return Task.CompletedTask;
        }), projectName, miniLcm, new HistoryServiceJsInvokable(historyService));
        await TrackScope(scope);
        return scope;
    }

    [JSInvokable]
    public async Task<ProjectScope> OpenFwDataProject(string projectName)
    {
        if (FwDataProjectProvider is null)
            throw new InvalidOperationException("FwData Project provider is not available");
        var project = FwDataProjectProvider.GetProject(projectName) ??
                      throw new InvalidOperationException($"FwData Project {projectName} not found");
        var miniLcm = ActivatorUtilities.CreateInstance<MiniLcmJsInvokable>(scopedServices,
            FwDataProjectProvider.OpenProject(project),
            project);
        var scope = new ProjectScope(Defer.Async(() =>
        {
            logger.LogInformation("Disposing fwdata project scope {ProjectName}", projectName);
            _projectScopes.Remove(projectName);
            return Task.CompletedTask;
        }), projectName, miniLcm, null);
        await TrackScope(scope);
        return scope;
    }

    private async ValueTask TrackScope(ProjectScope scope)
    {
        var oldScope = _projectScopes.Remove(scope.ProjectName);
        _projectScopes.Add(scope.ProjectName, scope);
        await (oldScope?.Cleanup?.Value.DisposeAsync() ?? ValueTask.CompletedTask);
    }
}

public class ProjectScope
{
    public ProjectScope(IAsyncDisposable cleanup,
        string projectName,
        MiniLcmJsInvokable miniLcm,
        HistoryServiceJsInvokable? historyService)
    {
        ProjectName = projectName;
        MiniLcm = DotNetObjectReference.Create(miniLcm);
        HistoryService = historyService is null ? null : DotNetObjectReference.Create(historyService);
        Cleanup = DotNetObjectReference.Create(Defer.Async(async () =>
        {
            await cleanup.DisposeAsync();
            if (HistoryService is not null)
            {
                HistoryService.Dispose();
            }

            MiniLcm.Value.Dispose();
            MiniLcm.Dispose();
            Cleanup?.Dispose();
            Cleanup = null;
        }));
    }

    public DotNetObjectReference<IAsyncDisposable>? Cleanup { get; set; }
    public string ProjectName { get; set; }
    public DotNetObjectReference<MiniLcmJsInvokable> MiniLcm { get; set; }
    public DotNetObjectReference<HistoryServiceJsInvokable>? HistoryService { get; set; }
}
