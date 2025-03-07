﻿using FwDataMiniLcmBridge.LcmUtils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FwDataMiniLcmBridge.Tests.Fixtures;

public static class FwDataTestsKernel
{
    public static IServiceCollection AddTestFwDataBridge(this IServiceCollection services, bool mockProjectLoader = true)
    {
        services.AddFwDataBridge();
        services.AddSingleton<IConfiguration>(_ => new ConfigurationRoot([]));
        if (mockProjectLoader)
        {
            services.AddSingleton<MockFwProjectLoader>();
            services.AddSingleton<IProjectLoader>(sp => sp.GetRequiredService<MockFwProjectLoader>());
            services.AddSingleton<FieldWorksProjectList, MockFwProjectList>();
        }
        return services;
    }
}
