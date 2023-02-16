using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using WebApi.Auth;

namespace WebApi;

public static class ProxyKernel
{
    public static void AddSyncProxy(this IServiceCollection services, IConfigurationRoot configuration)
    {
        services.AddReverseProxy()
            .LoadFromConfig(configuration.GetSection("ReverseProxy"));
        services.AddHttpClient();
        services.AddAuthentication()
            .AddScheme<AuthenticationSchemeOptions, BasicAuthHandler>("HgAuthScheme", null);
        services.AddAuthorizationBuilder()
            .AddPolicy("HgAuth",
                policyBuilder =>
                {
                    policyBuilder.RequireAuthenticatedUser();
                });
    }

    public static void MapSyncProxy(this IEndpointRouteBuilder app)
    {
        app.MapReverseProxy().RequireAuthorization(new AuthorizeAttribute { AuthenticationSchemes = "HgAuthScheme" });
    }
}