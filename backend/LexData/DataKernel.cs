using LexData.Configuration;
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using LinqToDB.Extensions.Logging;
using LinqToDB.Mapping;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SIL.Harmony.Core;

namespace LexData;

public static class DataKernel
{
    public static void AddLexData(this IServiceCollection services,
        bool autoApplyMigrations,
        bool useOpenIddict = true,
        bool useSeeding = true,
        ServiceLifetime dbContextLifeTime = ServiceLifetime.Scoped)
    {
        if (useSeeding)
            services.AddScoped<SeedingData>();

        LinqToDBForEFTools.Initialize();
        services.AddPooledDbContextFactory<LexBoxDbContext>((serviceProvider, options) =>
        {
            options.EnableDetailedErrors();
            options.UseNpgsql(serviceProvider.GetRequiredService<IOptions<DbConfig>>().Value.LexBoxConnectionString);
            options.UseLinqToDB(builder =>
            {
                var mappingSchema = new MappingSchema();
                new FluentMappingBuilder(mappingSchema)
                    .HasAttribute<ServerCommit>(new ColumnAttribute(
                        $"{nameof(ServerCommit.HybridDateTime)}_{nameof(HybridDateTime.DateTime)}",
                        $"{nameof(ServerCommit.HybridDateTime)}.{nameof(HybridDateTime.DateTime)}"))
                    .HasAttribute<ServerCommit>(new ColumnAttribute(
                        $"{nameof(ServerCommit.HybridDateTime)}_{nameof(HybridDateTime.Counter)}",
                        $"{nameof(ServerCommit.HybridDateTime)}.{nameof(HybridDateTime.Counter)}"))
                    .Build();
                builder.AddMappingSchema(mappingSchema);
                var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
                if (loggerFactory is not null)
                    builder.AddCustomOptions(dataOptions => dataOptions.UseLoggerFactory(loggerFactory));
            });
            if (useOpenIddict) options.UseOpenIddict();
#if DEBUG
            options.EnableSensitiveDataLogging();
#endif
        });
        //we're now using a pooled db context factory, but we don't want to rewrite everything else to use it, so we'll expose `LexBoxDbContext` as a service
        //it'll get disposed properly when the service scope is disposed, just like before.
        services.Add(new ServiceDescriptor(typeof(LexBoxDbContext), sp => sp.GetRequiredService<IDbContextFactory<LexBoxDbContext>>().CreateDbContext(), dbContextLifeTime));
        services.AddLogging();
        services.AddHealthChecks()
            .AddDbContextCheck<LexBoxDbContext>(customTestQuery: (context, token) => context.HeathCheck(token));
        if (autoApplyMigrations)
            services.AddHostedService<DbStartupService>();
        services.AddOptions<DbConfig>()
        .BindConfiguration(nameof(DbConfig))
        .ValidateDataAnnotations()
        .ValidateOnStart();
    }

    public static void ConfigureDbModel(this IServiceCollection services, Action<ModelBuilder> configureDbModel)
    {

        services.AddSingleton(new ConfigureDbModel(configureDbModel));
    }
}
