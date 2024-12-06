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
    IServiceProvider serviceProvider,
    FwDataProjectContext fwDataProjectContext,
    FieldWorksProjectList fieldWorksProjectList,
    IJSRuntime jsRuntime
): IDisposable
{
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
        return Enum.GetValues<DotnetService>().Where(s => s != DotnetService.MiniLcmApi).ToDictionary(s => s, GetService);
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

    public async Task SetService(DotnetService service, object serviceInstance)
    {
        var reference = DotNetObjectReference.Create(serviceInstance);
        _disposables.Add(reference);
        await jsRuntime.InvokeVoidAsync("setOverrideService", service.ToString(), reference);
    }

    public async Task<IAsyncDisposable> InjectCrdtProject(string projectName)
    {
        crdtProjectsService.SetActiveProject(projectName);
        var service = new MiniLcmJsInvokable(serviceProvider.GetRequiredService<IMiniLcmApi>());
        await SetService(DotnetService.MiniLcmApi, service);
        return Defer.Async(async () => await jsRuntime.InvokeVoidAsync("setOverrideService", DotnetService.MiniLcmApi.ToString(), null));
    }

    public async Task<IAsyncDisposable> InjectFwDataProject(string projectName)
    {
        fwDataProjectContext.Project = fieldWorksProjectList.GetProject(projectName);
        var service = new MiniLcmJsInvokable(serviceProvider.GetRequiredKeyedService<IMiniLcmApi>(FwDataBridgeKernel.FwDataApiKey));
        await SetService(DotnetService.MiniLcmApi, service);
        return Defer.Async(async () => await jsRuntime.InvokeVoidAsync("setOverrideService",
            DotnetService.MiniLcmApi.ToString(), null));
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
