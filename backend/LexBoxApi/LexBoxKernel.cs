using LexBoxApi.Auth;
using LexBoxApi.Config;
using LexBoxApi.GraphQL;
using LexBoxApi.Services;
using LexCore.Config;
using LexCore.ServiceInterfaces;
using LexSyncReverseProxy;

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
        // services.AddOptions<HasuraConfig>()
            // .BindConfiguration("HasuraConfig")
            // .ValidateDataAnnotations()
            // .ValidateOnStart();
        services.AddOptions<CloudFlareConfig>()
            .BindConfiguration("CloudFlare")
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddOptions<EmailConfig>()
            .BindConfiguration("Email")
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddOptions<TusConfig>()
            .BindConfiguration("Tus")
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddHttpClient();
        services.AddScoped<LoggedInContext>();
        services.AddScoped<ProjectService>();
        services.AddScoped<UserService>();
        services.AddScoped<EmailService>();
        services.AddScoped<TusService>();
        services.AddScoped<TurnstileService>();
        services.AddScoped<MySqlMigrationService>();
        services.AddScoped<IHgService, HgService>();
        services.AddScoped<ILexProxyService, LexProxyService>();
        services.AddSingleton<LexboxLinkGenerator>();
        services.AddSingleton<RepoMigrationService>();
        services.AddSingleton<IRepoMigrationService>(provider => provider.GetRequiredService<RepoMigrationService>());
        services.AddHostedService(provider => provider.GetRequiredService<RepoMigrationService>());
        services.AddSyncProxy();
        AuthKernel.AddLexBoxAuth(services, configuration, environment);
        services.AddLexGraphQL(environment);
    }
}
