using FwHeadless.Services;
using LcmCrdt;
using Microsoft.Extensions.Options;
using MiniLcmWeb;

namespace FwHeadless.Routes;

/// <summary>
/// The lexicon API's project routes, served from the headless harmony project. Only Lexbox calls these (it forwards
/// /api/lexicon), after checking the caller's scope and project role and setting the identity headers.
/// </summary>
public static class LexiconRoutes
{
    public const string UserIdHeader = "X-Lexbox-User-Id";
    /// <summary>Percent-encoded UTF-8, since display names aren't limited to ASCII.</summary>
    public const string UserNameHeader = "X-Lexbox-User-Name";
    public const string ClientIdHeader = "X-Lexbox-Client-Id";
    public const string OAuthClientIdMetadataKey = "OAuthClientId";
    public const string ProjectNotReadyProblemType = "/problems/project-not-ready";
    public const string SyncInProgressProblemType = "/problems/sync-in-progress";

    public static IEndpointConventionBuilder MapLexiconRoutes(this WebApplication app)
    {
        var group = app.MapGroup("/api/lexicon");
        group.MapPost("/projects/ready", ReadyProjects);
        group.MapGroup("/project/{projectId:guid}")
            .AddEndpointFilter(StampWrites)
            .MapMiniLcmRoutes("", ResolveProject);
        return group;
    }

    /// <summary>
    /// Which of the given projects have a headless harmony project, so Lexbox can list only ready projects.
    /// </summary>
    private static async Task<Guid[]> ReadyProjects(Guid[] projectIds,
        IProjectLookupService projectLookupService,
        IOptions<FwHeadlessConfig> config)
    {
        var codes = await projectLookupService.GetProjectCodes(projectIds);
        return codes.Where(p => File.Exists(config.Value.GetCrdtFile(p.Value, p.Key))).Select(p => p.Key).ToArray();
    }

    private static bool IsWrite(HttpContext context) =>
        !HttpMethods.IsGet(context.Request.Method) && !HttpMethods.IsHead(context.Request.Method);

    private static async ValueTask<MiniLcmApiResolution> ResolveProject(HttpContext context)
    {
        var projectId = Guid.Parse(context.Request.RouteValues["projectId"]!.ToString()!);
        var services = context.RequestServices;
        var project = await services.GetRequiredService<ProjectContextFromIdService>().FindCrdtProject(projectId);
        if (project is null)
        {
            if (!await services.GetRequiredService<IProjectLookupService>().ProjectExists(projectId))
            {
                return MiniLcmApiResolution.Failed(Results.Problem("Project not found",
                    statusCode: StatusCodes.Status404NotFound));
            }
            return MiniLcmApiResolution.Failed(Results.Problem(
                "This project has not been synced to FieldWorks Lite on Lexbox yet. Trigger a sync from the project page, then retry.",
                statusCode: StatusCodes.Status409Conflict,
                title: "Project not ready",
                type: ProjectNotReadyProblemType));
        }

        if (IsWrite(context) && services.GetRequiredService<SyncHostedService>().IsJobQueuedOrRunning(projectId))
        {
            return MiniLcmApiResolution.Failed(Results.Problem(
                "The project is being synced with FieldWorks, retry once the sync has finished.",
                statusCode: StatusCodes.Status409Conflict,
                title: "Sync in progress",
                type: SyncInProgressProblemType));
        }

        return MiniLcmApiResolution.Resolved(await services.OpenCrdtProject(project));
    }

    /// <summary>
    /// Stamps the commits a write makes with the Lexbox user and the OAuth client from the proxy's identity headers,
    /// then schedules a harmony sync to push them to Lexbox.
    /// </summary>
    private static async ValueTask<object?> StampWrites(EndpointFilterInvocationContext invocationContext, EndpointFilterDelegate next)
    {
        var context = invocationContext.HttpContext;
        if (!IsWrite(context)) return await next(invocationContext);

        var userId = context.Request.Headers[UserIdHeader].ToString();
        var userName = Uri.UnescapeDataString(context.Request.Headers[UserNameHeader].ToString());
        var clientId = context.Request.Headers[ClientIdHeader].ToString();
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(clientId))
        {
            return Results.Problem($"Writes need the {UserIdHeader}, {UserNameHeader} and {ClientIdHeader} headers",
                statusCode: StatusCodes.Status401Unauthorized);
        }

        var interceptor = context.RequestServices.GetRequiredService<CommitMetadataInterceptor>();
        object? result;
        using (interceptor.Intercept(metadata =>
               {
                   metadata.AuthorId = userId;
                   metadata.AuthorName = userName;
                   metadata.ExtraMetadata[OAuthClientIdMetadataKey] = clientId;
               }))
        {
            result = await next(invocationContext);
        }

        // nothing to push when the write was refused (unknown project, not ready, merge running, bad request)
        if (result is IStatusCodeHttpResult { StatusCode: >= 400 }) return result;
        var projectId = Guid.Parse(context.Request.RouteValues["projectId"]!.ToString()!);
        context.RequestServices.GetRequiredService<HarmonySyncScheduler>().RequestSync(projectId);
        return result;
    }
}
