using FwDataMiniLcmBridge;
using FwDataMiniLcmBridge.Tests.Fixtures;
using LcmCrdt;
using LcmCrdt.Tests;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FwLiteProjectSync.Tests.Fixtures;

public static class TestingKernel
{
    public static IServiceCollection AddSyncServices(this IServiceCollection services, string projectName, bool mockFwProjectLoader = true)
    {
        return services.AddTestLcmCrdtClient()
            .AddTestFwDataBridge(mockFwProjectLoader)
            .AddFwLiteProjectSync()
            .Configure<FwDataBridgeConfig>(c => c.ProjectsFolder = Path.Combine(".", projectName, "FwData"))
            .Configure<LcmCrdtConfig>(c => c.ProjectPath = Path.Combine(".", projectName, "LcmCrdt"))
            .AddLogging(builder => builder.AddDebug()
                .SetMinimumLevel(LogLevel.Warning));
    }
}
