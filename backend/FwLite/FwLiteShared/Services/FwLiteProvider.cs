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
    IOptions<FwLiteConfig> config,
    TestingService testingService,
    IAppLauncher? appLauncher = null,
    ITroubleshootingService? troubleshootingService = null
)
{
    public const string OverrideServiceFunctionName = "setOverrideService";

    public Dictionary<DotnetService, object> GetServices()
    {
        var services = new Dictionary<DotnetService, object>()
        {
            [DotnetService.CombinedProjectsService] = projectService,
            [DotnetService.AuthService] = authService,
            [DotnetService.ImportFwdataService] = importFwdataService,
            [DotnetService.FwLiteConfig] = config.Value,
            [DotnetService.TestingService] = testingService
        };
        if (appLauncher is not null)
            services[DotnetService.AppLauncher] = appLauncher;
        if (troubleshootingService is not null)
            services[DotnetService.TroubleshootingService] = troubleshootingService;
        return services;
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
    HistoryService,
    AppLauncher,
    TroubleshootingService,
    TestingService
}
