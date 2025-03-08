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
                [FromQuery] Guid lexboxProjectId) =>
            {
                var server = options.Value.GetServerByAuthority(serverAuthority);
                await syncService.UploadProject(lexboxProjectId, server);
                return TypedResults.Ok();
            });
        group.MapPost("/download/crdt/{serverAuthority}/{projectId}",
            async (IOptions<AuthConfig> options,
                CombinedProjectsService combinedProjectsService,
                Guid projectId,
                [FromQuery] string projectName,
                string serverAuthority
            ) =>
            {
                if (!CrdtProjectsService.ProjectCode().IsMatch(projectName))
                    return Results.BadRequest("Project name is invalid");
                var server = options.Value.GetServerByAuthority(serverAuthority);
                await combinedProjectsService.DownloadProject(projectId, server);
                return TypedResults.Ok();
            });
        return group;
    }
}
