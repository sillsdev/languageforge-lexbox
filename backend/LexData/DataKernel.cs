using LexData.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace LexData;

public static class DataKernel
{
    public static void AddLexData(this IServiceCollection services,
        bool autoApplyMigrations,
        ServiceLifetime dbContextLifeTime = ServiceLifetime.Scoped)
    {
        services.AddScoped<SeedingData>();
        services.AddDbContext<LexBoxDbContext>((serviceProvider, options) =>
        {
            options.EnableDetailedErrors();
            options.UseNpgsql(serviceProvider.GetRequiredService<IOptions<DbConfig>>().Value.LexBoxConnectionString);
            options.UseProjectables();
            options.UseOpenIddict();
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
}
