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
        services.AddSingleton<FwDataFactory>();
        services.AddKeyedScoped<ILexboxApi>(FwDataApiKey, (provider, o) => provider.GetRequiredService<FwDataFactory>().GetCurrentFwDataMiniLcmApi(true));
        services.AddSingleton<FwDataProjectContext>();
        return services;
    }
}
