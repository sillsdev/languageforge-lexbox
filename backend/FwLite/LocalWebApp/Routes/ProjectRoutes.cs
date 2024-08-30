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
    public static IEndpointConventionBuilder MapProjectRoutes(this WebApplication app)
    {
        var group = app.MapGroup("/api").WithOpenApi();
        group.MapGet("/projects",
            async (ProjectsService projectService, LexboxProjectService lexboxProjectService, FieldWorksProjectList fieldWorksProjectList) =>
        {
            var crdtProjects = await projectService.ListProjects();
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
            //todo split this out into it's own request so we can return other project types right away
            foreach (var lexboxProject in await lexboxProjectService.GetLexboxProjects())
            {
                if (projects.TryGetValue(lexboxProject.Name, out var project))
                {
                    projects[lexboxProject.Name] = project with { Lexbox = true };
                }
                else
                {
                    projects.Add(lexboxProject.Name, new ProjectModel(lexboxProject.Name, false, false, true));
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
                await projectService.CreateProject(new(name, AfterCreate: AfterCreate));
                return TypedResults.Ok();
            });
        group.MapPost($"/upload/crdt/{{{CrdtMiniLcmApiHub.ProjectRouteKey}}}",
            async (LexboxProjectService lexboxProjectService,
                SyncService syncService,
                IOptions<AuthConfig> options,
                CurrentProjectService currentProjectService) =>
            {
                //todo let the user pick a project to upload to instead of matching the name with the project code.
                var foundProjectGuid =
                    await lexboxProjectService.GetLexboxProjectId(currentProjectService.ProjectData.Name);
                if (foundProjectGuid is null)
                    return Results.BadRequest(
                        $"Project code {currentProjectService.ProjectData.Name} not found on lexbox");
                await currentProjectService.SetProjectSyncOrigin(options.Value.DefaultAuthority, foundProjectGuid);
                await syncService.ExecuteSync();
                return TypedResults.Ok();
            });
        group.MapPost("/download/crdt/{newProjectName}",
            async (LexboxProjectService lexboxProjectService,
                IOptions<AuthConfig> options,
                ProjectsService projectService,
                string newProjectName
                ) =>
            {
                if (!ProjectName().IsMatch(newProjectName))
                    return Results.BadRequest("Project name is invalid");
                var foundProjectGuid = await lexboxProjectService.GetLexboxProjectId(newProjectName);
                if (foundProjectGuid is null)
                    return Results.BadRequest($"Project code {newProjectName} not found on lexbox");
                await projectService.CreateProject(new(newProjectName, foundProjectGuid.Value, options.Value.DefaultAuthority,
                    async (provider, project) =>
                    {
                        await provider.GetRequiredService<SyncService>().ExecuteSync();
                    }, SeedNewProjectData: false));
                return TypedResults.Ok();
            });
        return group;
    }

    public record ProjectModel(string Name, bool Crdt, bool Fwdata, bool Lexbox = false);

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
                    Definition = { Values = { { "en", "fruit with red, yellow, or green skin with a sweet or tart crispy white flesh" } } },
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
