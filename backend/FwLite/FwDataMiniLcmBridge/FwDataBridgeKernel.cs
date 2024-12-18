using FwDataMiniLcmBridge.LcmUtils;
using Microsoft.Extensions.DependencyInjection;
using MiniLcm;
using MiniLcm.Validators;

namespace FwDataMiniLcmBridge;

public static class FwDataBridgeKernel
{
    public const string FwDataApiKey = "FwDataApiKey";
    public static IServiceCollection AddFwDataBridge(this IServiceCollection services)
    {
        services.AddMemoryCache();
        services.AddLogging();
        services.AddOptions<FwDataBridgeConfig>().BindConfiguration("FwDataBridge");
        services.AddSingleton<FwDataFactory>();
        services.AddSingleton<FieldWorksProjectList>();
        services.AddSingleton<IProjectLoader, ProjectLoader>();
        services.AddKeyedScoped<IMiniLcmApi>(FwDataApiKey,
            (provider, o) =>
            {
                var projectList = provider.GetRequiredService<FieldWorksProjectList>();
                var projectContext = provider.GetRequiredService<FwDataProjectContext>();
                return projectList.OpenProject(projectContext.Project ?? throw new InvalidOperationException("No project is set in the context."));
            });
        services.AddMiniLcmValidators();
        services.AddScoped<FwDataProjectContext>();
        return services;
    }
}
