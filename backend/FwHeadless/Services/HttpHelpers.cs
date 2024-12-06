namespace FwHeadless.Services;

public static class HttpHelpers
{
    public static Guid? GetProjectId(this HttpContext? context)
    {
        if (context is null) return null;
        if (context.Request.Query.TryGetValue("projectId", out var projectIds) && projectIds.FirstOrDefault() is string idStr)
        {
            if (Guid.TryParse(idStr, out var projectId)) return projectId;
        }
        return null;
    }
}
