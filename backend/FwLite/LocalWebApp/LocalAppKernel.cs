using System.Text.Json;
using SIL.Harmony;
using FwLiteProjectSync;
using FwDataMiniLcmBridge;
using FwLiteShared;
using FwLiteShared.Auth;
using FwLiteShared.Sync;
using LcmCrdt;
using LocalWebApp.Services;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using Refit;

namespace LocalWebApp;

public static class LocalAppKernel
{
    public static IServiceCollection AddLocalAppServices(this IServiceCollection services, IHostEnvironment environment)
    {
        services.AddHttpContextAccessor();
        services.AddSingleton<UrlContext>();
        services.AddSingleton<IRedirectUrlProvider, ServerRedirectUrlProvider>();
        services.AddSingleton<ImportFwdataService>();
        services.AddFwLiteShared(environment);
        services.AddFwLiteProjectSync();

        services.AddOptions<LocalWebAppConfig>().BindConfiguration("LocalWebApp");

        services.AddOptions<JsonOptions>().PostConfigure<IOptions<CrdtConfig>>((jsonOptions, crdtConfig) =>
        {
            jsonOptions.SerializerOptions.TypeInfoResolver = crdtConfig.Value.MakeJsonTypeResolver();
        });

        services.AddOptions<JsonHubProtocolOptions>().PostConfigure<IOptions<CrdtConfig>>(
            (jsonOptions, crdtConfig) =>
            {
                jsonOptions.PayloadSerializerOptions.TypeInfoResolver = crdtConfig.Value.MakeJsonTypeResolver();
            });
        return services;
    }
}
