using LexData.Configuration;
using LexData.Redmine;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace LexData;

public static class DataKernel
{
    public static void AddLexData(this IServiceCollection services, bool autoApplyMigrations, ServiceLifetime dbContextLifeTime = ServiceLifetime.Scoped)
    {
        services.AddScoped<SeedingData>();
        services.AddDbContext<LexBoxDbContext>((serviceProvider, options) =>
        {
            options.EnableDetailedErrors();
            options.UseNpgsql(serviceProvider.GetRequiredService<IOptions<DbConfig>>().Value.LexBoxConnectionString);
            options.UseProjectables();
        }, dbContextLifeTime);
        services.AddLogging();
        services.AddHealthChecks()
            .AddDbContextCheck<LexBoxDbContext>(customTestQuery: (context, token) => context.HeathCheck(token));
        services.AddDbContext<PublicRedmineDbContext>((serviceProvider, options) =>
        {
            options.EnableDetailedErrors();
            var connectionString =
                serviceProvider.GetRequiredService<IOptions<DbConfig>>().Value.RedmineConnectionString ??
                throw new ArgumentNullException("RedmineConnectionString");
            options.UseMySql(connectionString, ServerVersion.Parse("11.1.2-mariadb"));
        }, dbContextLifeTime);
        services.AddDbContext<PrivateRedmineDbContext>((serviceProvider, options) =>
        {
            options.EnableDetailedErrors();
            var connectionString =
                serviceProvider.GetRequiredService<IOptions<DbConfig>>().Value.RedmineConnectionString ??
                throw new ArgumentNullException("RedmineConnectionString");
            connectionString = connectionString.Replace("database=languagedepot", "database=languagedepotpvt");
            options.UseMySql(connectionString, ServerVersion.Parse("11.1.2-mariadb"));
        }, dbContextLifeTime);
        if (autoApplyMigrations)
            services.AddHostedService<DbStartupService>();
        services.AddOptions<DbConfig>()
        .BindConfiguration("DbConfig")
        .ValidateDataAnnotations()
        .ValidateOnStart();
    }
}
