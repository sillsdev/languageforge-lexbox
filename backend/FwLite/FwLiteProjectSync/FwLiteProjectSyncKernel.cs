using Microsoft.Extensions.DependencyInjection;

namespace FwLiteProjectSync;

public static class FwLiteProjectSyncKernel
{
    public static IServiceCollection AddFwLiteProjectSync(this IServiceCollection services)
    {
        services.AddSingleton<CrdtFwdataProjectSyncService>();
        services.AddSingleton<MiniLcmImport>();
        return services;
    }
}
