using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using FwDataMiniLcmBridge;
using FwLiteProjectSync;
using SIL.Harmony;
using FwLiteShared;
using FwLiteShared.Auth;
using FwLiteWeb.Routes;
using LcmCrdt;
using FwLiteWeb.Services;
using FwLiteWeb.Utils;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.SignalR;
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
        if (environment.IsDevelopment())
        {
            services.Configure<FwLiteConfig>(config => config.UseDevAssets = true);
        }

        services.AddOptions<FwLiteWebConfig>().BindConfiguration("FwLiteWeb");

        services.AddOptions<JsonOptions>().PostConfigure<IOptions<CrdtConfig>>((jsonOptions, crdtConfig) =>
        {
            jsonOptions.SerializerOptions.TypeInfoResolver = jsonOptions.SerializerOptions.TypeInfoResolver.WithWebTypeInfo(crdtConfig.Value);
        });

        services.AddOptions<JsonHubProtocolOptions>().PostConfigure<IOptions<CrdtConfig>>(
            (jsonOptions, crdtConfig) =>
            {
                jsonOptions.PayloadSerializerOptions.TypeInfoResolver = jsonOptions.PayloadSerializerOptions.TypeInfoResolver.WithWebTypeInfo(crdtConfig.Value);
            });
        services.PostConfigure<CrdtConfig>(config =>
        {
            var type = typeof(RazorComponentsServiceCollectionExtensions).Assembly.GetType(
                "Microsoft.AspNetCore.Components.ServerComponentSerializationSettings");
            if (type is null)
                throw new InvalidOperationException(
                    "Microsoft.AspNetCore.Components.ServerComponentSerializationSettings not found");
            var property = type.GetField("JsonSerializationOptions", BindingFlags.Static | BindingFlags.Public);
            if (property is null)
                throw new InvalidOperationException(
                    "ServerComponentSerializationSettings.JsonSerializationOptions property not found");
            var jsonSerializerOptions = (JsonSerializerOptions?)property.GetValue(null);
            if (jsonSerializerOptions is null)
                throw new InvalidOperationException(
                    "ServerComponentSerializationSettings.JsonSerializationOptions is null");
            jsonSerializerOptions.TypeInfoResolver = jsonSerializerOptions.TypeInfoResolver.WithWebTypeInfo(config);

        });
        return services;
    }

    private static IJsonTypeInfoResolver WithWebTypeInfo(this IJsonTypeInfoResolver? resolver, CrdtConfig config)
    {
        return JsonTypeInfoResolver.Combine(resolver, config.MakeLcmCrdtExternalJsonTypeResolver());
    }
}
