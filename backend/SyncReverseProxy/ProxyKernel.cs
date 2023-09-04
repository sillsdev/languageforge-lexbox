using System.Diagnostics;
using System.Text;
using LexCore.Config;
using LexSyncReverseProxy.Auth;
using LexSyncReverseProxy.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.Options;
using Yarp.ReverseProxy.Forwarder;
using Yarp.ReverseProxy.Model;
using Yarp.ReverseProxy.Transforms;

namespace LexSyncReverseProxy;

public static class ProxyKernel
{
    public const string UserHasAccessToProjectPolicy = "UserHasAccessToProject";

    public static void AddSyncProxy(this IServiceCollection services,
        ConfigurationManager configuration,
        IWebHostEnvironment env)
    {
        var defaultJsonSource = configuration.Sources.Last(s => s is JsonConfigurationSource);
        configuration.Sources.Insert(configuration.Sources.IndexOf(defaultJsonSource) + 1,
            new ChainedConfigurationSource
            {
                Configuration = new ConfigurationBuilder().AddJsonFile("proxy.appsettings.json",
                        optional: true,
                        reloadOnChange: env.IsDevelopment())
                    //used when running via LexBoxApi in dev
                    .AddJsonFile(Path.Combine(env.ContentRootPath, "../SyncReverseProxy", "proxy.appsettings.json"),
                        optional: true,
                        reloadOnChange: env.IsDevelopment())
                    .AddJsonFile($"proxy.appsettings.{env.EnvironmentName}.json",
                        optional: true,
                        reloadOnChange: env.IsDevelopment()).Build()
            });

        services.AddHttpContextAccessor();
        services.AddScoped<ProxyEventsService>();
        services.AddMemoryCache();
        services.AddScoped<IAuthorizationHandler, UserHasAccessToProjectRequirementHandler>();
        services.AddTelemetryConsumer<ForwarderTelemetryConsumer>();
        var reverseProxyConfig = configuration.GetSection("ReverseProxy");
        if (!reverseProxyConfig.Exists())
        {
            throw new OptionsValidationException("ReverseProxy",
                typeof(IConfiguration),
                new[] { "ReverseProxy config section is missing" });
        }

        services.AddHttpForwarder();
        services.AddAuthentication()
            .AddScheme<AuthenticationSchemeOptions, BasicAuthHandler>(BasicAuthHandler.AuthScheme, null);
        services.AddAuthorizationBuilder()
            .AddPolicy("UserHasAccessToProject",
                policyBuilder =>
                {
                    policyBuilder.RequireAuthenticatedUser()
                        .AddRequirements(new UserHasAccessToProjectRequirement());
                });
    }

    public static void MapSyncProxy(this IEndpointRouteBuilder app,
        string? extraAuthScheme = null)
    {
        var httpClient = new HttpMessageInvoker(new SocketsHttpHandler
        {
            UseProxy = false,
            UseCookies = false,
            ActivityHeadersPropagator = new ReverseProxyPropagator(DistributedContextPropagator.Current),
            ConnectTimeout = TimeSpan.FromSeconds(15)
        });

        var authorizeAttribute = new AuthorizeAttribute
        {
            AuthenticationSchemes = string.Join(',', BasicAuthHandler.AuthScheme, extraAuthScheme ?? ""),
            Policy = UserHasAccessToProjectPolicy
        };
        //hgresumable
        app.Map("/api/v03/{**catch-all}",
            async (IOptions<HgConfig> hgConfig,
                HttpContext context,
                [FromQuery(Name = "repoId")] string projectCode) =>
            {
                await Forward(context, httpClient, hgConfig.Value.HgResumableUrl, projectCode);
            }).RequireAuthorization(authorizeAttribute).WithMetadata(HgType.resumable);

        //hgweb
        app.Map($"/{{{ProxyConstants.HgProjectCodeRouteKey}}}/{{**catch-all}}",
            async (IOptions<HgConfig> hgConfig,
                HttpContext context,
                [FromRoute(Name = ProxyConstants.HgProjectCodeRouteKey)] string projectCode) =>
            {
                await Forward(context, httpClient, hgConfig.Value.HgWebUrl, projectCode);
            }).RequireAuthorization(authorizeAttribute).WithMetadata(HgType.hgWeb);
        app.Map($"/hg/{{{ProxyConstants.HgProjectCodeRouteKey}}}/{{**catch-all}}",
            async (IOptions<HgConfig> hgConfig,
                HttpContext context,
                [FromRoute(Name = ProxyConstants.HgProjectCodeRouteKey)] string projectCode) =>
            {
                var hgWebUrl = hgConfig.Value.HgWebUrl;
                if (hgWebUrl.EndsWith("hg/"))
                {
                    // the mapped path already starts with `hg/`
                    hgWebUrl = hgWebUrl[..^"hg/".Length];
                }

                await Forward(context, httpClient, hgWebUrl, projectCode);
            }).RequireAuthorization(authorizeAttribute).WithMetadata(HgType.hgWeb);
    }

    private enum HgType
    {
        hgWeb,
        resumable
    }

    private static async Task Forward(HttpContext context,
        HttpMessageInvoker httpClient,
        string destinationPrefix,
        string projectCode)
    {
        var forwarder = context.RequestServices.GetRequiredService<IHttpForwarder>();
        var eventsService = context.RequestServices.GetRequiredService<ProxyEventsService>();
        Activity.Current?.AddTag("app.project_code", projectCode);
        await forwarder.SendAsync(context, destinationPrefix, httpClient);
        var hgType = context.GetEndpoint()?.Metadata.OfType<HgType>().FirstOrDefault();
        if (hgType == HgType.hgWeb)
        {
            await eventsService.HandleHgRequest(context);
        }
    }

    /// <summary>
    /// this is required because if resumable receives a 403 Forbidden,
    /// it will retry forever, we must return a 401 Unauthorized instead.
    /// Must be called after routing but before Auth so it can intercept and change the 403
    /// </summary>
    public static void UseResumableStatusHack(this IApplicationBuilder app)
    {
        app.Use(async (context, next) =>
        {
            await next(context);
            if (context.Response.StatusCode != 403)
            {
                return;
            }

            var hgType = context.GetEndpoint()?.Metadata.OfType<HgType>().FirstOrDefault();
            if (hgType == HgType.resumable)
                context.Response.StatusCode = 401;
        });
    }
}
