using LexData;
using LexData.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using SIL.Progress;
using Testing.Logging;
using Testing.Services;

namespace Testing.Fixtures;

public class TestingServicesFixture : IAsyncLifetime
{
    public ServiceCollection Services { get; } = new();
    private Lazy<ServiceProvider> _lazyServiceProvider;
    public ServiceProvider ServiceProvider => _lazyServiceProvider.Value;
    public LexBoxDbContext DbContext => ServiceProvider.GetRequiredService<LexBoxDbContext>();

    public TestingServicesFixture()
    {
        Services.AddOptions<DbConfig>().Configure(config =>
        {
            config.LexBoxConnectionString = string.Join(";",
                "Database=lexbox-tests",
                "Host=localhost",
                "Port=5433",
                "Username=postgres",
                "Password=972b722e63f549938d07bd8c4ee5086c",
                "Include Error Detail=true");
        });
        Services.AddSingleton<IHostEnvironment>(new HostingEnvironment { EnvironmentName = Environments.Development });
        Services.AddSingleton<IConfiguration>(new ConfigurationManager());
        Services.AddScoped<SendReceiveService>();
        Services.AddScoped<IProgress, XunitStringBuilderProgress>();
        Services.AddLexData();
        _lazyServiceProvider = new(Services.BuildServiceProvider);
    }

    public async Task InitializeAsync()
    {
        //delete before we init the db, we don't do this on cleanup so that we can inspect the database in case of issues.
        await DbContext.Database.EnsureDeletedAsync();
        await Task.WhenAll(ServiceProvider.GetRequiredService<IEnumerable<IHostedService>>()
            .Select(s => s.StartAsync(CancellationToken.None)));

        //invalidate the service provider so that the Tests get a new one with their registrations.
        _lazyServiceProvider = new(Services.BuildServiceProvider);
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }
}
