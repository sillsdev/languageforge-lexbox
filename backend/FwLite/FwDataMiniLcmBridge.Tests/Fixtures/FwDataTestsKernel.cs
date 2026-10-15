using FwDataMiniLcmBridge.LcmUtils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FwDataMiniLcmBridge.Tests.Fixtures;

public static class FwDataTestsKernel
{
    public static IServiceCollection AddTestFwDataBridge(this IServiceCollection services, bool mockProjectLoader = true)
    {
        services.AddFwDataBridge();
        services.TryAddSingleton<IConfiguration>(_ => new ConfigurationRoot([]));
        //this path is typically not used for projects (they're in memory) but it is used for media
        services.Configure<FwDataBridgeConfig>(config =>
        {
            config.ProjectsFolder = Path.GetFullPath(Path.Combine(".", "fw-test-projects"));
            // the default resolves to an installed FieldWorks (if any), whose <lang>.ldml templates change WS defaults (e.g. Font).
            // Use liblcm's packaged templates (copied to the test output) so results don't depend on the machine.
            config.TemplatesFolder = Path.GetFullPath("Templates");
        });
        if (mockProjectLoader)
        {
            services.AddSingleton<MockFwProjectLoader>();
            services.AddSingleton<IProjectLoader>(sp => sp.GetRequiredService<MockFwProjectLoader>());
            services.AddSingleton<FieldWorksProjectList, MockFwProjectList>();
        }
        return services;
    }
}
