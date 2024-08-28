using System.Text.RegularExpressions;
using FwDataMiniLcmBridge;
using LcmCrdt;
using LocalWebApp.Auth;
using LocalWebApp.Hubs;
using LocalWebApp.Services;
using Microsoft.Extensions.Options;
using MiniLcm;

namespace LocalWebApp.Routes;

public static partial class ProjectRoutes
{
    public record ProjectsResponse(ProjectModel[] LocalProjects, Dictionary<string, ProjectModel[]> ServersProjects);
    public static IEndpointConventionBuilder MapProjectRoutes(this WebApplication app)
    {
        var group = app.MapGroup("/api").WithOpenApi();
        group.MapGet("/projects",
            async (ProjectsService projectService,
                LexboxProjectService lexboxProjectService,
                FieldWorksProjectList fieldWorksProjectList,
                IOptions<AuthConfig> options) =>
            {
                var crdtProjects = await projectService.ListProjects();
                //todo get project Id and use that to specify the Id in the model
                var projects = crdtProjects.ToDictionary(p => p.Name, p => new ProjectModel(p.Name, true, false));
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


                var serversProjects = new Dictionary<string, ProjectModel[]>();
                foreach (var server in options.Value.LexboxServers)
                {
                    var lexboxProjects = await lexboxProjectService.GetLexboxProjects(server);
                    serversProjects.Add(server.DisplayName, lexboxProjects.Select(p => new ProjectModel(p.Name, false, false, true, server.DisplayName, p.Id)).ToArray());
                }

                return new ProjectsResponse(projects.Values.ToArray(), serversProjects);
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
                await projectService.CreateProject(new(name, AfterCreate: AfterCreate));
                return TypedResults.Ok();
            });
        group.MapPost($"/upload/crdt/{{serverName}}/{{{CrdtMiniLcmApiHub.ProjectRouteKey}}}",
            async (LexboxProjectService lexboxProjectService,
                SyncService syncService,
                IOptions<AuthConfig> options,
                CurrentProjectService currentProjectService,
                string serverName) =>
            {
                var server = options.Value.GetServer(serverName);
                var foundProjectGuid =
                    await lexboxProjectService.GetLexboxProjectId(server, currentProjectService.ProjectData.Name);
                if (foundProjectGuid is null)
                    return Results.BadRequest(
                        $"Project code {currentProjectService.ProjectData.Name} not found on lexbox");
                await currentProjectService.SetProjectSyncOrigin(server.Authority, foundProjectGuid);
                await syncService.ExecuteSync();
                return TypedResults.Ok();
            });
        group.MapPost("/download/crdt/{serverName}/{newProjectName}",
            async (LexboxProjectService lexboxProjectService,
                IOptions<AuthConfig> options,
                ProjectsService projectService,
                string newProjectName,
                string serverName
            ) =>
            {
                if (!ProjectName().IsMatch(newProjectName))
                    return Results.BadRequest("Project name is invalid");
                var server = options.Value.GetServer(serverName);
                var foundProjectGuid = await lexboxProjectService.GetLexboxProjectId(server,newProjectName);
                if (foundProjectGuid is null)
                    return Results.BadRequest($"Project code {newProjectName} not found on lexbox");
                await projectService.CreateProject(new(newProjectName,
                    foundProjectGuid.Value,
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

    public record ProjectModel(string Name, bool Crdt, bool Fwdata, bool Lexbox = false, string? Server = null, Guid? Id = null);

    private static async Task AfterCreate(IServiceProvider provider, CrdtProject project)
    {
        var lexboxApi = provider.GetRequiredService<ILexboxApi>();
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
                Id = "en",
                Name = "English",
                Abbreviation = "en",
                Font = "Arial",
                Exemplars = WritingSystem.LatinExemplars
            });

        await lexboxApi.CreateWritingSystem(WritingSystemType.Analysis,
            new()
            {
                Id = "en",
                Name = "English",
                Abbreviation = "en",
                Font = "Arial",
                Exemplars = WritingSystem.LatinExemplars
            });
    }

    [GeneratedRegex("^[a-zA-Z0-9][a-zA-Z0-9-_]+$")]
    private static partial Regex ProjectName();
}
