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
    ILogger<FwLiteProvider> logger,
    IOptions<FwLiteConfig> config
)
{
    public const string OverrideServiceFunctionName = "setOverrideService";

    public Dictionary<DotnetService, object> GetServices()
    {
        return Enum.GetValues<DotnetService>().Where(s => s != DotnetService.MiniLcmApi && s != DotnetService.ProjectServicesProvider && s != DotnetService.HistoryService)
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
}


[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DotnetService
{
    MiniLcmApi,
    CombinedProjectsService,
    AuthService,
    ImportFwdataService,
    FwLiteConfig,
    ProjectServicesProvider,
    HistoryService
}
