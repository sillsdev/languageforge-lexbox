using LexData.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace LexData;

public static class DataKernel
{
    public static void AddLexData(this IServiceCollection services,
        bool autoApplyMigrations,
        bool useOpenIddict = true,
        ServiceLifetime dbContextLifeTime = ServiceLifetime.Scoped)
    {
        services.AddScoped<SeedingData>();
        services.AddDbContext<LexBoxDbContext>((serviceProvider, options) =>
        {
            options.EnableDetailedErrors();
            options.UseNpgsql(serviceProvider.GetRequiredService<IOptions<DbConfig>>().Value.LexBoxConnectionString);
            options.UseProjectables();
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
}
