using FwDataMiniLcmBridge;
using FwHeadless.Services;
using FwLiteProjectSync;
using LcmCrdt;

namespace FwHeadless;

public static class FwHeadlessKernel
{
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
    }
};
