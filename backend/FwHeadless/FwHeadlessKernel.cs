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
        services.AddScoped<SendReceiveService>();
        services.AddScoped<ProjectLookupService>();
        services.AddScoped<LogSanitizerService>();
        services.AddScoped<SafeLoggingProgress>();
        services
            .AddLcmCrdtClient()
            .AddFwDataBridge()
            .AddFwLiteProjectSync();
        services.AddScoped<CrdtSyncService>();
        services.AddTransient<HttpClientAuthHandler>();
        services.AddHttpClient(LexboxHttpClientName,
            (provider, client) =>
            {
                client.BaseAddress = new Uri(provider.GetRequiredService<IOptions<FwHeadlessConfig>>().Value.LexboxUrl);
            }).AddHttpMessageHandler<HttpClientAuthHandler>();
    }
}
