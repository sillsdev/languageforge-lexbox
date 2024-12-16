using System.Text.RegularExpressions;
using SIL.Harmony;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using LcmCrdt.Objects;

namespace LcmCrdt;

public partial class CrdtProjectsService(IServiceProvider provider, ProjectContext projectContext, ILogger<CrdtProjectsService> logger, IOptions<LcmCrdtConfig> config, IMemoryCache memoryCache)
{
    public Task<CrdtProject[]> ListProjects()
    {
        return Task.FromResult(Directory.EnumerateFiles(config.Value.ProjectPath, "*.sqlite").Select(file =>
        {
            var name = Path.GetFileNameWithoutExtension(file);
            return new CrdtProject(name, file)
            {
                Data = CurrentProjectService.LookupProjectData(memoryCache, name)
            };
        }).ToArray());
    }

    public CrdtProject? GetProject(string name)
    {
        var file = Directory.EnumerateFiles(config.Value.ProjectPath, "*.sqlite")
            .FirstOrDefault(file => Path.GetFileNameWithoutExtension(file) == name);
        return file is null ? null : new CrdtProject(name, file);
    }

    public bool ProjectExists(string name)
    {
        return GetProject(name) is not null;
    }

    public record CreateProjectRequest(
        string Name,
        Guid? Id = null,
        Uri? Domain = null,
        Func<IServiceProvider, CrdtProject, Task>? AfterCreate = null,
        bool SeedNewProjectData = false,
        string? Path = null,
        Guid? FwProjectId = null);

    public async Task<CrdtProject> CreateExampleProject(string name)
    {
        return await CreateProject(new(name, AfterCreate: SampleProjectData, SeedNewProjectData: true));
    }

    public async Task<CrdtProject> CreateProject(CreateProjectRequest request)
    {
        if (!ProjectName().IsMatch(request.Name)) throw new InvalidOperationException("Project name is invalid");
        //poor man's sanitation
        var name = Path.GetFileName(request.Name);
        var sqliteFile = Path.Combine(request.Path ?? config.Value.ProjectPath, $"{name}.sqlite");
        if (File.Exists(sqliteFile)) throw new InvalidOperationException("Project already exists");
        var crdtProject = new CrdtProject(name, sqliteFile);
        await using var serviceScope = CreateProjectScope(crdtProject);
        var db = serviceScope.ServiceProvider.GetRequiredService<LcmCrdtDbContext>();
        try
        {
            var projectData = new ProjectData(name,
                request.Id ?? Guid.NewGuid(),
                ProjectData.GetOriginDomain(request.Domain),
                Guid.NewGuid(), request.FwProjectId);
            await InitProjectDb(db, projectData);
            await serviceScope.ServiceProvider.GetRequiredService<CurrentProjectService>().PopulateProjectDataCache();
            if (request.SeedNewProjectData)
                await SeedSystemData(serviceScope.ServiceProvider.GetRequiredService<DataModel>(), projectData.ClientId);
            await (request.AfterCreate?.Invoke(serviceScope.ServiceProvider, crdtProject) ?? Task.CompletedTask);
        }
        catch
        {
            logger.LogError("Failed to create project {Project}, deleting database", crdtProject.Name);
            await db.Database.EnsureDeletedAsync();
            throw;
        }
        return crdtProject;
    }

    internal static async Task InitProjectDb(LcmCrdtDbContext db, ProjectData data)
    {
        await db.Database.EnsureCreatedAsync();
        db.ProjectData.Add(data);
        await db.SaveChangesAsync();
    }

    internal static async Task SeedSystemData(DataModel dataModel, Guid clientId)
    {
        await PreDefinedData.PredefinedComplexFormTypes(dataModel, clientId);
        await PreDefinedData.PredefinedPartsOfSpeech(dataModel, clientId);
        await PreDefinedData.PredefinedSemanticDomains(dataModel, clientId);
    }

    public AsyncServiceScope CreateProjectScope(CrdtProject crdtProject)
    {
        //todo make this helper method call `CurrentProjectService.PopulateProjectDataCache`
        var serviceScope = provider.CreateAsyncScope();
        SetProjectScope(crdtProject);
        return serviceScope;
    }

    public void SetProjectScope(CrdtProject crdtProject)
    {
        projectContext.Project = crdtProject;
    }

    public CrdtProject SetActiveProject(string name)
    {
        var project = GetProject(name) ?? throw new InvalidOperationException($"Crdt Project {name} not found");
        SetProjectScope(project);
        return project;
    }

    [GeneratedRegex("^[a-zA-Z0-9][a-zA-Z0-9-_]+$")]
    public static partial Regex ProjectName();

    public static async Task SampleProjectData(IServiceProvider provider, CrdtProject project)
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
}
