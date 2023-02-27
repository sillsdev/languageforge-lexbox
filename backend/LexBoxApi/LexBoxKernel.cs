using LexBoxApi.Auth;
using LexBoxApi.Config;
using LexBoxApi.Services;
using LexCore.ServiceInterfaces;
using LexSyncReverseProxy;
using Microsoft.Extensions.Options;

namespace LexBoxApi;

public static class LexBoxKernel
{
    public static void AddLexBoxApi(this IServiceCollection services,
        ConfigurationManager configuration,
        IWebHostEnvironment environment)
    {
        services.AddOptions<HgConfig>()
            .BindConfiguration("HgConfig")
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddOptions<HasuraConfig>()
            .BindConfiguration("HasuraConfig")
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddScoped<LoggedInContext>();
        services.AddScoped<ProjectService>();
        services.AddScoped<HgService>();
        services.AddScoped<IProxyAuthService, ProxyAuthService>();
        services.AddSyncProxy(configuration, environment);
        AuthKernel.AddLexBoxAuth(services, configuration, environment);

        services.AddHttpClient("hasura",
            (provider, client) =>
            {
                var hasuraConfig = provider.GetRequiredService<IOptions<HasuraConfig>>().Value;
                client.BaseAddress = new Uri(hasuraConfig.HasuraUrl);
                client.DefaultRequestHeaders.Add("x-hasura-admin-secret", hasuraConfig.HasuraSecret);
            });
        services.AddGraphQLServer()
            .InitializeOnStartup()
            .AddType(new DateTimeType("timestamptz"))
            .AddType(new UuidType("uuid"))
            .AddRemoteSchema("hasura")
            .AddGraphQL("hasura")
            .AddType(new DateTimeType("timestamptz"))
            .AddType(new UuidType("uuid"))
            ;
    }
}