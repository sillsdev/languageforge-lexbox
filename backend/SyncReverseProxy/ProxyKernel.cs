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
            .LoadFromConfig(configuration.GetSection("ReverseProxy"));
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