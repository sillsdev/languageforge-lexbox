using FwDataMiniLcmBridge;
using FwLiteProjectSync;
using LcmCrdt;

namespace CrdtMerge;

public static class CrdtMergeKernel
{
    public static void AddCrdtMerge(this IServiceCollection services)
    {
        services
            .AddLogging(builder => builder.AddConsole().AddDebug().AddConfiguration(new ConfigurationManager().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Logging:LogLevel:Microsoft.EntityFrameworkCore"] = "Warning"
            }).Build()));
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
