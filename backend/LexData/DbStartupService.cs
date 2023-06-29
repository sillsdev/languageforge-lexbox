using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Logging;

namespace LexData;

public class DbStartupService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DbStartupService> _logger;
    private static bool _migrateExecuted = false;

    public static bool IsMigrationRequest(string[] args)
    {
        return args is ["migrate", ..];
    }

    public static async Task RunMigrationRequest(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);
        builder.Services.AddLogging();
        builder.Services.AddSingleton<IHostLifetime, ConsoleLifetime>();
        builder.Services.AddLexData(true);
        var host = builder.Build();
        await host.StartAsync();
        await host.StopAsync();
        if (!_migrateExecuted) throw new ApplicationException("database migrations not executed");
    }

    public DbStartupService(IServiceProvider serviceProvider, ILogger<DbStartupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // prevents code from running when using the ef tools migrations.
        if (EF.IsDesignTime) return;
        var startTime = Stopwatch.GetTimestamp();
        await using var serviceScope = _serviceProvider.CreateAsyncScope();
        var dbContext = serviceScope.ServiceProvider.GetRequiredService<LexBoxDbContext>();
        await dbContext.Database.MigrateAsync(cancellationToken);
        var environment = serviceScope.ServiceProvider.GetRequiredService<IHostEnvironment>();
        if (environment.IsDevelopment() || environment.IsStaging())
        {
            await serviceScope.ServiceProvider.GetRequiredService<SeedingData>().SeedIfNoUsers(cancellationToken);
        }

        _migrateExecuted = true;
        var elapsedTime = Stopwatch.GetElapsedTime(startTime);
        _logger.LogInformation($"Migrations applied successfully ({elapsedTime:s\\.fff} sec)");
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
