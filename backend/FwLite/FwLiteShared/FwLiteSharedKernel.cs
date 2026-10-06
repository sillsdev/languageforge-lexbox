using SIL.Harmony.Config;
using System.Net;
using FwLiteShared.Analytics;
using FwLiteShared.AppUpdate;
using FwLiteShared.Auth;
using FwLiteShared.Events;
using FwLiteShared.KeepAwake;
using FwLiteShared.Projects;
using FwLiteShared.Services;
using FwLiteShared.Sync;
using LcmCrdt;
using LexCore.Analytics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using MiniLcm.Project;
using System.Globalization;
using System.Net.Sockets;
using Polly;
using Polly.Simmy.Fault;
using Polly.Simmy;
using SIL.Harmony;

namespace FwLiteShared;

public static class FwLiteSharedKernel
{
    public static IServiceCollection AddFwLiteShared(this IServiceCollection services, IHostEnvironment environment)
    {
        services.AddMemoryCache();
        services.AddHttpClient();
        var lexboxClientBuilder = services.AddHttpClient(UpdateChecker.HttpClientName);
        if (ChaosEnabled(environment)) ConfigureHttpClientChaos(lexboxClientBuilder);
        services.AddHttpClient(MixpanelClient.HttpClientName, client =>
        {
            client.Timeout = TimeSpan.FromSeconds(10);
        });
        services.AddSingleton<MixpanelClient>();
        services.AddSingleton<IAnalyticsService, AnalyticsService>();
        services.AddOptions<AnalyticsConfig>()
            .BindConfiguration("Analytics")
            // CI is a static, start-of-process signal, so fold it into the config switch rather than
            // re-checking it on every event. PostConfigure runs after binding, so CI always wins.
            .PostConfigure(config =>
            {
                if (MixpanelAnalytics.IsCiEnvironment())
                    config.Enabled = false;
            });
        services.AddSingleton<IHostedService, AnalyticsIdentityListener>();
        services.AddSingleton<IHostedService, AppLaunchTracker>();
        services.AddAuthHelpers(environment);
        services.AddLcmCrdtClient();
        services.AddLogging();
        services.AddScoped<ImportFwdataService>();
        services.AddScoped<SyncService>();
        services.AddScoped<ProjectServicesProvider>();
        services.AddScoped<IServerHttpClientProvider, LexboxOauthServerClientProvider>();
        services.AddSingleton<LexboxProjectChangeListener>();
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
        services.AddSingleton<IBackgroundSyncService>(s => s.GetRequiredService<BackgroundSyncService>());
        services.AddSingleton<IHostedService>(s => s.GetRequiredService<BackgroundSyncService>());
        services.AddSingleton<IHostedService, PushListenerRecoveryService>();
        services.AddSingleton<UpdateCheckThrottle>();
        services.AddSingleton<UpdateChecker>();
        services.AddSingleton<IHostedService>(s => s.GetRequiredService<UpdateChecker>());
        services.TryAddSingleton<IPlatformUpdateService, CorePlatformUpdateService>();
        services.TryAddSingleton<INetworkStatus, NetworkInterfaceNetworkStatus>();
        services.TryAddSingleton<IPlatformFeaturesService, DummyPlatformFeaturesService>();
        services.TryAddSingleton<IKeepAwakePlatform, NoOpKeepAwakePlatform>();
        services.TryAddSingleton<IKeepAwake, RefCountedKeepAwake>();
        services.AddSingleton<UpdateService>();
        services.AddSingleton<TestingService>();
        services.AddOptions<FwLiteConfig>().BindConfiguration("FwLite");
        services.DecorateConstructor<IJSRuntime>((provider, runtime) =>
        {
            var harmonyConfig = provider.GetRequiredService<IOptions<HarmonyConfig>>().Value;
            runtime.ConfigureJsonSerializerOptions(harmonyConfig);
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
        if (ChaosEnabled(environment)) ConfigureHttpClientChaos(httpClientBuilder);
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

    private static bool ChaosEnabled(IHostEnvironment environment)
    {
        return environment.IsDevelopment() &&
               !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("FW_LITE_CHAOS"));
    }

    /// <summary>
    /// FW_LITE_CHAOS=true injects chaos into 30% of requests; a number between 0 and 1 (e.g. 1.0) sets the
    /// rate directly, which makes a specific failure reproducible instead of a dice roll.
    /// </summary>
    private static double ChaosInjectionRate()
    {
        var value = Environment.GetEnvironmentVariable("FW_LITE_CHAOS");
        return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var rate)
            ? Math.Clamp(rate, 0, 1)
            : 0.3;
    }

    private static void ConfigureHttpClientChaos(IHttpClientBuilder builder)
    {
        builder.AddResilienceHandler("chaos",
            pipelineBuilder =>
            {
                var injectionRate = ChaosInjectionRate();
                pipelineBuilder.AddChaosLatency(injectionRate, TimeSpan.FromSeconds(5))
                    .AddChaosFault(new ChaosFaultStrategyOptions
                    {
                        InjectionRate = injectionRate,
                        FaultGenerator = new FaultGenerator()
                            .AddException(() => new InvalidOperationException("Chaos injected fault"))
                            //what SocketsHttpHandler throws when DNS is unreachable, e.g. while a VPN is still connecting
                            .AddException(() => new HttpRequestException("No such host is known. (chaos)",
                                new SocketException((int)SocketError.HostNotFound)))
                    })
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
