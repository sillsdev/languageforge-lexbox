using LexSyncReverseProxy.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;

namespace LexSyncReverseProxy;

public static class ProxyKernel
{
    public static void AddSyncProxy(this IServiceCollection services, IConfigurationRoot configuration)
    {
        services.AddReverseProxy()
            .LoadFromConfig(configuration.GetSection("ReverseProxy"));
        services.AddAuthentication()
            .AddScheme<AuthenticationSchemeOptions, BasicAuthHandler>(BasicAuthHandler.AuthScheme, null);
        services.AddAuthorizationBuilder()
            .AddPolicy("HgAuthPolicy",
                policyBuilder =>
                {
                    policyBuilder.RequireAuthenticatedUser();
                });
    }

    public static ReverseProxyConventionBuilder MapSyncProxy(this IEndpointRouteBuilder app)
    {
        return app.MapReverseProxy()
            .RequireAuthorization(new AuthorizeAttribute { AuthenticationSchemes = BasicAuthHandler.AuthScheme });
    }
}