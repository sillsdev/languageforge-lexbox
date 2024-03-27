using LexBoxApi.Auth;
using LexBoxApi.Config;
using LexBoxApi.GraphQL;
using LexBoxApi.Services;
using LexCore.Config;
using LexCore.ServiceInterfaces;
using LexSyncReverseProxy;
using Swashbuckle.AspNetCore.Swagger;

namespace LexBoxApi;

public static class LexBoxKernel
{
    public const string SwaggerDocumentName = "v1";

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
        services.AddOptions<GoogleOptions>()
            .BindConfiguration("Authentication:Google")
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
        services.AddScoped<IPermissionService, PermissionService>();
        services.AddScoped<ProjectService>();
        services.AddScoped<UserService>();
        services.AddScoped<EmailService>();
        services.AddScoped<TusService>();
        services.AddScoped<TurnstileService>();
        services.AddScoped<IHgService, HgService>();
        services.AddScoped<ILexProxyService, LexProxyService>();
        services.AddSingleton<ISendReceiveService, SendReceiveService>();
        services.AddSingleton<LexboxLinkGenerator>();
        if (environment.IsDevelopment())
            services.AddHostedService<SwaggerValidationService>();
        services.AddScheduledTasks(configuration);
        services.AddSyncProxy();
        AuthKernel.AddLexBoxAuth(services, configuration, environment);
        services.AddLexGraphQL(environment);
    }

    private class SwaggerValidationService(IAsyncSwaggerProvider swaggerProvider): IHostedService
    {
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await swaggerProvider.GetSwaggerAsync(SwaggerDocumentName);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
