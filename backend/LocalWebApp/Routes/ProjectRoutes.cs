using System.Text.RegularExpressions;
using FwDataMiniLcmBridge;
using LcmCrdt;
using LocalWebApp.Auth;
using LocalWebApp.Services;
using MiniLcm;

namespace LocalWebApp.Routes;

public static partial class ProjectRoutes
{
    public static IEndpointConventionBuilder MapProjectRoutes(this WebApplication app)
    {
        var group = app.MapGroup("/api").WithOpenApi();
        group.MapGet("/projects",
            async (ProjectsService projectService, LexboxProjectService lexboxProjectService) =>
        {
            var crdtProjects = await projectService.ListProjects();
            var projects = crdtProjects.ToDictionary(p => p.Name, p => new ProjectModel(p.Name, true, false));
            //basically populate projects and indicate if they are lexbox or fwdata
            foreach (var p in FieldWorksProjectList.EnumerateProjects())
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
        Regex alphaNumericRegex = ProjectName();
        group.MapPost("/project",
            async (ProjectsService projectService, string name) =>
            {
                if (string.IsNullOrWhiteSpace(name))
                    return Results.BadRequest("Project name is required");
                if (projectService.ProjectExists(name))
                    return Results.BadRequest("Project already exists");
                if (!alphaNumericRegex.IsMatch(name))
                    return Results.BadRequest("Project name must be alphanumeric");
                await projectService.CreateProject(name, afterCreate: AfterCreate);
                return TypedResults.Ok();
            });
        return group;
    }

    public record ProjectModel(string Name, bool Crdt, bool Fwdata, bool Lexbox = false);

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
                    Definition = { Values = { { "en", "fruit with red, yellow, or green skin with a sweet or tart crispy white flesh" } } },
                    SemanticDomain = ["Fruit"],
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

    [GeneratedRegex("^[a-zA-Z0-9-_]*$")]
    private static partial Regex ProjectName();
}
