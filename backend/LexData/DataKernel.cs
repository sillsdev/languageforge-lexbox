using LexCore.Entities;
using LexData.Configuration;
using LexData.Entities;
using LinqToDB;
using LinqToDB.AspNet.Logging;
using LinqToDB.EntityFrameworkCore;
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
        services.AddDbContext<LexBoxDbContext>((serviceProvider, options) =>
        {
            options.EnableDetailedErrors();
            options.UseNpgsql(serviceProvider.GetRequiredService<IOptions<DbConfig>>().Value.LexBoxConnectionString);
            options.UseLinqToDB(builder =>
            {
                var mappingSchema = new MappingSchema();
                var mappingBuilder =
                new FluentMappingBuilder(mappingSchema)
                    .HasAttribute<ServerCommit>(new ColumnAttribute(
                        $"{nameof(ServerCommit.HybridDateTime)}_{nameof(HybridDateTime.DateTime)}",
                        $"{nameof(ServerCommit.HybridDateTime)}.{nameof(HybridDateTime.DateTime)}"))
                    .HasAttribute<ServerCommit>(new ColumnAttribute(
                        $"{nameof(ServerCommit.HybridDateTime)}_{nameof(HybridDateTime.Counter)}",
                        $"{nameof(ServerCommit.HybridDateTime)}.{nameof(HybridDateTime.Counter)}"));
                mappingBuilder.ConfigureEntity<Project, ProjectEntityConfiguration>();
                mappingBuilder.ConfigureEntity<Organization, OrganizationEntityConfiguration>();
                mappingBuilder.ConfigureEntity<OrgProjects, OrgProjectsEntityConfiguration>();
                mappingBuilder.Build();
                builder.AddMappingSchema(mappingSchema);
                var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
                if (loggerFactory is not null)
                    builder.AddCustomOptions(dataOptions => dataOptions.UseLoggerFactory(loggerFactory));
            });
            //todo remove this once this bug is fixed: https://github.com/dotnet/efcore/issues/35110
            //we ended up not upgrading to EF Core 9, so this was disabled for now, may or may not be needed in the future
            // options.ConfigureWarnings(builder => builder.Ignore(RelationalEventId.PendingModelChangesWarning));
            if (useOpenIddict) options.UseOpenIddict();
#if DEBUG
            options.EnableSensitiveDataLogging();
#endif
        }, dbContextLifeTime);
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

    private static void ConfigureEntity<T, TConfig>(this FluentMappingBuilder builder) where TConfig : ILinq2DbEntityConfiguration<T>
    {
        TConfig.ConfigureLinq2Db(builder.Entity<T>());
    }
}
