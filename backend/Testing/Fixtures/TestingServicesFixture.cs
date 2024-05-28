using LexData;
using LexData.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;

namespace Testing.Fixtures;

[CollectionDefinition(nameof(TestingServicesFixture))]
public class TestingServicesFixture : IAsyncLifetime, ICollectionFixture<TestingServicesFixture>
{
    private readonly ServiceProvider _serviceProvider;
    public TestingServicesFixture()
    {
        _serviceProvider = ConfigureServices(_ => { });
    }

    private static void ConfigureBaseServices(IServiceCollection services)
    {
        services.AddOptions<DbConfig>().Configure(config =>
        {
            config.LexBoxConnectionString = string.Join(";",
                "Database=lexbox-tests",
                "Host=localhost",
                "Port=5433",
                "Username=postgres",
                "Password=972b722e63f549938d07bd8c4ee5086c",
                "Include Error Detail=true");
        });
        services.AddSingleton<IHostEnvironment>(new HostingEnvironment
        {
            EnvironmentName = Environments.Development
        });
        services.AddSingleton<IConfiguration>(new ConfigurationManager());
        services.AddLexData(true, ServiceLifetime.Singleton);
    }

    public ServiceProvider ConfigureServices(Action<ServiceCollection>? configureServices = null)
    {
        var services = new ServiceCollection();
        ConfigureBaseServices(services);
        configureServices?.Invoke(services);
        return services.BuildServiceProvider();
    }

    public async Task InitializeAsync()
    {
        //delete before we init the db, we don't do this on cleanup so that we can inspect the database in case of issues.
        await _serviceProvider.GetDbContext().Database.EnsureDeletedAsync();
        //will run any migration/seeding services
        await Task.WhenAll(_serviceProvider.GetRequiredService<IEnumerable<IHostedService>>()
            .Select(s => s.StartAsync(CancellationToken.None)));
    }

    public async Task DisposeAsync()
    {
        await _serviceProvider.DisposeAsync();
    }
}

public static class ProviderExtensions
{
    public static LexBoxDbContext GetDbContext(this IServiceProvider provider)
    {
        return provider.GetRequiredService<LexBoxDbContext>();
    }
}
