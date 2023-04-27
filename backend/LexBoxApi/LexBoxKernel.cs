using LexBoxApi.Auth;
using LexBoxApi.Config;
using LexBoxApi.GraphQL;
using LexBoxApi.Services;
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
        services.AddOptions<HasuraConfig>()
            .BindConfiguration("HasuraConfig")
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddOptions<CloudFlareConfig>()
            .BindConfiguration("CloudFlare")
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddOptions<EmailConfig>()
            .BindConfiguration("Email")
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddScoped<LoggedInContext>();
        services.AddScoped<ProjectService>();
        services.AddScoped<EmailService>();
        services.AddScoped<TurnstileService>();
        services.AddScoped<IHgService, HgService>();
        services.AddScoped<ILexProxyService, LexProxyService>();
        services.AddSyncProxy(configuration, environment);
        AuthKernel.AddLexBoxAuth(services, configuration, environment);
        services.AddLexGraphQL(environment);
    }
}