using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using FwLiteShared.Auth;
using FwLiteShared.Projects;
using LcmCrdt;
using LexCore.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using MiniLcm.Models;
using MiniLcm.Project;

namespace FwLiteShared.Services;

public class FwLiteProvider(
    CombinedProjectsService projectService,
    AuthService authService,
    ImportFwdataService importFwdataService,
    CrdtProjectsService crdtProjectsService,
    LexboxProjectService lexboxProjectService,
    ChangeEventBus changeEventBus,
    IEnumerable<IProjectProvider> projectProviders,
    ILogger<FwLiteProvider> logger
) : IDisposable
{
    public const string OverrideServiceFunctionName = "setOverrideService";
    private readonly List<IDisposable> _disposables = [];

    private IProjectProvider? FwDataProjectProvider =>
        projectProviders.FirstOrDefault(p => p.DataFormat == ProjectDataFormat.FwData);

    public void Dispose()
    {
        foreach (var disposable in _disposables)
        {
            disposable.Dispose();
        }
    }

    public Dictionary<DotnetService, object> GetServices()
    {
        return Enum.GetValues<DotnetService>().Where(s => s != DotnetService.MiniLcmApi)
            .ToDictionary(s => s, GetService);
    }

    public object GetService(DotnetService service)
    {
        return service switch
        {
            DotnetService.CombinedProjectsService => projectService,
            DotnetService.AuthService => authService,
            DotnetService.ImportFwdataService => importFwdataService,
            _ => throw new ArgumentOutOfRangeException(nameof(service), service, null)
        };
    }

    public async Task<IDisposable?> SetService(IJSRuntime jsRuntime, DotnetService service, object? serviceInstance)
    {
        DotNetObjectReference<object>? reference = null;
        if (serviceInstance is not null)
        {
            reference = DotNetObjectReference.Create(serviceInstance);
        }
        else
        {
            logger.LogInformation("Clearing Service {Service}", service);
        }

        await jsRuntime.InvokeVoidAsync(OverrideServiceFunctionName, service.ToString(), reference);
        return reference;
    }

    public async Task<IAsyncDisposable> InjectCrdtProject(IJSRuntime jsRuntime,
        IServiceProvider scopedServices,
        string projectName)
    {
        var project = crdtProjectsService.GetProject(projectName) ?? throw new InvalidOperationException($"Crdt Project {projectName} not found");
        var projectData = await scopedServices.GetRequiredService<CurrentProjectService>().SetupProjectContext(project);
        await lexboxProjectService.ListenForProjectChanges(projectData, CancellationToken.None);
        var entryUpdatedSubscription = changeEventBus.OnProjectEntryUpdated(project).Subscribe(entry =>
        {
            _ = jsRuntime.InvokeVoidAsync("notifyEntryUpdated", projectName, entry);
        });
        var service = ActivatorUtilities.CreateInstance<MiniLcmJsInvokable>(scopedServices, project);
        var reference = await SetService(jsRuntime,DotnetService.MiniLcmApi, service);
        return Defer.Async(() =>
        {
            reference?.Dispose();
            entryUpdatedSubscription.Dispose();
            return Task.CompletedTask;
        });
    }

    public async Task<IAsyncDisposable> InjectFwDataProject(IJSRuntime jsRuntime, IServiceProvider scopedServices, string projectName)
    {
        if (FwDataProjectProvider is null) throw new InvalidOperationException("FwData Project provider is not available");
        var project = FwDataProjectProvider.GetProject(projectName) ?? throw new InvalidOperationException($"FwData Project {projectName} not found");
        var service = ActivatorUtilities.CreateInstance<MiniLcmJsInvokable>(scopedServices,
            FwDataProjectProvider.OpenProject(project), project);
        var reference = await SetService(jsRuntime, DotnetService.MiniLcmApi, service);
        return Defer.Async(() =>
        {
            reference?.Dispose();
            service.Dispose();
            return Task.CompletedTask;
        });
    }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DotnetService
{
    MiniLcmApi,
    CombinedProjectsService,
    AuthService,
    ImportFwdataService,
}
