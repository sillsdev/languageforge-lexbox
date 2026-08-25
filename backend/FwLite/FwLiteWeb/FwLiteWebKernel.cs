using System.Text.Json.Serialization.Metadata;
using MiniLcm;
using SIL.Harmony.Config;
using FwDataMiniLcmBridge;
using FwLiteProjectSync;
using SIL.Harmony;
using FwLiteShared;
using FwLiteShared.Analytics;
using FwLiteShared.Auth;
using FwLiteShared.Services;
using FwLiteWeb.Routes;
using LcmCrdt;
using FwLiteWeb.Services;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;

namespace FwLiteWeb;

public static class FwLiteWebKernel
{
    public static IServiceCollection AddFwLiteWebServices(this IServiceCollection services, IHostEnvironment environment)
    {
        services.AddHttpContextAccessor();
        services.AddSingleton<UrlContext>();
        services.AddSingleton<IRedirectUrlProvider, ServerRedirectUrlProvider>();
        services.AddFwDataBridge();
        services.AddFwLiteProjectSync();
        services.AddMiniLcmRouteServices();
        services.AddFwLiteShared(environment);
        services.AddSingleton<IHostedService>(sp =>
            new AppLaunchTracker(sp.GetRequiredService<IAnalyticsService>(), MixpanelAnalytics.WebHost));
        services.AddSingleton<IPreferencesService, JsonFilePreferencesService>();

        services.AddSingleton<ITroubleshootingService, WebTroubleshootingService>();
        services.AddSingleton<IHostedService, NetworkChangeSyncTrigger>();
        services.AddOptions<FwLiteWebConfig>().BindConfiguration("FwLiteWeb");

        services.AddOptions<JsonOptions>().PostConfigure<IOptions<HarmonyConfig>>((jsonOptions, harmonyConfig) =>
        {
            // Layer the external MiniLcm modifiers onto ASP.NET's resolver, then let Harmony add its
            // IChange/IObject polymorphism (type-info modifier + change converter) so ChangeEntity<IChange>
            // fields in sync payloads deserialize.
            var options = jsonOptions.SerializerOptions;
            options.TypeInfoResolver = (options.TypeInfoResolver ?? new DefaultJsonTypeInfoResolver())
                .AddExternalMiniLcmModifiers();
            harmonyConfig.Value.ConfigureExternalJsonOptions(options);
        });
        return services;
    }
}
