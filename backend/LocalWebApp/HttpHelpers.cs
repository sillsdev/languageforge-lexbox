namespace LocalWebApp;

public static class HttpHelpers
{
    public static string? GetProjectName(this HttpContext? context)
    {
        var name = context?.Request.RouteValues.GetValueOrDefault(LexboxApiHub.ProjectRouteKey, null)?.ToString();
        return string.IsNullOrWhiteSpace(name) ? null : name;
    }
}
