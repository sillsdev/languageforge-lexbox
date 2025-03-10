using LexBoxApi.Auth;
using LexData;
using LexData.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Testing.Fixtures;

[CollectionDefinition(nameof(TestingServicesFixture))]
public class TestingServicesFixture : IAsyncLifetime, ICollectionFixture<TestingServicesFixture>
{
    private readonly ServiceProvider _serviceProvider;
    private readonly string _dbName;

    private TestingServicesFixture(string dbName)
    {
        _dbName = dbName;
        _serviceProvider = ConfigureServices(_ => { });
    }

    public TestingServicesFixture(): this("lexbox-tests")
    {
    }

    public static TestingServicesFixture Create(string dbName)
    {
        return new TestingServicesFixture(dbName);
    }

    private static void ConfigureBaseServices(IServiceCollection services, string dbName)
    {
        services.AddOptions<DbConfig>().Configure(config =>
        {
            config.LexBoxConnectionString = string.Join(";",
                $"Database={dbName}",
                "Host=localhost",
                "Port=5433",
                "Username=postgres",
                "Password=972b722e63f549938d07bd8c4ee5086c",
                "Include Error Detail=true");
        });
        var environment = new HostingEnvironment
        {
            EnvironmentName = Environments.Development
        };
        services.AddSingleton<IHostEnvironment>(environment);
        services.AddSingleton<IConfiguration>(new ConfigurationManager());
        services.AddLexData(true, dbContextLifeTime: ServiceLifetime.Singleton);
        AuthKernel.AddOpenId(services, environment);
        services.AddLogging(builder => builder.AddDebug());
    }

    public ServiceProvider ConfigureServices(Action<ServiceCollection>? configureServices = null)
    {
        var services = new ServiceCollection();
        ConfigureBaseServices(services, _dbName);
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
