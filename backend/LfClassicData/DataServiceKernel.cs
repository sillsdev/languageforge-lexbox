using LfClassicData.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MiniLcm;
using MongoDB.Driver;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Extensions.DiagnosticSources;

namespace LfClassicData;

public static class DataServiceKernel
{
    public static void AddLanguageForgeClassicMiniLcm(this IServiceCollection services)
    {
        BsonConfiguration.Setup();
        services.AddOptions<LfClassicConfig>()
            .BindConfiguration(nameof(LfClassicConfig))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddSingleton(BuildMongoClientSettings);
        services.AddSingleton(provider => new MongoClient(provider.GetRequiredService<MongoClientSettings>()));
        services.AddSingleton<LfClassicLexboxApiProvider>();
        services.AddSingleton<SystemDbContext>();
        services.AddSingleton<ProjectDbContext>();
    }

    public static MongoClientSettings BuildMongoClientSettings(IServiceProvider provider)
    {
        var config = provider.GetRequiredService<IOptions<LfClassicConfig>>().Value;
        var mongoSettings = MongoClientSettings.FromConnectionString(config.ConnectionString);
        if (config.HasCredentials)
        {
            mongoSettings.Credential = MongoCredential.CreateCredential(
                databaseName: config.AuthSource,
                username: config.Username,
                password: config.Password
            );
        }
        mongoSettings.LoggingSettings = new LoggingSettings(provider.GetRequiredService<ILoggerFactory>());
        mongoSettings.ConnectTimeout = config.ConnectTimeout;
        mongoSettings.ServerSelectionTimeout = config.ServerSelectionTimeout;
        mongoSettings.ClusterConfigurator = cb =>
            cb.Subscribe(new DiagnosticsActivityEventSubscriber(new() { CaptureCommandText = true }));
        return mongoSettings;
    }
}
