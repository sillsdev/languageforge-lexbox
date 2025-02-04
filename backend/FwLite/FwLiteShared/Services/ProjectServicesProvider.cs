using System.Collections.Concurrent;
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
    ChangeEventBus changeEventBus,
    IEnumerable<IProjectProvider> projectProviders,
    IJSRuntime jsRuntime,
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
    public async Task<ProjectScope> OpenCrdtProject(string projectName)
    {
        var serviceScope = serviceProvider.CreateAsyncScope();
        var scopedServices = serviceScope.ServiceProvider;
        var project = crdtProjectsService.GetProject(projectName) ??
                      throw new InvalidOperationException($"Crdt Project {projectName} not found");
        var currentProjectService = scopedServices.GetRequiredService<CurrentProjectService>();
        var projectData = await currentProjectService.SetupProjectContext(project);
        await scopedServices.GetRequiredService<SyncService>().SafeExecuteSync(true);
        await lexboxProjectService.ListenForProjectChanges(projectData, CancellationToken.None);
        var entryUpdatedSubscription = changeEventBus.OnProjectEntryUpdated(project).Subscribe(entry =>
        {
            _ = jsRuntime.DurableInvokeVoidAsync("notifyEntryUpdated", projectName, entry);
        });
        var miniLcm = ActivatorUtilities.CreateInstance<MiniLcmJsInvokable>(scopedServices, project);
        var scope = new ProjectScope(Defer.Async(() =>
        {
            logger.LogInformation("Disposing project scope {ProjectName}", projectName);
            entryUpdatedSubscription.Dispose();
            return Task.CompletedTask;
        }), serviceScope, this, projectName, miniLcm, ActivatorUtilities.CreateInstance<HistoryServiceJsInvokable>(scopedServices));
        _projectScopes.TryAdd(scope, scope);
        return scope;
    }

    [JSInvokable]
    public Task<ProjectScope> OpenFwDataProject(string projectName)
    {
        var serviceScope = serviceProvider.CreateAsyncScope();
        var scopedServices = serviceScope.ServiceProvider;
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
            return Task.CompletedTask;
        }), serviceScope, this, projectName, miniLcm, null);
        _projectScopes.TryAdd(scope, scope);
        return Task.FromResult(scope);
    }
}

public class ProjectScope
{
    public ProjectScope(IAsyncDisposable cleanup,
        AsyncServiceScope serviceScope,
        ProjectServicesProvider projectServicesProvider,
        string projectName,
        MiniLcmJsInvokable miniLcm,
        HistoryServiceJsInvokable? historyService)
    {
        ProjectName = projectName;
        MiniLcm = DotNetObjectReference.Create(miniLcm);
        HistoryService = historyService is null ? null : DotNetObjectReference.Create(historyService);
        Cleanup = DotNetObjectReference.Create(Defer.Async(async () =>
        {
            projectServicesProvider._projectScopes.TryRemove(this, out _);
            await cleanup.DisposeAsync();
            if (HistoryService is not null)
            {
                HistoryService.Dispose();
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
    public DotNetObjectReference<MiniLcmJsInvokable> MiniLcm { get; set; }
    public DotNetObjectReference<HistoryServiceJsInvokable>? HistoryService { get; set; }
}
