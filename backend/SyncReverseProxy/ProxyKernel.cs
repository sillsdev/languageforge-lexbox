using System.Diagnostics;
using System.Text;
using LexSyncReverseProxy.Auth;
using LexSyncReverseProxy.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.Options;
using Yarp.ReverseProxy.Model;
using Yarp.ReverseProxy.Transforms;

namespace LexSyncReverseProxy;

public static class ProxyKernel
{
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
        var reverseProxyConfig = configuration.GetSection("ReverseProxy");
        if (!reverseProxyConfig.Exists())
        {
            throw new OptionsValidationException("ReverseProxy",
                typeof(IConfiguration),
                new[] { "ReverseProxy config section is missing" });
        }

        services.AddReverseProxy()
            .LoadFromConfig(reverseProxyConfig)
            .AddTransforms(context =>
            {
                context.AddRequestTransform(async transformContext =>
                {
                    var projectCode = transformContext.HttpContext.GetRouteValue("project-code")?.ToString();
                    if (projectCode is not null)
                    {
                        Activity.Current?.AddTag("app.project_code", projectCode);
                    }
                });

                if (context.Route.RouteId == "hgWebView")
                {
                    context.AddResponseTransform(async transformContext =>
                    {
                        if (transformContext.ProxyResponse is null) return;
                        string? projectCode = null;
                        //this is used for hg requests. the key we're using is defined in app settings hg.path.match
                        if (transformContext.HttpContext.Request.RouteValues.TryGetValue("project-code",
                                out var projectCodeObj))
                        {
                            projectCode = projectCodeObj?.ToString() ?? null;
                        }
                        if (projectCode is null) return;
                        var responseString = await transformContext.ProxyResponse.Content.ReadAsStringAsync();
                        if (string.IsNullOrEmpty(responseString)) return;
                        var newResponse = responseString.Replace($"""href="/{projectCode}""", $"""href="/api/hg-view/{projectCode}""");
                        transformContext.SuppressResponseBody = true;
                        var bytes = Encoding.UTF8.GetBytes(newResponse);
                        transformContext.HttpContext.Response.ContentLength = bytes.Length;
                        await transformContext.HttpContext.Response.Body.WriteAsync(bytes);
                    });
                }
            });
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

    public static ReverseProxyConventionBuilder MapSyncProxy(this IEndpointRouteBuilder app,
        string? extraAuthScheme = null)
    {
        return app.MapReverseProxy(builder => builder.Use(async (context, next) =>
            {
                var eventsService = context.RequestServices.GetRequiredService<ProxyEventsService>();
                var proxyFeature = context.Features.Get<IReverseProxyFeature>();
                await next(context);
                ArgumentNullException.ThrowIfNull(proxyFeature);
                await eventsService.AfterRequest(context, proxyFeature);
            }))
            .RequireAuthorization(new AuthorizeAttribute
            {
                AuthenticationSchemes = string.Join(',', BasicAuthHandler.AuthScheme, extraAuthScheme ?? "")
            });
    }
}