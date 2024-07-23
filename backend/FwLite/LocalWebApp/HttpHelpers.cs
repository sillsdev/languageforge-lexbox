using LocalWebApp.Hubs;

namespace LocalWebApp;

public static class HttpHelpers
{
    public static string? GetProjectName(this HttpContext? context)
    {
        var name = context?.Request.RouteValues.GetValueOrDefault(CrdtMiniLcmApiHub.ProjectRouteKey, null)?.ToString();
        return string.IsNullOrWhiteSpace(name) ? null : name;
    }

    public static string? GetFwDataName(this HttpContext? context)
    {
        var name = context?.Request.RouteValues.GetValueOrDefault(FwDataMiniLcmHub.ProjectRouteKey, null)?.ToString();
        return string.IsNullOrWhiteSpace(name) ? null : name;
    }
}
