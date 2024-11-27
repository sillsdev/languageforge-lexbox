using FwDataMiniLcmBridge;
using FwLiteProjectSync;
using FwLiteShared.Auth;
using FwLiteShared.Projects;
using FwLiteShared.Sync;
using LcmCrdt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FwLiteShared;

public static class FwLiteSharedKernel
{
    public static IServiceCollection AddFwLiteShared(this IServiceCollection services, IHostEnvironment environment)
    {
        services.AddHttpClient();
        services.AddAuthHelpers(environment);
        services.AddLcmCrdtClient();
        services.AddFwDataBridge();
        services.AddFwLiteProjectSync();

        services.AddSingleton<ImportFwdataService>();
        services.AddScoped<SyncService>();
        services.AddSingleton<LexboxProjectService>();
        services.AddSingleton<CombinedProjectsService>();
        services.AddSingleton<ChangeEventBus>();
        services.AddSingleton<BackgroundSyncService>();
        services.AddSingleton<IHostedService>(s => s.GetRequiredService<BackgroundSyncService>());
        return services;
    }

    private static void AddAuthHelpers(this IServiceCollection services, IHostEnvironment environment)
    {
        services.AddSingleton<AuthService>();
        services.AddSingleton<OAuthClientFactory>();
        services.AddScoped(CurrentAuthHelperFactory);
        services.AddSingleton<OAuthService>();
        services.AddSingleton<IHostedService>(sp => sp.GetRequiredService<OAuthService>());
        services.AddOptionsWithValidateOnStart<AuthConfig>().BindConfiguration("Auth").ValidateDataAnnotations();
        services.AddSingleton<LoggerAdapter>();
        var httpClientBuilder = services.AddHttpClient(OAuthClient.AuthHttpClientName);
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

    private static OAuthClient CurrentAuthHelperFactory(this IServiceProvider serviceProvider)
    {
        var authHelpersFactory = serviceProvider.GetRequiredService<OAuthClientFactory>();
        var currentProjectService = serviceProvider.GetRequiredService<CurrentProjectService>();
        return authHelpersFactory.GetClient(currentProjectService.ProjectData);
    }
}
