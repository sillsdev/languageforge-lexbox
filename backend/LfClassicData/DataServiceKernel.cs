using LfClassicData.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
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
        services.AddSingleton<ILexboxApiProvider, LfClassicLexboxApiProvider>();

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

    public static IEndpointConventionBuilder MapLfClassicApi(this IEndpointRouteBuilder builder)
    {
        var group = builder.MapGroup("/api/lfclassic/{projectCode}");
        group.MapGet("/writingSystems",
            (string projectCode, [FromServices] ILexboxApiProvider provider) =>
            {
                var api = provider.GetProjectApi(projectCode);
                return api.GetWritingSystems();
            });
        group.MapGet("/entries",
            (string projectCode,
                [FromServices] ILexboxApiProvider provider,
                int count = 1000,
                int offset = 0
                ) =>
            {
                var api = provider.GetProjectApi(projectCode);
                return api.GetEntries(new QueryOptions(SortOptions.Default, count, offset));
            });
        group.MapGet("/entries/{search}",
            (string projectCode,
                [FromServices] ILexboxApiProvider provider,
                string search,
                int count = 1000,
                int offset = 0) =>
            {
                var api = provider.GetProjectApi(projectCode);
                return api.SearchEntries(search, new QueryOptions(SortOptions.Default, count, offset));
            });
        group.MapGet("/entry/{id:Guid}",
            (string projectCode, Guid id, [FromServices] ILexboxApiProvider provider) =>
            {
                var api = provider.GetProjectApi(projectCode);
                return api.GetEntry(id);
            });
        return group;
    }
}
