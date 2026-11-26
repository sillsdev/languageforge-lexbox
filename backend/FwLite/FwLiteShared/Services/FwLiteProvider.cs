using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using FwLiteShared.Analytics;
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
    IOptions<FwLiteConfig> config,
    IServiceProvider services
)
{
    public const string OverrideServicesFunctionName = "setOverrideServices";

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
        DotnetService.UpdateService,
        DotnetService.PreferencesService,
        DotnetService.PlatformFeaturesService,
        DotnetService.AnalyticsService,
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
        DotnetService.MediaFilesService => typeof(MediaFilesServiceJsInvokable),
        DotnetService.AppLauncher => typeof(IAppLauncher),
        DotnetService.TroubleshootingService => typeof(ITroubleshootingService),
        DotnetService.TestingService => typeof(TestingService),
        DotnetService.MultiWindowService => typeof(IMultiWindowService),
        DotnetService.JsEventListener => typeof(JsEventListener),
        DotnetService.JsInvokableLogger => typeof(JsInvokableLogger),
        DotnetService.UpdateService => typeof(UpdateService),
        DotnetService.PreferencesService => typeof(IPreferencesService),
        DotnetService.PlatformFeaturesService => typeof(IPlatformFeaturesService),
        DotnetService.AnalyticsService => typeof(IAnalyticsService),
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

    /// <summary>
    /// One interop call for all services: each call is a round trip, and the viewer can't load until they're set.
    /// </summary>
    public async Task SetServices(IJSRuntime jsRuntime, IReadOnlyDictionary<DotnetService, object> services)
    {
        var jsServices = services.ToDictionary(s => s.Key.ToString(),
            s => ShouldConvertToDotnetObject(s.Key, s.Value) ? DotNetObjectReference.Create(s.Value) : s.Value);
        await jsRuntime.DurableInvokeVoidAsync(OverrideServicesFunctionName, jsServices);
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
    MediaFilesService,
    AppLauncher,
    TroubleshootingService,
    TestingService,
    MultiWindowService,
    JsEventListener,
    JsInvokableLogger,
    UpdateService,
    PreferencesService,
    PlatformFeaturesService,
    AnalyticsService,
}
