using System.Text.Json.Serialization;
using FwDataMiniLcmBridge;
using FwLiteShared.Auth;
using FwLiteShared.Projects;
using LcmCrdt;
using LexCore.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using MiniLcm;

namespace FwLiteShared.Services;

public class FwLiteProvider(
    CombinedProjectsService projectService,
    AuthService authService,
    ImportFwdataService importFwdataService,
    CrdtProjectsService crdtProjectsService,
    FwDataProjectContext fwDataProjectContext,
    FieldWorksProjectList fieldWorksProjectList,
    LexboxProjectService lexboxProjectService,
    ChangeEventBus changeEventBus
) : IDisposable
{
    public const string OverrideServiceFunctionName = "setOverrideService";
    private readonly List<IDisposable> _disposables = [];

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

    public async Task SetService(IJSRuntime jsRuntime, DotnetService service, object? serviceInstance)
    {
        DotNetObjectReference<object>? reference = null;
        if (serviceInstance is not null)
        {
            reference = DotNetObjectReference.Create(serviceInstance);
            _disposables.Add(reference);
        }

        await jsRuntime.InvokeVoidAsync(OverrideServiceFunctionName, service.ToString(), reference);
    }

    public async Task<IAsyncDisposable> InjectCrdtProject(IServiceProvider scopedServices, string projectName)
    {
        var jsRuntime = scopedServices.GetRequiredService<IJSRuntime>();
        var project = crdtProjectsService.GetProject(projectName) ?? throw new InvalidOperationException($"Crdt Project {projectName} not found");
        var projectData = await scopedServices.GetRequiredService<CurrentProjectService>().SetupProjectContext(project);
        await lexboxProjectService.ListenForProjectChanges(projectData, CancellationToken.None);
        var entryUpdatedSubscription = changeEventBus.OnProjectEntryUpdated(project).Subscribe(entry =>
        {
            _ = jsRuntime.InvokeVoidAsync("notifyEntryUpdated", projectName, entry);
        });
        var service = ActivatorUtilities.CreateInstance<MiniLcmJsInvokable>(scopedServices, project);
        await SetService(jsRuntime,DotnetService.MiniLcmApi, service);
        return Defer.Async(async () =>
        {
            entryUpdatedSubscription.Dispose();
            await SetService(jsRuntime, DotnetService.MiniLcmApi, null);
        });
    }

    public async Task<IAsyncDisposable> InjectFwDataProject(IServiceProvider scopedServices, string projectName)
    {
        var jsRuntime = scopedServices.GetRequiredService<IJSRuntime>();
        fwDataProjectContext.Project = fieldWorksProjectList.GetProject(projectName);
        var service = ActivatorUtilities.CreateInstance<MiniLcmJsInvokable>(scopedServices,
            scopedServices.GetRequiredKeyedService<IMiniLcmApi>(FwDataBridgeKernel.FwDataApiKey));
        await SetService(jsRuntime, DotnetService.MiniLcmApi, service);
        return Defer.Async(async () => await SetService(jsRuntime, DotnetService.MiniLcmApi, null));
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
