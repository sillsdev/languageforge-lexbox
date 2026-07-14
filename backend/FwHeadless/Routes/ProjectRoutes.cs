using FwHeadless.Services;
using LexCore.Entities;
using Microsoft.AspNetCore.Http.HttpResults;
using SIL.WritingSystems;

namespace FwHeadless.Routes;

public static class ProjectRoutes
{
    public static IEndpointConventionBuilder MapProjectRoutes(this WebApplication app)
    {
        var group = app.MapGroup("/api/project");
        group.MapPost("/create-from-template", CreateProjectFromTemplate);
        return group;
    }

    // Internal endpoint: LexBoxApi has already created the project row + empty repo and performed the
    // admin-auth check. FwHeadless populates the empty repo with a template .fwdata configured for the
    // requested writing systems. Not idempotent — on failure LexBoxApi deletes and recreates.
    private static async Task<Results<Ok, ProblemHttpResult>> CreateProjectFromTemplate(
        Guid projectId,
        CreateProjectFromTemplateInput input,
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

        var projectCode = await projectLookupService.GetProjectCode(projectId);
        if (projectCode is null)
        {
            logger.LogError("Create-from-template request for non-existent project {ProjectId}", projectId);
            return TypedResults.Problem("Project not found", statusCode: StatusCodes.Status404NotFound);
        }

        await projectCreationService.CreateFromTemplate(
            projectId, projectCode, input.WsVernacular, input.WsAnalysis, input.AnthropologyCategories);
        return TypedResults.Ok();
    }
}
