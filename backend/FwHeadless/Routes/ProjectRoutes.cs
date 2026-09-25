using System.Diagnostics;
using FwHeadless.Services;
using LexCore.Entities;
using LexCore.Sync;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;
using SIL.WritingSystems;

namespace FwHeadless.Routes;

public static class ProjectRoutes
{
    public static IEndpointConventionBuilder MapProjectRoutes(this WebApplication app)
    {
        var group = app.MapGroup("/api/project");
        group.MapPost("/initFwDataProject", InitFwDataProject);
        group.MapGet("/creation-status", GetCreationStatus);
        group.MapGet("/await-creation-finished", AwaitCreationFinished);
        return group;
    }

    // Internal endpoint: LexBoxApi has already created the project row + empty repo and performed the
    // admin-auth check. FwHeadless populates the empty repo with a template .fwdata configured for the
    // requested writing systems. Not idempotent — on failure LexBoxApi deletes and recreates.
    private static async Task<Results<Ok, ProblemHttpResult>> InitFwDataProject(
        Guid projectId,
        InitFwDataProjectInput input,
        IProjectLookupService projectLookupService,
        ProjectCreationService projectCreationService,
        ILogger<Program> logger)
    {
        // Null-safe against a malformed body (System.Text.Json leaves an absent list null).
        if (input.WsVernacular is not { Count: > 0 })
            return TypedResults.Problem("At least one vernacular writing system is required",
                statusCode: StatusCodes.Status400BadRequest);
        if (input.WsAnalysis is not { Count: > 0 })
            return TypedResults.Problem("At least one analysis writing system is required",
                statusCode: StatusCodes.Status400BadRequest);
        var invalidWs = input.WsVernacular.Concat(input.WsAnalysis).FirstOrDefault(ws => !IetfLanguageTag.IsValid(ws));
        if (invalidWs is not null)
            return TypedResults.Problem($"Invalid writing system code: {invalidWs}",
                statusCode: StatusCodes.Status400BadRequest);
        if (string.IsNullOrEmpty(input.WsUi) || !IetfLanguageTag.IsValid(input.WsUi))
            return TypedResults.Problem($"Invalid UI writing system code: {input.WsUi}",
                statusCode: StatusCodes.Status400BadRequest);

        var projectCode = await projectLookupService.GetProjectCode(projectId);
        if (projectCode is null)
        {
            logger.LogError("initFwDataProject request for non-existent project {ProjectId}", projectId);
            return TypedResults.Problem("Project not found", statusCode: StatusCodes.Status404NotFound);
        }

        await projectCreationService.InitFwDataProject(
            projectId, projectCode, input.WsVernacular, input.WsAnalysis, input.WsUi);
        return TypedResults.Ok();
    }

    // Instantaneous creation status, for a caller that wants to poll. Mirrors /api/merge/status.
    private static async Task<Results<Ok<ProjectCreationStatus>, NotFound>> GetCreationStatus(
        Guid projectId,
        SyncHostedService syncHostedService,
        IProjectLookupService projectLookupService,
        IOptions<FwHeadlessConfig> config)
    {
        using var activity = FwHeadlessActivitySource.Value.StartActivity();
        activity?.SetTag("app.project_id", projectId);

        var status = await DetermineCreationStatus(projectId, syncHostedService, projectLookupService, config);
        if (status is null)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Project not found");
            return TypedResults.NotFound();
        }
        activity?.SetStatus(ActivityStatusCode.Ok, status.Status.ToString());
        return TypedResults.Ok(status);
    }

    // Long-poll until an in-flight creation finishes. Mirrors /api/merge/await-finished: giving up on the
    // wait is reported as a status, not an error, because the creation itself keeps running either way.
    private static async Task<Results<Ok<ProjectCreationStatus>, NotFound>> AwaitCreationFinished(
        Guid projectId,
        SyncHostedService syncHostedService,
        IProjectLookupService projectLookupService,
        IOptions<FwHeadlessConfig> config,
        CancellationToken cancellationToken)
    {
        using var activity = FwHeadlessActivitySource.Value.StartActivity();
        activity?.SetTag("app.project_id", projectId);

        try
        {
            var result = await syncHostedService.AwaitCreationFinished(projectId, cancellationToken);
            if (result is not null)
            {
                activity?.SetStatus(ActivityStatusCode.Ok, result.Status.ToString());
                return TypedResults.Ok(result);
            }
        }
        catch (OperationCanceledException)
        {
            // The caller's wait was cancelled; the creation was not (it takes no cancellation token).
            // Report that distinctly so a caller can't read a timeout as a failed creation.
            activity?.SetStatus(ActivityStatusCode.Unset, "Timed out awaiting creation");
            return TypedResults.Ok(ProjectCreationStatus.TimedOutAwaitingCreation);
        }

        // Nothing in flight and nothing remembered, so fall back to what the project itself tells us.
        var status = await DetermineCreationStatus(projectId, syncHostedService, projectLookupService, config);
        if (status is null)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Project not found");
            return TypedResults.NotFound();
        }
        activity?.SetStatus(ActivityStatusCode.Ok, status.Status.ToString());
        return TypedResults.Ok(status);
    }

    /// <summary>
    /// Creation status for a project, or null when LexBox has no such project. In-memory state wins
    /// when we have it; otherwise the status is derived from durable state, so it survives a FwHeadless
    /// restart and a result that has aged out of the cache.
    /// </summary>
    private static async Task<ProjectCreationStatus?> DetermineCreationStatus(
        Guid projectId,
        SyncHostedService syncHostedService,
        IProjectLookupService projectLookupService,
        IOptions<FwHeadlessConfig> config)
    {
        if (syncHostedService.IsProjectBeingCreated(projectId)) return ProjectCreationStatus.Creating;
        var recent = syncHostedService.TryGetRecentCreationResult(projectId);
        if (recent is not null) return recent;

        var projectCode = await projectLookupService.GetProjectCode(projectId);
        if (projectCode is null) return null;

        // A successful creation leaves the .fwdata on disk; a failed one deletes the project folder
        // (ProjectCreationService.CleanupLocalProject). Note this says "project data is here", not "this
        // process created it" -- a project cloned by an ordinary sync looks the same, which is the right
        // answer for a caller asking whether the project is ready to use.
        var fwDataProject = config.Value.GetFwDataProject(projectCode, projectId);
        return File.Exists(fwDataProject.FilePath)
            ? ProjectCreationStatus.Created
            : ProjectCreationStatus.NotCreated;
    }
}
