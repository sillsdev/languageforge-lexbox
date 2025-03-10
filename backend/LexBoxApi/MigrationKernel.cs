using Microsoft.Extensions.Hosting.Internal;
using LexData;
using LexBoxApi.Auth;

namespace LexBoxApi;

public static class MigrationKernel
{
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
        // Required for (re-)seeding OAuth applications
        builder.Services.AddOpenIddict().AddCore(options =>
        {
            options.UseEntityFrameworkCore().UseDbContext<LexBoxDbContext>();
        });
        var host = builder.Build();
        await host.StartAsync();
        await host.StopAsync();
        if (!DbStartupService.MigrateExecuted) throw new ApplicationException("database migrations not executed");
        if (!SeedingData.OAuthSeeded) throw new ApplicationException("oauth not seeded/updated");
    }
}
