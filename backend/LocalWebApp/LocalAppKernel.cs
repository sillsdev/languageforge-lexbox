using System.Text.Json;
using Crdt;
using LcmCrdt;
using LocalWebApp.Auth;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using Refit;

namespace LocalWebApp;

public static class LocalAppKernel
{
    public static void AddLocalAppServices(this IServiceCollection services, IHostEnvironment environment)
    {
        services.AddHttpContextAccessor();
        services.AddHttpClient();
        services.AddAuthHelpers(environment);
        services.AddSingleton<UrlContext>();
        services.AddScoped<SyncService>();
        services.AddSingleton<BackgroundSyncService>();
        services.AddSingleton<IHostedService>(s => s.GetRequiredService<BackgroundSyncService>());
        services.AddLcmCrdtClient();
        services.AddOptions<JsonOptions>().PostConfigure<IOptions<CrdtConfig>>((jsonOptions, crdtConfig) =>
        {
            jsonOptions.SerializerOptions.TypeInfoResolver = crdtConfig.Value.MakeJsonTypeResolver();
        });

        services.AddOptions<JsonHubProtocolOptions>().PostConfigure<IOptions<CrdtConfig>>(
            (jsonOptions, crdtConfig) =>
            {
                jsonOptions.PayloadSerializerOptions.TypeInfoResolver = crdtConfig.Value.MakeJsonTypeResolver();
            });
        services.AddHttpClient();
        services.AddSingleton<RefitSettings>(provider => new RefitSettings
        {
            ContentSerializer = new SystemTextJsonContentSerializer(new(JsonSerializerDefaults.Web)
            {
                TypeInfoResolver = provider.GetRequiredService<IOptions<CrdtConfig>>().Value
                    .MakeJsonTypeResolver()
            })
        });
        services.AddSingleton<CrdtHttpSyncService>();
    }

    private static void AddAuthHelpers(this IServiceCollection services, IHostEnvironment environment)
    {
        services.AddSingleton<AuthHelpersFactory>();
        services.AddTransient<AuthHelpers>(sp => sp.GetRequiredService<AuthHelpersFactory>().GetCurrentHelper());
        services.AddSingleton<OAuthService>();
        services.AddSingleton<IHostedService>(sp => sp.GetRequiredService<OAuthService>());
        services.AddOptionsWithValidateOnStart<AuthConfig>().ValidateDataAnnotations();
        services.AddSingleton<LoggerAdapter>();
        var httpClientBuilder = services.AddHttpClient(AuthHelpers.AuthHttpClientName);
        if (environment.IsDevelopment())
        {
            // Allow self-signed certificates in development
            httpClientBuilder.ConfigurePrimaryHttpMessageHandler(() =>
                {
                    return new HttpClientHandler
                    {
                        ClientCertificateOptions = ClientCertificateOption.Manual,
                        ServerCertificateCustomValidationCallback = (message, certificate2, arg3, arg4) => true
                    };
                });
        }

    }
}
