using System.Text.RegularExpressions;
using FwDataMiniLcmBridge;
using FwLiteShared;
using FwLiteShared.Auth;
using FwLiteShared.Projects;
using FwLiteShared.Sync;
using LcmCrdt;
using FwLiteWeb.Hubs;
using FwLiteWeb.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MiniLcm;
using MiniLcm.Models;
using SIL.WritingSystems;

namespace FwLiteWeb.Routes;

public static class ProjectRoutes
{
    public static IEndpointConventionBuilder MapProjectRoutes(this WebApplication app)
    {
        var group = app.MapGroup("/api");
        group.MapGet("/remoteProjects",
            async (CombinedProjectsService combinedProjectsService) =>
            {
                return (await combinedProjectsService.RemoteProjects()).ToDictionary(p => p.Server.Authority.Authority, p => p.Projects);
            });
        group.MapGet("/localProjects",
            (CombinedProjectsService combinedProjectsService) => combinedProjectsService.LocalProjects());
        // Name is free-form; code identifies the project on disk and must match ProjectCode().
        group.MapPost("/project",
            async (CrdtProjectsService projectService, string name, string code, string vernacularWs) =>
            {
                if (string.IsNullOrWhiteSpace(name)) return Results.BadRequest("Project name is required");
                if (ValidateProjectCode(projectService, code) is { } codeError) return codeError;
                if (string.IsNullOrWhiteSpace(vernacularWs))
                    return Results.BadRequest("Vernacular writing system is required");
                if (!IetfLanguageTag.IsValid(vernacularWs))
                    return Results.BadRequest($"'{vernacularWs}' is not a valid IETF language tag");
                await projectService.CreateProjectFromTemplate(new(name, code, Role: UserProjectRole.Manager, VernacularWs: vernacularWs));
                return Results.Ok();
            });
        // Example/demo project — built from the template (parts of speech, complex form types,
        // semantic domains, custom views, morph types) plus a handful of demo entries. Dev use.
        group.MapPost("/project/demo",
            async (CrdtProjectsService projectService, string name) =>
            {
                if (string.IsNullOrWhiteSpace(name)) return Results.BadRequest("Project name is required");
                // Display name keeps its casing; the on-disk code is its lowercased form (see CreateExampleProject).
                if (ValidateProjectCode(projectService, name.ToLowerInvariant()) is { } error) return error;
                await projectService.CreateExampleProject(name);
                return Results.Ok();
            });
        group.MapPost($"/upload/crdt/{{serverAuthority}}/{{{CrdtMiniLcmApiHub.ProjectRouteKey}}}",
            async (SyncService syncService,
                IOptions<AuthConfig> options,
                string serverAuthority,
                [FromRoute(Name = CrdtMiniLcmApiHub.ProjectRouteKey)]string project,
                [FromQuery] Guid lexboxProjectId) =>
            {
                var server = options.Value.GetServerByAuthority(serverAuthority);
                await syncService.UploadProject(lexboxProjectId, server);
                return TypedResults.Ok();
            });
        group.MapPost("/download/crdt/{serverAuthority}/{code}",
            async (IOptions<AuthConfig> options,
                CombinedProjectsService combinedProjectsService,
                string code,
                string serverAuthority,
                [FromQuery] UserProjectRole? role
            ) =>
            {
                var server = options.Value.GetServerByAuthority(serverAuthority);
                var result = await combinedProjectsService.DownloadProjectByCode(code, server, role);
                return result switch
                {
                    DownloadProjectByCodeResult.Success => Results.Ok(),
                    DownloadProjectByCodeResult.Forbidden => Results.Forbid(),
                    DownloadProjectByCodeResult.NotCrdtProject => Results.InternalServerError("Not a CRDT project"),
                    DownloadProjectByCodeResult.ProjectNotFound => Results.NotFound("Project not found"),
                    DownloadProjectByCodeResult.ProjectAlreadyDownloaded => Results.NoContent(),
                    // If we reach this point then we updated DownloadProjectByCodeResult and forgot to update this switch
                    _ => Results.InternalServerError("DownloadProjectByCodeResult enum value not handled, please inform FW Lite devs")
                };
            });
        return group;
    }

    private static IResult? ValidateProjectCode(CrdtProjectsService projectService, string code)
    {
        if (string.IsNullOrWhiteSpace(code)) return Results.BadRequest("Project code is required");
        if (!CrdtProjectsService.ProjectCode().IsMatch(code))
            return Results.BadRequest("Only lowercase letters, numbers and '-' are allowed");
        if (projectService.ProjectExists(code)) return Results.BadRequest("Project already exists");
        return null;
    }
}
