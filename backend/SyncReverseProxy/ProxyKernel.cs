using LexSyncReverseProxy.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;

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
            .AddJsonFile($"proxy.appsettings.{env.EnvironmentName}.json",
                optional: true,
                reloadOnChange: env.IsDevelopment());
        services.AddHttpContextAccessor();
        services.AddScoped<IAuthorizationHandler, UserHasAccessToProjectRequirementHandler>();
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

    public static ReverseProxyConventionBuilder MapSyncProxy(this IEndpointRouteBuilder app)
    {
        return app.MapReverseProxy()
            .RequireAuthorization(new AuthorizeAttribute { AuthenticationSchemes = BasicAuthHandler.AuthScheme });
    }
}