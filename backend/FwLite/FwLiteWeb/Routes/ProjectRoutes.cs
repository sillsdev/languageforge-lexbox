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

namespace FwLiteWeb.Routes;

public static class ProjectRoutes
{
    public static IEndpointConventionBuilder MapProjectRoutes(this WebApplication app)
    {
        var group = app.MapGroup("/api").WithOpenApi();
        group.MapGet("/remoteProjects",
            async (CombinedProjectsService combinedProjectsService) =>
            {
                return (await combinedProjectsService.RemoteProjects()).ToDictionary(p => p.Server.Authority.Authority, p => p.Projects);
            });
        group.MapGet("/localProjects",
            (CombinedProjectsService combinedProjectsService) => combinedProjectsService.LocalProjects());
        group.MapPost("/project",
            async (CrdtProjectsService projectService, string name) =>
            {
                if (string.IsNullOrWhiteSpace(name))
                    return Results.BadRequest("Project name is required");
                if (projectService.ProjectExists(name))
                    return Results.BadRequest("Project already exists");
                if (!CrdtProjectsService.ProjectCode().IsMatch(name))
                    return Results.BadRequest("Only letters, numbers, '-' and '_' are allowed");
                await projectService.CreateExampleProject(name);
                return TypedResults.Ok();
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
        group.MapDelete("/crdt/{code}",
            async (CrdtProjectsService projectService, string code) =>
            {
                await projectService.DeleteProject(code);
                return TypedResults.Ok();
            });
        return group;
    }
}
