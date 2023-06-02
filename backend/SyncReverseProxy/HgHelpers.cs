namespace LexSyncReverseProxy;

public static class HgHelpers
{
    public static string? GetProjectCode(this HttpRequest request)
    {
        string? projectCode = null;
        //this is used for hg requests. the key we're using is defined in app settings hg.path.match
        if (request.RouteValues.TryGetValue("project-code", out var projectCodeObj))
        {
            projectCode = projectCodeObj?.ToString() ?? null;
        }
        //this is for resumable requests.
        else if (request.Query.TryGetValue("repoId", out var projectCodeValues))
        {
            projectCode = projectCodeValues.ToString();
        }

        return projectCode;
    }
}