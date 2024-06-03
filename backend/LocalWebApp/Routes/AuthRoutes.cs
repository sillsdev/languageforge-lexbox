using System.Web;
using LocalWebApp.Auth;

namespace LocalWebApp.Routes;

public static class AuthRoutes
{
    public const string CallbackRoute = "AuthRoutes_Callback";
    public static IEndpointConventionBuilder MapAuthRoutes(this WebApplication app)
    {
        var group = app.MapGroup("/api/auth").WithOpenApi();
        group.MapGet("/login",
            async (AuthHelpers helper) =>
            {
                return Results.Redirect(await helper.SignIn());
            });
        group.MapGet("/oauth-callback",
            async (AuthHelpers helper, HttpContext context) =>
            {
                var uriBuilder = new UriBuilder(context.Request.Scheme, context.Request.Host.Host, context.Request.Host.Port ?? 80, context.Request.Path);
                uriBuilder.Query = context.Request.QueryString.ToUriComponent();
                await helper.FinishSignin(uriBuilder.Uri);
                return Results.Redirect("/");
            }).WithName(CallbackRoute);
        group.MapGet("/me", async (AuthHelpers helper) => new { name = await helper.GetCurrentName() });
        group.MapGet("/logout", async (AuthHelpers helper) =>
        {
            await helper.Logout();
            return Results.Redirect("/");
        });
        return group;
    }
}
