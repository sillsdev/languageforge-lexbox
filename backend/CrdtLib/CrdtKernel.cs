using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Crdt.Core;
using CrdtLib.Changes;
using CrdtLib.Db;
using CrdtLib.Entities;
using CrdtLib.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CrdtLib;

public static class CrdtKernel
{
    public static IServiceCollection AddCrdtData(this IServiceCollection services,
        Action<DbContextOptionsBuilder> configureOptions,
        Action<CrdtConfig> configureCrdt)
    {
        return AddCrdtData(services, (_, builder) => configureOptions(builder), configureCrdt);
    }

    public static IServiceCollection AddCrdtData(this IServiceCollection services,
        Action<IServiceProvider, DbContextOptionsBuilder> configureOptions,
        Action<CrdtConfig> configureCrdt)
    {
        services.AddOptions<CrdtConfig>().Configure(configureCrdt);
        services.AddSingleton(sp => new JsonSerializerOptions(JsonSerializerDefaults.General)
        {
            TypeInfoResolver = new DefaultJsonTypeInfoResolver
            {
                Modifiers =
                {
                    sp.GetRequiredService<IOptions<CrdtConfig>>().Value.MakeJsonTypeModifier()
                }
            }
        });
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<IHybridDateTimeProvider>(provider =>
        {
            //todo, if this causes issues getting the order correct, we can update the last date time after the db is created
            //as long as it's before we get a date time from the provider
            var hybridDateTime = provider.GetRequiredService<CrdtRepository>().GetLatestDateTime();
            hybridDateTime ??= HybridDateTimeProvider.DefaultLastDateTime;
            return ActivatorUtilities.CreateInstance<HybridDateTimeProvider>(provider, hybridDateTime);
        });
        services.AddDbContext<CrdtDbContext>((provider, builder) =>
            {
                configureOptions(provider, builder);
                builder
                    .AddInterceptors(provider.GetServices<IInterceptor>().ToArray())
                    .EnableDetailedErrors()
                    .EnableSensitiveDataLogging();
            },
            ServiceLifetime.Scoped);
        services.AddScoped<CrdtRepository>();
        services.AddScoped<DataModel>();
        return services;
    }
}
