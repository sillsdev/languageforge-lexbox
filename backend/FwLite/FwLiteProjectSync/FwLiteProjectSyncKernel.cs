using Microsoft.Extensions.DependencyInjection;
using MiniLcm.Project;

namespace FwLiteProjectSync;

public static class FwLiteProjectSyncKernel
{
    public static IServiceCollection AddFwLiteProjectSync(this IServiceCollection services)
    {
        services.AddScoped<CrdtFwdataProjectSyncService>();
        services.AddScoped<ProjectSnapshotService>();
        services.AddScoped<MiniLcmImport>();
        services.AddScoped<IProjectImport>(s => s.GetRequiredService<MiniLcmImport>());
        return services;
    }
}
