using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace LcmCrdt.Tests;

public static class LcmCrdtTestsKernel
{
    public static IServiceCollection AddTestLcmCrdtClient(this IServiceCollection services, CrdtProject? project = null)
    {
        services.TryAddSingleton<IConfiguration>(new ConfigurationRoot([]));
        services.AddLogging(builder => builder.AddDebug());
        services.AddLcmCrdtClient();
        if (project is not null)
        {
            services.AddScoped<CurrentProjectService>(provider =>
            {
                var currentProjectService = ActivatorUtilities.CreateInstance<CurrentProjectService>(provider);
                currentProjectService.SetupProjectContextForNewDb(project);
                return currentProjectService;
            });
        }
        return services;
    }
}
