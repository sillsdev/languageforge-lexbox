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
        services.Configure<LcmCrdtConfig>(config => config.EnableProjectDataFileCache = false);
        if (project is not null)
        {
            var initializedNewDb = false;
            services.AddScoped(provider =>
            {
                var currentProjectService = ActivatorUtilities.CreateInstance<CurrentProjectService>(provider);
                if (!initializedNewDb)
                {
                    // this init code is practical in most cases, but if it happens a second time,
                    // we assume the code intentionally created a seperate scope that it will explicitly initialize
                    currentProjectService.SetupProjectContextForNewDb(project);
                    initializedNewDb = true;
                }
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
