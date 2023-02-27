using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LexData;

public class DbStartupService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;

    public DbStartupService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // prevents code from running when using the ef tools migrations.
        if (EF.IsDesignTime) return;
        await using var serviceScope = _serviceProvider.CreateAsyncScope();
        var dbContext = serviceScope.ServiceProvider.GetRequiredService<LexBoxDbContext>();
        var environment = serviceScope.ServiceProvider.GetRequiredService<IHostEnvironment>();
        await dbContext.Database.MigrateAsync(cancellationToken);
        if (environment.IsDevelopment())
        {
            await serviceScope.ServiceProvider.GetRequiredService<SeedingData>().SeedIfNoUsers();
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}