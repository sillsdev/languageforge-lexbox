using System.Net;
using FwLiteShared.AppUpdate;
using FwLiteShared.Auth;
using FwLiteShared.Events;
using FwLiteShared.Projects;
using FwLiteShared.Services;
using FwLiteShared.Sync;
using LcmCrdt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using MiniLcm.Project;
using Polly;
using Polly.Simmy;
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
        services.AddScoped<ImportFwdataService>();
        services.AddScoped<SyncService>();
        services.AddScoped<ProjectServicesProvider>();
        services.AddScoped<IServerHttpClientProvider, LexboxOauthServerClientProvider>();
        services.AddSingleton<LexboxProjectService>();
        services.AddSingleton<CombinedProjectsService>();
        services.AddSingleton<GlobalEventBus>();
        services.AddSingleton<ProjectEventBus>();
        services.AddSingleton<MiniLcmApiNotifyWrapperFactory>();
        services.AddScoped<JsEventListener>();
        services.AddScoped<JsInvokableLogger>();
        //this is scoped so that there will be once instance per blazor circuit, this prevents issues where the same instance is used when reloading the page.
        //it also avoids issues if there's multiple blazor circuits running at the same time
        services.AddScoped<FwLiteProvider>();

        services.AddSingleton<BackgroundSyncService>();
        services.AddSingleton<IHostedService>(s => s.GetRequiredService<BackgroundSyncService>());
        services.AddSingleton<UpdateChecker>();
        services.AddSingleton<IHostedService>(s => s.GetRequiredService<UpdateChecker>());
        services.TryAddSingleton<IPlatformUpdateService, CorePlatformUpdateService>();
        services.AddSingleton<UpdateService>();
        services.AddSingleton<TestingService>();
        services.AddOptions<FwLiteConfig>().BindConfiguration("FwLite");
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
        services.AddTransient<HttpClientRefreshDelegate>();
        var httpClientBuilder = services.AddHttpClient(OAuthClient.AuthHttpClientName);
        httpClientBuilder.AddHttpMessageHandler<HttpClientRefreshDelegate>();
        if (environment.IsDevelopment())
        {
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("FW_LITE_CHAOS")))
            {
                ConfigureHttpClientChaos(httpClientBuilder);
            }
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

    private static void ConfigureHttpClientChaos(IHttpClientBuilder builder)
    {
        builder.AddResilienceHandler("chaos",
            pipelineBuilder =>
            {
                const double injectionRate = 0.3;
                pipelineBuilder.AddChaosLatency(injectionRate, TimeSpan.FromSeconds(5))
                    .AddChaosFault(injectionRate, () => new InvalidOperationException("Chaos injected fault"))
                    .AddChaosOutcome(new()
                    {
                        InjectionRate = injectionRate,
                        OutcomeGenerator = arguments =>
                            new(Outcome.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)
                            {
                                RequestMessage = arguments.Context.GetRequestMessage()
                            }))
                    });
            });
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
