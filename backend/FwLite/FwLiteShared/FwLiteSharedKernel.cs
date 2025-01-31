﻿using FwLiteShared.Auth;
using FwLiteShared.Projects;
using FwLiteShared.Services;
using FwLiteShared.Sync;
using LcmCrdt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using SIL.Harmony;

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
        services.AddSingleton<TestingService>();
        services.AddOptions<FwLiteConfig>();
        services.DecorateConstructor<IJSRuntime>((provider, runtime) =>
        {
            var crdtConfig = provider.GetRequiredService<IOptions<CrdtConfig>>().Value;
            runtime.ConfigureJsonSerializerOptions(crdtConfig);
        });
        return services;
    }

    private static void AddAuthHelpers(this IServiceCollection services, IHostEnvironment environment)
    {
        services.AddSingleton<AuthService>();
        services.AddSingleton<OAuthClientFactory>();
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

    private static void DecorateConstructor<TService>(this IServiceCollection services,
        Action<IServiceProvider, TService> constructor)
    {
        for (var i = 0; i < services.Count; i++)
        {
            var descriptor = services[i];
            if (descriptor.ServiceType != typeof(TService)) continue;
            services[i] = new ServiceDescriptor(descriptor.ServiceType,
                sp =>
                {
                    if (descriptor.ImplementationType is null) throw new InvalidOperationException("Decorated constructor must have a non-null ImplementationType");
                    TService service = (TService)ActivatorUtilities.CreateInstance(sp, descriptor.ImplementationType);
                    if (service is null) throw new InvalidOperationException("Decorated constructor must return a non-null instance");
                    constructor(sp, service);
                    return service;
                }, descriptor.Lifetime);
        }
    }
}
