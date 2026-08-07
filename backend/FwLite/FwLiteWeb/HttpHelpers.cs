namespace FwLiteWeb;

public static class HttpHelpers
{
    public static string? GetProjectCode(this HttpContext? context)
    {
        var name = context?.Request.RouteValues.GetValueOrDefault(RouteKeys.Project, null)?.ToString();
        return string.IsNullOrWhiteSpace(name) ? null : name;
    }

    public static string? GetFwDataName(this HttpContext? context)
    {
        var name = context?.Request.RouteValues.GetValueOrDefault(RouteKeys.FwData, null)?.ToString();
        return string.IsNullOrWhiteSpace(name) ? null : name;
    }
}
