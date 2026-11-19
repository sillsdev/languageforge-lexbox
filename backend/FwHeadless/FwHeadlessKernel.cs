using FwDataMiniLcmBridge;
using FwDataMiniLcmBridge.Media;
using FwHeadless.Media;
using FwHeadless.Services;
using FwLiteProjectSync;
using LcmCrdt;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using MiniLcm.Project;
using MiniLcmWeb;

namespace FwHeadless;

public static class FwHeadlessKernel
{
    public const string LexboxHttpClientName = "LexboxHttpClient";
    public const string LexboxSyncHttpClientName = "LexboxSyncHttpClient";
    public static IServiceCollection AddFwHeadless(this IServiceCollection services)
    {
        services
            .AddLogging(builder => builder.AddConsole().AddDebug()
                .AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning)
                // LinqToDB logs every SQL command (with parameter values) at Information: 3 records per query
                .AddFilter("LinqToDB", LogLevel.Warning));
        services.AddOptions<FwHeadlessConfig>()
            .BindConfiguration("FwHeadlessConfig")
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddSingleton<ISyncJobStatusService, SyncJobStatusService>();
        services.AddScoped<ISendReceiveService, SendReceiveService>();
        services.AddScoped<IProjectLookupService, ProjectLookupService>();
        services.AddScoped<ProjectDeletionService>();
        services.AddScoped<LogSanitizerService>();
        services.AddScoped<SafeLoggingProgress>();
        // Singleton so its file lock is shared across requests and the sync worker
        services.AddSingleton<IProjectMetadataService, ProjectMetadataService>();
        services
            .AddLcmCrdtClientCore()
            .AddFwDataBridge(ServiceLifetime.Scoped)
            .AddFwLiteProjectSync();
        services.RemoveAll(typeof(IMediaAdapter));
        services.AddScoped<IMediaAdapter, LexboxFwDataMediaAdapter>();
        services.AddScoped<MediaFileService>();
        services.AddScoped<IServerHttpClientProvider, LexboxServerHttpClientProvider>();

        services.AddSingleton<SyncHostedService>();
        services.AddHostedService(s => s.GetRequiredService<SyncHostedService>());
        services.AddSingleton<HarmonySyncScheduler>();
        services.AddHostedService(s => s.GetRequiredService<HarmonySyncScheduler>());
        services.AddMiniLcmRouteServices();

        services.AddScoped<CrdtSyncService>();
        services.AddScoped<ProjectContextFromIdService>();
        services.AddTransient<HttpClientAuthHandler>();
        services.AddLexboxHttpClient(LexboxHttpClientName);
        // Lexbox stores a push in one transaction, so a project's whole unpushed history must fit in one attempt.
        services.AddLexboxHttpClient(LexboxSyncHttpClientName, options =>
        {
            options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(30);
            options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(90);
            // must be at least twice the attempt timeout
            options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(60);
        });
        return services;
    }

    private static IHttpClientBuilder AddLexboxHttpClient(this IServiceCollection services, string name,
        Action<HttpStandardResilienceOptions>? configureResilience = null)
    {
        var builder = services.AddHttpClient(name,
            (provider, client) =>
            {
                client.BaseAddress = new Uri(provider.GetRequiredService<IOptions<FwHeadlessConfig>>().Value.LexboxUrl);
            });
        if (configureResilience is not null)
        {
#pragma warning disable EXTEXP0001
            builder.RemoveAllResilienceHandlers()
#pragma warning restore EXTEXP0001
                .AddStandardResilienceHandler(configureResilience);
        }
        // Auth goes after the resilience handler (the default one is added first), so every retry re-runs auth.
        builder.AddHttpMessageHandler<HttpClientAuthHandler>();
        return builder;
    }
}
