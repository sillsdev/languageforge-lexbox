using FwDataMiniLcmBridge.LcmUtils;
using Microsoft.Extensions.DependencyInjection;
using MiniLcm;

namespace FwDataMiniLcmBridge;

public static class FwDataBridgeKernel
{
    public const string FwDataApiKey = "FwDataApiKey";
    public static IServiceCollection AddFwDataBridge(this IServiceCollection services)
    {
        services.AddMemoryCache();
        services.AddLogging();
        services.AddOptions<FwDataBridgeConfig>().BindConfiguration("FwDataBridge");
        services.AddSingleton<FwDataFactory>();
        services.AddSingleton<FieldWorksProjectList>();
        services.AddSingleton<IProjectLoader, ProjectLoader>();
        services.AddKeyedScoped<ILexboxApi>(FwDataApiKey, (provider, o) => provider.GetRequiredService<FwDataFactory>().GetCurrentFwDataMiniLcmApi(true));
        services.AddSingleton<FwDataProjectContext>();
        return services;
    }
}
