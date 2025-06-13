using FwDataMiniLcmBridge;
using FwHeadless.Services;
using FwLiteProjectSync;
using LcmCrdt;
using Microsoft.Extensions.Options;

namespace FwHeadless;

public static class FwHeadlessKernel
{
    public const string LexboxHttpClientName = "LexboxHttpClient";
    public static void AddFwHeadless(this IServiceCollection services)
    {
        services
            .AddLogging(builder => builder.AddConsole().AddDebug().AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning));
        services.AddOptions<FwHeadlessConfig>()
            .BindConfiguration("FwHeadlessConfig")
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddSingleton<SyncJobStatusService>();
        services.AddScoped<SendReceiveService>();
        services.AddScoped<ProjectLookupService>();
        services.AddScoped<LogSanitizerService>();
        services.AddScoped<SafeLoggingProgress>();
        services
            .AddLcmCrdtClientCore()
            .AddFwDataBridge()
            .AddFwLiteProjectSync();

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
    }
}
