using LocalWebApp.Auth;

namespace LocalWebApp.Routes;

public static class AuthRoutes
{
    public static IEndpointConventionBuilder MapAuthRoutes(this WebApplication app)
    {
        var group = app.MapGroup("/api/auth").WithOpenApi();
        group.MapGet("/login",
            async () =>
            {
                await AuthHelpers.Instance.SignIn();
                return Results.Redirect("/");
            });
        group.MapGet("/me", async () => new { name = await AuthHelpers.Instance.GetCurrentName() });
        return group;
    }
}
