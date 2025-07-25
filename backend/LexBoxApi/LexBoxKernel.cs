using LexBoxApi.Auth;
using LexBoxApi.Config;
using LexBoxApi.GraphQL;
using LexBoxApi.GraphQL.CustomTypes;
using LexBoxApi.Proxies;
using LexBoxApi.Services;
using LexBoxApi.Services.Email;
using LexBoxApi.Services.FwLiteReleases;
using LexCore.Config;
using LexCore.ServiceInterfaces;
using LexSyncReverseProxy;
using LfClassicData;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Polly;
using Swashbuckle.AspNetCore.Swagger;

namespace LexBoxApi;

public static class LexBoxKernel
{
    public const string SwaggerDocumentName = "v1";
    public const string OpenApiPublicDocumentName = "public";

    public static void AddLexBoxApi(this IServiceCollection services,
        ConfigurationManager configuration,
        IWebHostEnvironment environment)
    {
        services.AddOptions<HgConfig>()
            .BindConfiguration("HgConfig")
            .ValidateDataAnnotations()
            .ValidateOnStart();
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
        services.AddOptions<MediaFileConfig>()
            .BindConfiguration("MediaFileConfig")
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddOptions<HealthChecksConfig>()
            .BindConfiguration("HealthChecks")
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddOptions<FwLiteReleaseConfig>()
            .BindConfiguration("FwLiteRelease")
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddHttpClient();
        services.AddServiceDiscovery();
        services.AddHttpClient<FwHeadlessClient>(client => client.BaseAddress = new ("http://fwHeadless"))
            .AddServiceDiscovery();//service discovery means that we lookup the hostname in Services__fwHeadless__http in config
        services.AddHttpContextAccessor();
        services.AddMemoryCache();
        services.AddScoped<LoggedInContext>();
        services.AddScoped<IPermissionService, PermissionService>();
        services.AddScoped<ProjectService>();
        services.AddScoped<CrdtCommitService>();
        services.AddScoped<UserService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<TusService>();
        services.AddScoped<TurnstileService>();
        services.AddScoped<IHgService, HgService>();
        services.AddSingleton<FwLiteReleaseService>();
        services.AddHostedService<HgService>();
        services.AddTransient<HgWebHealthCheck>();
        services.AddTransient<FwHeadlessHealthCheck>();
        services.AddScoped<ILexProxyService, LexProxyService>();
        services.AddSingleton<ISendReceiveService, SendReceiveService>();
        services.AddSingleton<LexboxLinkGenerator>();
        if (environment.IsDevelopment())
            services.AddHostedService<SwaggerValidationService>();
        services.AddScheduledTasks(configuration);
        services.AddHealthChecks()
            .AddCheck<HgWebHealthCheck>("hgweb", HealthStatus.Unhealthy, ["hg"], TimeSpan.FromSeconds(5))
            //todo enable this once we want to make lexbox depend on fw-headless
            // .AddCheck<FwHeadlessHealthCheck>("fw-headless", HealthStatus.Unhealthy, ["fw-headless"], TimeSpan.FromSeconds(5))
            ;
        services.AddSyncProxy();
        services.AddFileUploadProxy();
        AuthKernel.AddLexBoxAuth(services, configuration, environment);
        services.AddLexGraphQL(environment);
    }

    private class SwaggerValidationService(IAsyncSwaggerProvider swaggerProvider): BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            //this delay is because there's some kind of race condition where minimal apis are not yet registered
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            await swaggerProvider.GetSwaggerAsync(SwaggerDocumentName);
        }
    }
}
