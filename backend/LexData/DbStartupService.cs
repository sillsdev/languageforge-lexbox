using System.Diagnostics;
using System.Net.Sockets;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Logging;
using Npgsql;

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
        var timeoutToken = CancellationTokenSource
            .CreateLinkedTokenSource(cancellationToken, new CancellationTokenSource(TimeSpan.FromSeconds(30)).Token)
            .Token;
        int counter = 1;
        while (!await TryMigrate(dbContext, timeoutToken))
        {
            timeoutToken.ThrowIfCancellationRequested();
            _logger.LogInformation("Waiting for database connection... {Counter}", counter);
            counter++;
            await Task.Delay(TimeSpan.FromSeconds(counter), cancellationToken);
        }

        var environment = serviceScope.ServiceProvider.GetRequiredService<IHostEnvironment>();
        var seedingData = serviceScope.ServiceProvider.GetRequiredService<SeedingData>();
        if (environment.IsDevelopment() || environment.IsStaging())
        {
            await seedingData.SeedIfNoUsers(cancellationToken);
        }
        else
        {
            await seedingData.SeedOAuth(cancellationToken);
        }

        _migrateExecuted = true;
        var elapsedTime = Stopwatch.GetElapsedTime(startTime);
        _logger.LogInformation($"Migrations applied successfully ({elapsedTime:s\\.fff} sec)");
    }

    private async Task<bool> TryMigrate(DbContext dbContext, CancellationToken cancellationToken)
    {
        try
        {
            // there's a method to check if we can connect, but that won't work when the database is not created yet.
            await dbContext.Database.MigrateAsync(cancellationToken);
            return true;
        }
        //copied from NpgsqlDatabaseCreator.Exists
        catch (PostgresException e) when (e is { SqlState: "3D000" })
        {
            return false;
        }
        catch (NpgsqlException e) when (e.InnerException is IOException
                                        {
                                            InnerException: SocketException
                                            {
                                                SocketErrorCode: SocketError.ConnectionReset
                                            }
                                        })
        {
            return false;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
