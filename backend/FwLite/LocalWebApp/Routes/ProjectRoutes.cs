﻿using System.Text.RegularExpressions;
using FwDataMiniLcmBridge;
using LcmCrdt;
using LocalWebApp.Auth;
using LocalWebApp.Hubs;
using LocalWebApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MiniLcm;
using MiniLcm.Models;

namespace LocalWebApp.Routes;

public static partial class ProjectRoutes
{
    public static IEndpointConventionBuilder MapProjectRoutes(this WebApplication app)
    {
        var group = app.MapGroup("/api").WithOpenApi();
        group.MapGet("/remoteProjects",
            async (
                LexboxProjectService lexboxProjectService,
                IOptions<AuthConfig> options) =>
            {
                var serversProjects = new Dictionary<string, ProjectModel[]>();
                foreach (var server in options.Value.LexboxServers)
                {
                    var lexboxProjects = await lexboxProjectService.GetLexboxProjects(server);
                    serversProjects.Add(server.Authority.Authority, lexboxProjects.Select(p => new ProjectModel
                            (p.Name, Crdt: p.IsCrdtProject, Fwdata: false, Lexbox: true, server.Authority.Authority, p.Id))
                        .ToArray());
                }

                return serversProjects;
            });
        group.MapGet("/localProjects",
            async (
            ProjectsService projectService,
            FieldWorksProjectList fieldWorksProjectList) =>
        {
            var crdtProjects = await projectService.ListProjects();
            //todo get project Id and use that to specify the Id in the model. Also pull out server
            var projects = crdtProjects.ToDictionary(p => p.Name, p =>
            {
                var uri = p.Data?.OriginDomain is not null ? new Uri(p.Data.OriginDomain) : null;
                return new ProjectModel(p.Name,
                    true,
                    false,
                    p.Data?.OriginDomain is not null,
                    uri?.Authority,
                    p.Data?.Id);
            });
            //basically populate projects and indicate if they are lexbox or fwdata
            foreach (var p in fieldWorksProjectList.EnumerateProjects())
            {
                if (projects.TryGetValue(p.Name, out var project))
                {
                    projects[p.Name] = project with { Fwdata = true };
                }
                else
                {
                    projects.Add(p.Name, new ProjectModel(p.Name, false, true));
                }
            }
            return projects.Values;
        });
        group.MapPost("/project",
            async (ProjectsService projectService, string name) =>
            {
                if (string.IsNullOrWhiteSpace(name))
                    return Results.BadRequest("Project name is required");
                if (projectService.ProjectExists(name))
                    return Results.BadRequest("Project already exists");
                if (!ProjectName().IsMatch(name))
                    return Results.BadRequest("Only letters, numbers, '-' and '_' are allowed");
                await projectService.CreateProject(new(name, AfterCreate: AfterCreate, SeedNewProjectData: true));
                return TypedResults.Ok();
            });
        group.MapPost($"/upload/crdt/{{serverAuthority}}/{{{CrdtMiniLcmApiHub.ProjectRouteKey}}}",
            async (LexboxProjectService lexboxProjectService,
                SyncService syncService,
                IOptions<AuthConfig> options,
                CurrentProjectService currentProjectService,
                string serverAuthority,
                [FromQuery] Guid lexboxProjectId) =>
            {
                var server = options.Value.GetServerByAuthority(serverAuthority);
                await currentProjectService.SetProjectSyncOrigin(server.Authority, lexboxProjectId);
                try
                {
                    await syncService.ExecuteSync();
                }
                catch
                {
                    await currentProjectService.SetProjectSyncOrigin(null, null);
                    throw;
                }
                lexboxProjectService.InvalidateProjectsCache(server);
                return TypedResults.Ok();
            });
        group.MapPost("/download/crdt/{serverAuthority}/{projectId}",
            async (LexboxProjectService lexboxProjectService,
                IOptions<AuthConfig> options,
                ProjectsService projectService,
                Guid projectId,
                [FromQuery] string projectName,
                string serverAuthority
            ) =>
            {
                if (!ProjectName().IsMatch(projectName))
                    return Results.BadRequest("Project name is invalid");
                var server = options.Value.GetServerByAuthority(serverAuthority);
                await projectService.CreateProject(new(projectName,
                    projectId,
                    server.Authority,
                    async (provider, project) =>
                    {
                        await provider.GetRequiredService<SyncService>().ExecuteSync();
                    },
                    SeedNewProjectData: false));
                return TypedResults.Ok();
            });
        return group;
    }

    public record ProjectModel(string Name, bool Crdt, bool Fwdata, bool Lexbox = false, string? ServerAuthority = null, Guid? Id = null);

    private static async Task AfterCreate(IServiceProvider provider, CrdtProject project)
    {
        var lexboxApi = provider.GetRequiredService<IMiniLcmApi>();
        await lexboxApi.CreateEntry(new()
        {
            Id = Guid.NewGuid(),
            LexemeForm = { Values = { { "en", "Apple" } } },
            CitationForm = { Values = { { "en", "Apple" } } },
            LiteralMeaning = { Values = { { "en", "Fruit" } } },
            Senses =
            [
                new()
                {
                    Gloss = { Values = { { "en", "Fruit" } } },
                    Definition =
                    {
                        Values =
                        {
                            {
                                "en",
                                "fruit with red, yellow, or green skin with a sweet or tart crispy white flesh"
                            }
                        }
                    },
                    SemanticDomains = [],
                    ExampleSentences = [new() { Sentence = { Values = { { "en", "We ate an apple" } } } }]
                }
            ]
        });

        await lexboxApi.CreateWritingSystem(WritingSystemType.Vernacular,
            new()
            {
                Id = Guid.NewGuid(),
                Type = WritingSystemType.Vernacular,
                WsId = "en",
                Name = "English",
                Abbreviation = "en",
                Font = "Arial",
                Exemplars = WritingSystem.LatinExemplars
            });

        await lexboxApi.CreateWritingSystem(WritingSystemType.Analysis,
            new()
            {
                Id = Guid.NewGuid(),
                Type = WritingSystemType.Analysis,
                WsId = "en",
                Name = "English",
                Abbreviation = "en",
                Font = "Arial",
                Exemplars = WritingSystem.LatinExemplars
            });
    }

    [GeneratedRegex("^[a-zA-Z0-9][a-zA-Z0-9-_]+$")]
    private static partial Regex ProjectName();
}
