using FwLiteShared.Auth;
using FwLiteShared.Projects;
using FwLiteShared.Services;
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
        services.AddLogging();

        services.AddSingleton<ImportFwdataService>();
        services.AddScoped<SyncService>();
        services.AddScoped<ProjectServicesProvider>();
        services.AddSingleton<LexboxProjectService>();
        services.AddSingleton<CombinedProjectsService>();
        //this is scoped so that there will be once instance per blazor circuit, this prevents issues where the same instance is used when reloading the page.
        //it also avoids issues if there's multiple blazor circuits running at the same time
        services.AddScoped<FwLiteProvider>();

        services.AddSingleton<ChangeEventBus>();
        services.AddSingleton<BackgroundSyncService>();
        services.AddSingleton<IHostedService>(s => s.GetRequiredService<BackgroundSyncService>());
        services.AddOptions<FwLiteConfig>();
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
