using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using FwLiteShared.Auth;
using FwLiteShared.Projects;
using LcmCrdt;
using LexCore.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using MiniLcm;
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
    ILogger<FwLiteProvider> logger,
    IOptions<FwLiteConfig> config,
    ILoggerFactory loggerFactory
) : IDisposable
{
    public const string OverrideServiceFunctionName = "setOverrideService";
    private readonly List<IDisposable> _disposables = [];
    private readonly MiniLcmApiProvider _miniLcmApiProvider = new(loggerFactory.CreateLogger<MiniLcmApiProvider>());

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
            DotnetService.FwLiteConfig => config.Value,
            DotnetService.MiniLcmApiProvider => _miniLcmApiProvider,
            _ => throw new ArgumentOutOfRangeException(nameof(service), service, null)
        };
    }

    public async Task<IDisposable?> SetService(IJSRuntime jsRuntime, DotnetService service, object? serviceInstance)
    {
        DotNetObjectReference<object>? reference = null;
        if (serviceInstance is null)
        {
            logger.LogInformation("Clearing Service {Service}", service);
        }
        if (ShouldConvertToDotnetObject(service, serviceInstance))
        {
            reference = DotNetObjectReference.Create(serviceInstance);
            serviceInstance = reference;
        }

        await jsRuntime.DurableInvokeVoidAsync(OverrideServiceFunctionName, service.ToString(), serviceInstance);
        return reference;
    }


    private bool ShouldConvertToDotnetObject(DotnetService service, [NotNullWhen(true)] object? serviceInstance)
    {
        return serviceInstance is not null && service switch
        {
            DotnetService.FwLiteConfig => false,
            _ => true
        };
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
            _ = jsRuntime.DurableInvokeVoidAsync("notifyEntryUpdated", projectName, entry);
        });
        var cleanup = ProvideMiniLcmApi(ActivatorUtilities.CreateInstance<MiniLcmJsInvokable>(scopedServices, project));

        return Defer.Async(() =>
        {
            cleanup.Dispose();
            entryUpdatedSubscription.Dispose();
            return Task.CompletedTask;
        });
    }

    public IAsyncDisposable InjectFwDataProject(IServiceProvider scopedServices, string projectName)
    {
        if (FwDataProjectProvider is null) throw new InvalidOperationException("FwData Project provider is not available");
        var project = FwDataProjectProvider.GetProject(projectName) ?? throw new InvalidOperationException($"FwData Project {projectName} not found");
        var service = ActivatorUtilities.CreateInstance<MiniLcmJsInvokable>(scopedServices,
            FwDataProjectProvider.OpenProject(project), project);
        var cleanup = ProvideMiniLcmApi(service);
        return Defer.Async(() =>
        {
            cleanup.Dispose();
            service.Dispose();
            return Task.CompletedTask;
        });
    }

    private IDisposable ProvideMiniLcmApi(MiniLcmJsInvokable miniLcmApi)
    {
        var reference = DotNetObjectReference.Create(miniLcmApi);
        _miniLcmApiProvider.SetMiniLcmApi(reference);
        return Defer.Action(() =>
        {
            reference?.Dispose();
            _miniLcmApiProvider.ClearMiniLcmApi();
        });
    }
}

/// <summary>
/// this service is used to allow the frontend to await the api being setup by the backend, this means the frontend doesn't need to poll for the api being ready
/// </summary>
internal class MiniLcmApiProvider(ILogger<MiniLcmApiProvider> logger)
{
    private TaskCompletionSource<DotNetObjectReference<MiniLcmJsInvokable>> _tcs = new();

    [JSInvokable]
    public async Task<DotNetObjectReference<MiniLcmJsInvokable>> GetMiniLcmApi()
    {
        return await _tcs.Task;
    }

    public void SetMiniLcmApi(DotNetObjectReference<MiniLcmJsInvokable> miniLcmApi)
    {
        logger.LogInformation("Setting MiniLcmApi");
        _tcs.SetResult(miniLcmApi);
    }

    [JSInvokable]
    public void ClearMiniLcmApi()
    {
        logger.LogInformation("Clearing MiniLcmApi");
        //we can't cancel a tcs if it's already completed
        if (!_tcs.Task.IsCompleted)
        {
            //we need to tell any clients awaiting the tcs it's canceled. otherwise they will hang
            _tcs.SetCanceled();
        }

        //create a new tcs so any new clients will await the new api.
        _tcs = new();
    }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DotnetService
{
    MiniLcmApi,
    MiniLcmApiProvider,
    CombinedProjectsService,
    AuthService,
    ImportFwdataService,
    FwLiteConfig,
}
