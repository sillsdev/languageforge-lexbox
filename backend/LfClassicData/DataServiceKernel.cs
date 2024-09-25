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
        var config = provider.GetRequiredService<IOptions<LfClassicConfig>>();
        var mongoSettings = MongoClientSettings.FromConnectionString(config.Value.ConnectionString);
        if (config.Value.HasCredentials)
        {
            mongoSettings.Credential = MongoCredential.CreateCredential(
                databaseName: config.Value.AuthSource,
                username: config.Value.Username,
                password: config.Value.Password
            );
        }
        mongoSettings.LoggingSettings = new LoggingSettings(provider.GetRequiredService<ILoggerFactory>());
        mongoSettings.ClusterConfigurator = cb =>
            cb.Subscribe(new DiagnosticsActivityEventSubscriber(new() { CaptureCommandText = true }));
        return mongoSettings;
    }
}
