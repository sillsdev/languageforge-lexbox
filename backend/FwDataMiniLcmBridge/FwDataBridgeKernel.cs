using FwDataMiniLcmBridge.LcmUtils;
using Microsoft.Extensions.DependencyInjection;
using MiniLcm;

namespace FwDataMiniLcmBridge;

public static class FwDataBridgeKernel
{
    public const string FwDataApiKey = "FwDataApiKey";
    public static IServiceCollection AddFwDataBridge(this IServiceCollection services)
    {
        services.AddSingleton<FwDataFactory>();
        //todo since this is scoped it gets created on each request (or hub method call), which opens the project file on each request
        //this is not ideal since opening the project file can be slow. It should be done once per hub connection.
        services.AddKeyedScoped<ILexboxApi>(FwDataApiKey, (provider, o) => provider.GetRequiredService<FwDataFactory>().GetCurrentFwDataMiniLcmApi());
        services.AddSingleton<FwDataProjectContext>();
        return services;
    }
}
