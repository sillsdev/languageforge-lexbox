using System.Text.RegularExpressions;
using FwDataMiniLcmBridge;
using LcmCrdt;
using LocalWebApp.Auth;
using MiniLcm;

namespace LocalWebApp.Routes;

public static class ProjectRoutes
{
    public static IEndpointConventionBuilder MapProjectRoutes(this WebApplication app)
    {
        var group = app.MapGroup("/api").WithOpenApi();
        group.MapGet("/projects",
            async (ProjectsService projectService) =>
        {
            var crdtProjects = await projectService.ListProjects();
            return crdtProjects.UnionBy(FieldWorksProjectList.EnumerateProjects(), identifier => identifier.Name);
        });
        Regex alphaNumericRegex = new Regex("^[a-zA-Z0-9]*$");
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

    private static async Task AfterCreate(IServiceProvider provider, CrdtProject project)
    {
        var lexboxApi = provider.GetRequiredService<ILexboxApi>();
        await lexboxApi.CreateEntry(new()
        {
            Id = Guid.NewGuid(),
            LexemeForm = { Values = { { "en", "Kevin" } } },
            Note = { Values = { { "en", "this is a test note from Kevin" } } },
            CitationForm = { Values = { { "en", "Kevin" } } },
            LiteralMeaning = { Values = { { "en", "Kevin" } } },
            Senses =
            [
                new()
                {
                    Gloss = { Values = { { "en", "Kevin" } } },
                    Definition = { Values = { { "en", "Kevin" } } },
                    SemanticDomain = ["Person"],
                    ExampleSentences = [new() { Sentence = { Values = { { "en", "Kevin is a good guy" } } } }]
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
}
