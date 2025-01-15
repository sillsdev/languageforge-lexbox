﻿using System.Text.RegularExpressions;
using SIL.Harmony;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using LcmCrdt.Objects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using MiniLcm.Project;

namespace LcmCrdt;

public partial class CrdtProjectsService(IServiceProvider provider, ILogger<CrdtProjectsService> logger, IOptions<LcmCrdtConfig> config, IMemoryCache memoryCache): IProjectProvider
{
    public ProjectDataFormat DataFormat { get; } = ProjectDataFormat.Harmony;
    IEnumerable<IProjectIdentifier> IProjectProvider.ListProjects()
    {
        return ListProjects();
    }

    IProjectIdentifier? IProjectProvider.GetProject(string name)
    {
        return GetProject(name);
    }

    IMiniLcmApi IProjectProvider.OpenProject(IProjectIdentifier project, bool saveChangesOnDispose = true)
    {
        //todo not sure if we should implement this, it's mostly there for the FwData version
        throw new NotImplementedException();
    }

    public IEnumerable<CrdtProject> ListProjects()
    {
        return Directory.EnumerateFiles(config.Value.ProjectPath, "*.sqlite").Select(file =>
        {
            var name = Path.GetFileNameWithoutExtension(file);
            return new CrdtProject(name, file)
            {
                Data = CurrentProjectService.LookupProjectData(memoryCache, name)
            };
        });
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
        await using var serviceScope = provider.CreateAsyncScope();
        var currentProjectService = serviceScope.ServiceProvider.GetRequiredService<CurrentProjectService>();
        currentProjectService.SetupProjectContextForNewDb(crdtProject);
        var db = serviceScope.ServiceProvider.GetRequiredService<LcmCrdtDbContext>();
        try
        {
            var projectData = new ProjectData(name,
                request.Id ?? Guid.NewGuid(),
                ProjectData.GetOriginDomain(request.Domain),
                Guid.NewGuid(), request.FwProjectId);
            await InitProjectDb(db, projectData);
            await currentProjectService.RefreshProjectData();
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
        await db.Database.MigrateAsync();
        db.ProjectData.Add(data);
        await db.SaveChangesAsync();
    }

    internal static async Task SeedSystemData(DataModel dataModel, Guid clientId)
    {
        await PreDefinedData.PredefinedComplexFormTypes(dataModel, clientId);
        await PreDefinedData.PredefinedPartsOfSpeech(dataModel, clientId);
        await PreDefinedData.PredefinedSemanticDomains(dataModel, clientId);
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
