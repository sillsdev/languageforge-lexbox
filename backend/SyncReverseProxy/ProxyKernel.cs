using System.Text;
using LexSyncReverseProxy.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Yarp.ReverseProxy.Transforms;

namespace LexSyncReverseProxy;

public static class ProxyKernel
{
    public static void AddSyncProxy(this IServiceCollection services,
        ConfigurationManager configuration,
        IWebHostEnvironment env)
    {
        configuration.AddJsonFile("proxy.appsettings.json",
                optional: true,
                reloadOnChange: env.IsDevelopment())
            //used when running via LexBoxApi in dev
            .AddJsonFile(Path.Combine(env.ContentRootPath, "../SyncReverseProxy", "proxy.appsettings.json"),
                optional: true,
                reloadOnChange: env.IsDevelopment())
            .AddJsonFile($"proxy.appsettings.{env.EnvironmentName}.json",
                optional: true,
                reloadOnChange: env.IsDevelopment());
        services.AddHttpContextAccessor();
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
                if (context.Route.RouteId == "hg-web-view")
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
        return app.MapReverseProxy()
            .RequireAuthorization(new AuthorizeAttribute
            {
                AuthenticationSchemes = string.Join(',', BasicAuthHandler.AuthScheme, extraAuthScheme ?? "")
            });
    }
}