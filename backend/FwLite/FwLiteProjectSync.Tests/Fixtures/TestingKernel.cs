using FwDataMiniLcmBridge;
using FwDataMiniLcmBridge.Tests.Fixtures;
using LcmCrdt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FwLiteProjectSync.Tests.Fixtures;

public static class TestingKernel
{
    public static IServiceCollection AddSyncServices(this IServiceCollection services, string projectName, bool mockFwProjectLoader = true)
    {
        return services.AddLcmCrdtClient()
            .AddTestFwDataBridge(mockFwProjectLoader)
            .AddFwLiteProjectSync()
            .AddSingleton<ProjectContext>(new MockProjectContext(null))
            .Configure<FwDataBridgeConfig>(c => c.ProjectsFolder = Path.Combine(".", projectName, "FwData"))
            .Configure<LcmCrdtConfig>(c => c.ProjectPath = Path.Combine(".", projectName, "LcmCrdt"))
            .AddLogging(builder => builder.AddDebug());
    }

    public class MockProjectContext(CrdtProject? project) : ProjectContext
    {
        public override CrdtProject? Project { get; set; } = project;
    }

}
