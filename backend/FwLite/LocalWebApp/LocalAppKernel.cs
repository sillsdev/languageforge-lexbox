using SIL.Harmony;
using FwLiteShared;
using FwLiteShared.Auth;
using LcmCrdt;
using LocalWebApp.Services;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;

namespace LocalWebApp;

public static class LocalAppKernel
{
    public static IServiceCollection AddLocalAppServices(this IServiceCollection services, IHostEnvironment environment)
    {
        services.AddHttpContextAccessor();
        services.AddSingleton<UrlContext>();
        services.AddSingleton<IRedirectUrlProvider, ServerRedirectUrlProvider>();
        services.AddFwLiteShared(environment);

        services.AddOptions<LocalWebAppConfig>().BindConfiguration("LocalWebApp");

        services.AddOptions<JsonOptions>().PostConfigure<IOptions<CrdtConfig>>((jsonOptions, crdtConfig) =>
        {
            jsonOptions.SerializerOptions.TypeInfoResolver = crdtConfig.Value.MakeLcmCrdtExternalJsonTypeResolver();
        });

        services.AddOptions<JsonHubProtocolOptions>().PostConfigure<IOptions<CrdtConfig>>(
            (jsonOptions, crdtConfig) =>
            {
                jsonOptions.PayloadSerializerOptions.TypeInfoResolver = crdtConfig.Value.MakeLcmCrdtExternalJsonTypeResolver();
            });
        return services;
    }
}
