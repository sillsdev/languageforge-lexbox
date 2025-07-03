using FwDataMiniLcmBridge.LcmUtils;
using FwDataMiniLcmBridge.Media;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MiniLcm;
using MiniLcm.Project;
using MiniLcm.Validators;

namespace FwDataMiniLcmBridge;

public static class FwDataBridgeKernel
{
    public const string FwDataApiKey = "FwDataApiKey";
    public static IServiceCollection AddFwDataBridge(this IServiceCollection services, ServiceLifetime lifetime = ServiceLifetime.Singleton)
    {
        services.AddMemoryCache();
        services.AddLogging();
        services.AddOptions<FwDataBridgeConfig>().BindConfiguration("FwDataBridge");
        services.AddLifetime<FwDataFactory>(lifetime);
        if (lifetime == ServiceLifetime.Singleton)
            services.AddSingleton<IHostedService>(sp => sp.GetRequiredService<FwDataFactory>());
        services.AddLifetime<FieldWorksProjectList>(lifetime);
        services.AddLifetime<IProjectProvider>(lifetime, s => s.GetRequiredService<FieldWorksProjectList>());
        services.AddLifetime<IProjectLoader, ProjectLoader>(lifetime);
        services.AddLifetime<IMediaAdapter, LocalMediaAdapter>(lifetime);
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

    private static IServiceCollection AddLifetime<TService>(this IServiceCollection services, ServiceLifetime lifetime)
    {
        services.Add(new ServiceDescriptor(typeof(TService), typeof(TService), lifetime));
        return services;
    }

    private static IServiceCollection AddLifetime<TService, TImplementation>(this IServiceCollection services, ServiceLifetime lifetime)
    {
        services.Add(new ServiceDescriptor(typeof(TService), typeof(TImplementation), lifetime));
        return services;
    }
    private static IServiceCollection AddLifetime<TService>(this IServiceCollection services, ServiceLifetime lifetime, Func<IServiceProvider, TService> implementationFactory) where TService : class
    {
        services.Add(new ServiceDescriptor(typeof(TService), factory: implementationFactory, lifetime));
        return services;
    }
}
