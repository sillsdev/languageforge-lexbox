using FwDataMiniLcmBridge;
using FwDataMiniLcmBridge.Media;
using FwHeadless.Media;
using FwHeadless.Services;
using FwLiteProjectSync;
using LcmCrdt;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using MiniLcm.Project;
using MiniLcmWeb;

namespace FwHeadless;

public static class FwHeadlessKernel
{
    public const string LexboxHttpClientName = "LexboxHttpClient";
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
        services.AddScoped<ProjectCreationService>();
        services.AddScoped<LogSanitizerService>();
        services.AddScoped<SafeLoggingProgress>();
        // Singleton so its file lock is shared across requests and the sync worker
        services.AddSingleton<IProjectMetadataService, ProjectMetadataService>();
        services
            .AddLcmCrdtClientCore()
            .AddFwDataBridge(ServiceLifetime.Scoped)
            .AddFwLiteProjectSync();
        // The LCM project templates (NewLangProj.fwdata etc.) ship in our output (see FwHeadless.csproj).
        // FwDataBridgeConfig's default TemplatesFolder points at the FieldWorks user-data dir, which
        // doesn't exist in the container; point it at the shipped templates unless overridden in config.
        services.Configure<FwDataBridgeConfig>(config =>
        {
            if (config.TemplatesFolder == new FwDataBridgeConfig().TemplatesFolder)
                config.TemplatesFolder = Path.Combine(AppContext.BaseDirectory, "Templates");
        });
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
        services.AddHttpClient(LexboxHttpClientName,
            (provider, client) =>
            {
                client.BaseAddress = new Uri(provider.GetRequiredService<IOptions<FwHeadlessConfig>>().Value.LexboxUrl);
            }).AddHttpMessageHandler<HttpClientAuthHandler>();
        return services;
    }
}
