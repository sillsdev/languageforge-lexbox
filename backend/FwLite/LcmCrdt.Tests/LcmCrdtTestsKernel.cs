using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LcmCrdt.Tests;

public static class LcmCrdtTestsKernel
{
    public static IServiceCollection AddTestLcmCrdtClient(this IServiceCollection services, CrdtProject? project = null)
    {
        services.TryAddSingleton<IConfiguration>(new ConfigurationRoot([]));
        services.AddLcmCrdtClient();
        if (project is not null)
        {
            services.AddSingleton<CurrentProjectService>(provider =>
            {
                var currentProjectService = ActivatorUtilities.CreateInstance<CurrentProjectService>(provider);
                currentProjectService.SetupProjectContextForNewDb(project);
                return currentProjectService;
            });
        }
        return services;
    }
}
