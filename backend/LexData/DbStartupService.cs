using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LexData;

public class DbStartupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;

    public DbStartupService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    { // prevents code from running when using the ef tools migrations.
        if (EF.IsDesignTime) return;
        await using var serviceScope = _serviceProvider.CreateAsyncScope();
        var dbContext = serviceScope.ServiceProvider.GetRequiredService<LexBoxDbContext>();
        var environment = serviceScope.ServiceProvider.GetRequiredService<IHostEnvironment>();
        await dbContext.Database.MigrateAsync(stoppingToken);
        if (environment.IsDevelopment() || environment.IsStaging())
        {
            await serviceScope.ServiceProvider.GetRequiredService<SeedingData>().SeedIfNoUsers(stoppingToken);
        }
    }
}