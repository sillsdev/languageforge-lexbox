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

namespace LfClassicData;

public static class DataServiceKernel
{
    public static void AddLanguageForgeClassicMiniLcm(this IServiceCollection services)
    {
        BsonConfiguration.Setup();
        services.AddSingleton(provider =>
        {
            var configuration = provider.GetRequiredService<IConfiguration>();
            return BuildMongoClientSettings("mongodb://localhost:27017", provider);
        });
        services.AddSingleton(provider => new MongoClient(provider.GetRequiredService<MongoClientSettings>()));
        services.AddSingleton<ILexboxApiProvider, LfClassicLexboxApiProvider>();

        services.AddSingleton<SystemDbContext>();
        services.AddSingleton<ProjectDbContext>();
    }

    public static MongoClientSettings BuildMongoClientSettings(string connectionString, IServiceProvider provider)
    {
        var mongoSettings = MongoClientSettings.FromConnectionString(connectionString);
        mongoSettings.LoggingSettings = new LoggingSettings(provider.GetRequiredService<ILoggerFactory>());
        return mongoSettings;
    }

    public static IEndpointConventionBuilder MapLfClassicApi(this IEndpointRouteBuilder builder)
    {
        var group = builder.MapGroup("/api/lfclassic/{projectCode}");
        group.MapGet("/entries",
            (string projectCode, [AsParameters] QueryOptions options, [FromServices] ILexboxApiProvider provider) =>
            {
                var api = provider.GetProjectApi(projectCode);
                return api.GetEntries(options);
            });
        group.MapGet("/entries/{search}",
            (string projectCode,
                string search,
                [AsParameters] QueryOptions options,
                [FromServices] ILexboxApiProvider provider) =>
            {
                var api = provider.GetProjectApi(projectCode);
                return api.SearchEntries(search, options);
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
