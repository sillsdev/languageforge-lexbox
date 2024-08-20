using FwDataMiniLcmBridge.LcmUtils;
using Microsoft.Extensions.DependencyInjection;

namespace FwDataMiniLcmBridge.Tests.Fixtures;

public static class FwDataTestsKernel
{
    public static IServiceCollection AddTestFwDataBridge(this IServiceCollection services)
    {
        services.AddFwDataBridge();
        services.AddSingleton<MockFwProjectLoader>();
        services.AddSingleton<IProjectLoader>(sp => sp.GetRequiredService<MockFwProjectLoader>());
        services.AddSingleton<FieldWorksProjectList, MockFwProjectList>();
        return services;
    }
}
