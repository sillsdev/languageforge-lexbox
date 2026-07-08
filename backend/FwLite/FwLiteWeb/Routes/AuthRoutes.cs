using System.Security.AccessControl;
using System.Web;
using FwLiteShared.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace FwLiteWeb.Routes;

public static class AuthRoutes
{
    public const string CallbackRoute = "AuthRoutes_Callback";
    public record ServerStatus(string DisplayName, bool LoggedIn, string? LoggedInAs, string? Authority);
    public static IEndpointConventionBuilder MapAuthRoutes(this WebApplication app)
    {
        var group = app.MapGroup("/api/auth");
        group.MapGet("/servers", (AuthService authService) => authService.Servers());
        group.MapGet("/login/{authority}",
            async (AuthService authService, string authority, IOptions<AuthConfig> options, [FromHeader] string referer) =>
            {
                var returnUrl = new Uri(referer).PathAndQuery;
                if (options.Value.SystemWebViewLogin)
                {
                    throw new NotSupportedException("System web view login is not supported for this endpoint");
                }

                return Results.Redirect(await authService.SignInWebApp(options.Value.GetServerByAuthority(authority), returnUrl));
            });
        //separate from /login/{authority} because that endpoint redirects the caller's browser to an auth
        //url and requires a Referer header, neither of which applies here: this opens the system's default
        //browser itself and blocks until the user finishes (or MSAL gives up), then reports the outcome.
        group.MapGet("/login-system/{authority}",
            async (AuthService authService, string authority, IOptions<AuthConfig> options) =>
            {
                if (!options.Value.SystemWebViewLogin)
                {
                    throw new NotSupportedException("System web view login is not enabled for this server");
                }

                return await authService.SignInWebView(options.Value.GetServerByAuthority(authority));
            });
        group.MapGet("/oauth-callback",
            async (OAuthService oAuthService, HttpContext context) =>
            {
                var uriBuilder = new UriBuilder(context.Request.Scheme,
                    context.Request.Host.Host,
                    context.Request.Host.Port ?? 80,
                    context.Request.Path);
                uriBuilder.Query = context.Request.QueryString.ToUriComponent();

                var (_, returnUrl) = await oAuthService.FinishLoginRequest(uriBuilder.Uri);
                return Results.Redirect(returnUrl);
            }).WithName(CallbackRoute);
        group.MapGet("/me/{authority}",
            async (AuthService authService, string authority, IOptions<AuthConfig> options) =>
            {
                return new { name = await authService.GetLoggedInName(options.Value.GetServerByAuthority(authority)) };
            });
        group.MapGet("/logout/{authority}",
            async (AuthService authService, string authority, IOptions<AuthConfig> options) =>
            {
                await authService.Logout(options.Value.GetServerByAuthority(authority));
                return Results.Redirect("/");
            });
        return group;
    }
}
