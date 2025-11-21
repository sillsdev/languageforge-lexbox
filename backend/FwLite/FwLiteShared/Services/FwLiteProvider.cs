using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using FwLiteShared.Auth;
using FwLiteShared.Events;
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
    ILogger<FwLiteProvider> logger,
    IOptions<FwLiteConfig> config,
    IServiceProvider services
)
{
    public const string OverrideServiceFunctionName = "setOverrideService";

    public static readonly DotnetService[] ExportedServices =
    [
        DotnetService.CombinedProjectsService,
        DotnetService.AuthService,
        DotnetService.ImportFwdataService,
        DotnetService.FwLiteConfig,
        DotnetService.TestingService,
        DotnetService.AppLauncher,
        DotnetService.TroubleshootingService,
        DotnetService.MultiWindowService,
        DotnetService.JsEventListener,
        DotnetService.JsInvokableLogger,
    ];

    public static Type GetServiceType(DotnetService service) => service switch
    {
        DotnetService.MiniLcmApi => typeof(IMiniLcmApi),
        DotnetService.CombinedProjectsService => typeof(CombinedProjectsService),
        DotnetService.AuthService => typeof(AuthService),
        DotnetService.ImportFwdataService => typeof(ImportFwdataService),
        DotnetService.FwLiteConfig => typeof(FwLiteConfig),
        DotnetService.ProjectServicesProvider => typeof(ProjectServicesProvider),
        DotnetService.HistoryService => typeof(HistoryServiceJsInvokable),
        DotnetService.SyncService => typeof(SyncServiceJsInvokable),
        DotnetService.AppLauncher => typeof(IAppLauncher),
        DotnetService.TroubleshootingService => typeof(ITroubleshootingService),
        DotnetService.TestingService => typeof(TestingService),
        DotnetService.MultiWindowService => typeof(IMultiWindowService),
        DotnetService.JsEventListener => typeof(JsEventListener),
        DotnetService.JsInvokableLogger => typeof(JsInvokableLogger),
        _ => throw new ArgumentOutOfRangeException(nameof(service), service, null)
    };

    public Dictionary<DotnetService, object> GetServices()
    {
        var result = ExportedServices.Select(s => (key: s, service: services.GetService(GetServiceType(s))))
            .Where(t => t.service is not null)
            .ToDictionary(s => s.key, s => s.service);
        result[DotnetService.FwLiteConfig] = config.Value;
        return result!;
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
    SyncService,
    AppLauncher,
    TroubleshootingService,
    TestingService,
    MultiWindowService,
    JsEventListener,
    JsInvokableLogger,
}
