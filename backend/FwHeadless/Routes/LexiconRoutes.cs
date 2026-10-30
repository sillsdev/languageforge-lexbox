using FwHeadless.Services;
using LcmCrdt;
using LexCore.Sync;
using Microsoft.Extensions.Options;
using MiniLcm.Wrappers;
using MiniLcmWeb;

namespace FwHeadless.Routes;

/// <summary>
/// The lexicon API's project routes, served from the headless harmony project. Only Lexbox calls these (it forwards
/// /api/lexicon), after checking the caller's scope and project role and setting the identity headers.
/// </summary>
public static class LexiconRoutes
{
    public const string OAuthClientIdMetadataKey = "OAuthClientId";

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
        return codes.Where(p => config.Value.IsHeadlessHarmonyProjectReady(p.Value, p.Key)).Select(p => p.Key).ToArray();
    }

    private static bool IsWrite(HttpContext context) =>
        !HttpMethods.IsGet(context.Request.Method) && !HttpMethods.IsHead(context.Request.Method);

    private static async ValueTask<MiniLcmApiResolution> ResolveProject(HttpContext context)
    {
        var projectId = Guid.Parse(context.Request.RouteValues["projectId"]!.ToString()!);
        var services = context.RequestServices;
        var project = await services.GetRequiredService<ProjectContextFromIdService>().FindReadyCrdtProject(projectId);
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
                type: LexiconApi.ProjectNotReadyProblemType));
        }

        if (IsWrite(context) && services.GetRequiredService<SyncHostedService>().IsJobQueuedOrRunning(projectId))
        {
            return MiniLcmApiResolution.Failed(Results.Problem(
                "The project is being synced with FieldWorks, retry once the sync has finished.",
                statusCode: StatusCodes.Status409Conflict,
                title: "Sync in progress",
                type: LexiconApi.SyncInProgressProblemType));
        }

        var userFacingWrappers = services.GetRequiredService<MiniLcmApiUserFacingWrappers>();
        return MiniLcmApiResolution.Resolved(userFacingWrappers.Apply(await services.OpenCrdtProject(project), project));
    }

    /// <summary>
    /// Stamps the commits a write makes with the Lexbox user and the OAuth client from the proxy's identity headers,
    /// then schedules a harmony sync to push them to Lexbox.
    /// </summary>
    private static async ValueTask<object?> StampWrites(EndpointFilterInvocationContext invocationContext, EndpointFilterDelegate next)
    {
        var context = invocationContext.HttpContext;
        if (!IsWrite(context)) return await next(invocationContext);

        var userId = context.Request.Headers[LexiconApi.UserIdHeader].ToString();
        var userName = Uri.UnescapeDataString(context.Request.Headers[LexiconApi.UserNameHeader].ToString());
        var clientId = context.Request.Headers[LexiconApi.ClientIdHeader].ToString();
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(clientId))
        {
            return Results.Problem($"Writes need the {LexiconApi.UserIdHeader}, {LexiconApi.UserNameHeader} and {LexiconApi.ClientIdHeader} headers",
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
