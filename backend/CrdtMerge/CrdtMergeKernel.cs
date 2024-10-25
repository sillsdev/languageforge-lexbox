using FwDataMiniLcmBridge;
using FwLiteProjectSync;
using LcmCrdt;

namespace CrdtMerge;

public static class CrdtMergeKernel
{
    public static void AddCrdtMerge(this IServiceCollection services)
    {
        services
            .AddLogging(builder => builder.AddConsole().AddDebug().AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning));
        services.AddOptions<CrdtMergeConfig>()
            .BindConfiguration("SendReceiveConfig")
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddScoped<SendReceiveService>();
        services
            .AddLcmCrdtClient()
            .AddFwDataBridge()
            .AddFwLiteProjectSync();
    }
};
