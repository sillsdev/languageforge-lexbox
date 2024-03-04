using LfClassicData.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Linq;

namespace LfClassicData;

public static class DataServiceKernel
{
    public static void Setup(IServiceCollection services)
    {
        BsonConfiguration.Setup();
        services.AddSingleton(provider =>
        {
            var configuration = provider.GetRequiredService<IConfiguration>();
            return BuildMongoClientSettings(configuration.GetValue<string>("Mongo:ConnectionString"), provider);
        });
        services.AddSingleton(provider => new MongoClient(provider.GetRequiredService<MongoClientSettings>()));

        services.AddSingleton<SystemDbContext>();
        services.AddSingleton<ProjectDbContext>();
    }

    public static MongoClientSettings BuildMongoClientSettings(string connectionString, IServiceProvider provider)
    {
        var mongoSettings = MongoClientSettings.FromConnectionString(connectionString);
        mongoSettings.LoggingSettings = new LoggingSettings(provider.GetRequiredService<ILoggerFactory>());
        return mongoSettings;
    }
}
