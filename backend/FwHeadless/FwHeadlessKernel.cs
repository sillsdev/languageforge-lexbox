using FwDataMiniLcmBridge;
using FwDataMiniLcmBridge.Media;
using FwHeadless.Media;
using FwHeadless.Services;
using FwLiteProjectSync;
using LcmCrdt;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using MiniLcm.Project;

namespace FwHeadless;

public static class FwHeadlessKernel
{
    public const string LexboxHttpClientName = "LexboxHttpClient";
    public static IServiceCollection AddFwHeadless(this IServiceCollection services)
    {
        services
            .AddLogging(builder => builder.AddConsole().AddDebug().AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning));
        services.AddOptions<FwHeadlessConfig>()
            .BindConfiguration("FwHeadlessConfig")
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddSingleton<ISyncJobStatusService, SyncJobStatusService>();
        services.AddScoped<ISendReceiveService, SendReceiveService>();
        services.AddScoped<IProjectLookupService, ProjectLookupService>();
        services.AddScoped<ProjectDeletionService>();
        services.AddScoped<IProjectMetadataService, ProjectMetadataService>();
        services.AddScoped<LogSanitizerService>();
        services.AddScoped<SafeLoggingProgress>();
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
