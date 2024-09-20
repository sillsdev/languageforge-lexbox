using System.Security.AccessControl;
using System.Web;
using LocalWebApp.Auth;
using Microsoft.Extensions.Options;

namespace LocalWebApp.Routes;

public static class AuthRoutes
{
    public const string CallbackRoute = "AuthRoutes_Callback";
    public record ServerStatus(string DisplayName, bool LoggedIn, string? LoggedInAs, string? Authority);
    public static IEndpointConventionBuilder MapAuthRoutes(this WebApplication app)
    {
        var group = app.MapGroup("/api/auth").WithOpenApi();
        group.MapGet("/servers", (IOptions<AuthConfig> options, AuthHelpersFactory factory) =>
        {
            return options.Value.LexboxServers.ToAsyncEnumerable().SelectAwait(async s =>
            {
                var currentName = await factory.GetHelper(s).GetCurrentName();
                return new ServerStatus(s.DisplayName,
                    !string.IsNullOrEmpty(currentName),
                    currentName, s.Authority.Authority);
            });
        });
        group.MapGet("/login/{server}",
            async (AuthHelpersFactory factory, string server, IOptions<AuthConfig> options) =>
            {
                var result = await factory.GetHelper(options.Value.GetServerByAuthority(server)).SignIn();
                if (result.HandledBySystemWebView)
                {
                    return Results.Redirect("/");
                }

                if (result.AuthUri is null) throw new InvalidOperationException("AuthUri is null");
                return Results.Redirect(result.AuthUri.ToString());
            });
        group.MapGet("/oauth-callback",
            async (OAuthService oAuthService, HttpContext context) =>
            {
                var uriBuilder = new UriBuilder(context.Request.Scheme,
                    context.Request.Host.Host,
                    context.Request.Host.Port ?? 80,
                    context.Request.Path);
                uriBuilder.Query = context.Request.QueryString.ToUriComponent();

                await oAuthService.FinishLoginRequest(uriBuilder.Uri);
                return Results.Redirect("/");
            }).WithName(CallbackRoute);
        group.MapGet("/me/{server}",
            async (AuthHelpersFactory factory, string server, IOptions<AuthConfig> options) =>
            {
                return new { name = await factory.GetHelper(options.Value.GetServerByAuthority(server)).GetCurrentName() };
            });
        group.MapGet("/logout/{server}",
            async (AuthHelpersFactory factory, string server, IOptions<AuthConfig> options) =>
            {
                await factory.GetHelper(options.Value.GetServerByAuthority(server)).Logout();
                return Results.Redirect("/");
            });
        return group;
    }
}
