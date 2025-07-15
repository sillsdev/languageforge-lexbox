using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using MiniLcm.Project;

namespace LcmCrdt.Tests;

public static class LcmCrdtTestsKernel
{
    public static IServiceCollection AddTestLcmCrdtClient(this IServiceCollection services, CrdtProject? project = null)
    {
        services.TryAddSingleton<IConfiguration>(new ConfigurationRoot([]));
        services.AddLogging(builder => builder.AddDebug());
        services.AddSingleton<IServerHttpClientProvider, FakeHttpClientProvider>();
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

    private class FakeHttpClientProvider : IServerHttpClientProvider
    {
        public ValueTask<HttpClient> GetHttpClient()
        {
            throw new NotImplementedException();
        }

        public ValueTask<ConnectionStatus> ConnectionStatus(bool forceRefresh = false)
        {
            return ValueTask.FromResult(MiniLcm.Project.ConnectionStatus.Offline);
        }
    }
}
