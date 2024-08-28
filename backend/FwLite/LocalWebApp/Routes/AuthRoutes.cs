using System.Security.AccessControl;
using System.Web;
using LocalWebApp.Auth;

namespace LocalWebApp.Routes;

public static class AuthRoutes
{
    public const string CallbackRoute = "AuthRoutes_Callback";
    public static IEndpointConventionBuilder MapAuthRoutes(this WebApplication app)
    {
        var group = app.MapGroup("/api/auth").WithOpenApi();
        group.MapGet("/login/default", async (AuthHelpersFactory factory) =>
        {
            var result = await factory.GetDefault().SignIn();
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
                var uriBuilder = new UriBuilder(context.Request.Scheme, context.Request.Host.Host, context.Request.Host.Port ?? 80, context.Request.Path);
                uriBuilder.Query = context.Request.QueryString.ToUriComponent();

                await oAuthService.FinishLoginRequest(uriBuilder.Uri);
                return Results.Redirect("/");
            }).WithName(CallbackRoute);
        group.MapGet("/me", async (AuthHelpersFactory factory) => new { name = await factory.GetDefault().GetCurrentName() });
        group.MapGet("/logout/default", async (AuthHelpersFactory factory) =>
        {
            await factory.GetDefault().Logout();
            return Results.Redirect("/");
        });
        return group;
    }
}
