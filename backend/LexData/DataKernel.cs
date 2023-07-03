using LexData.Configuration;
using LexData.Redmine;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace LexData;

public static class DataKernel
{
    public static void AddLexData(this IServiceCollection services, bool autoApplyMigrations)
    {
        services.AddScoped<SeedingData>();
        services.AddDbContext<LexBoxDbContext>((serviceProvider, options) =>
        {
            options.EnableDetailedErrors();
            options.UseNpgsql(serviceProvider.GetRequiredService<IOptions<DbConfig>>().Value.LexBoxConnectionString);
            options.UseProjectables();
        });
        services.AddLogging();
        services.AddHealthChecks()
            .AddDbContextCheck<LexBoxDbContext>(customTestQuery: (context, token) => context.HeathCheck(token));
        services.AddDbContext<RedmineDbContext>((serviceProvider, options) =>
        {
            options.EnableDetailedErrors();
            options.UseMySQL(serviceProvider.GetRequiredService<IOptions<DbConfig>>().Value.RedmineConnectionString ?? throw new ArgumentNullException("RedmineConnectionString"));
        });
        if (autoApplyMigrations)
            services.AddHostedService<DbStartupService>();
        services.AddOptions<DbConfig>()
        .BindConfiguration("DbConfig")
        .ValidateDataAnnotations()
        .ValidateOnStart();
    }
}
