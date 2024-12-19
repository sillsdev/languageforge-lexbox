using Microsoft.Extensions.DependencyInjection;
using MiniLcm.Project;

namespace FwLiteProjectSync;

public static class FwLiteProjectSyncKernel
{
    public static IServiceCollection AddFwLiteProjectSync(this IServiceCollection services)
    {
        services.AddSingleton<CrdtFwdataProjectSyncService>();
        services.AddSingleton<MiniLcmImport>();
        services.AddSingleton<IProjectImport>(s => s.GetRequiredService<MiniLcmImport>());
        return services;
    }
}
