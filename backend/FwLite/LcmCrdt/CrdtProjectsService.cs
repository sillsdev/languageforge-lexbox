﻿using System.Diagnostics;
using System.Text.RegularExpressions;
using SIL.Harmony;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using LcmCrdt.Objects;
using Microsoft.Data.Sqlite;
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

    public async ValueTask<IMiniLcmApi> OpenProject(IProjectIdentifier project, IServiceProvider serviceProvider, bool saveChangesOnDispose = true)
    {
        if (project is not CrdtProject crdtProject) throw new ArgumentException("Project is not a crdt project");
        return await serviceProvider.OpenCrdtProject(crdtProject);
    }

    private async Task<ProjectData> EnsureProjectDataCacheIsLoaded(CrdtProject project)
    {
        if (project.Data is not null) return project.Data;
        await using var scope = provider.CreateAsyncScope();
        var scopedServices = scope.ServiceProvider;
        var currentProjectService = scopedServices.GetRequiredService<CurrentProjectService>();
        return await currentProjectService.SetupProjectContext(project);
    }

    public async ValueTask EnsureProjectDataCacheIsLoaded()
    {
        var tasks = ListProjects().Where(p => p.Data is null).Select(EnsureProjectDataCacheIsLoaded).ToArray();
        if (tasks is []) return;
        await Task.WhenAll(tasks);
    }

    public IEnumerable<CrdtProject> ListProjects()
    {
        return Directory.EnumerateFiles(config.Value.ProjectPath, "*.sqlite").Select(file =>
        {
            var name = Path.GetFileNameWithoutExtension(file);
            return new CrdtProject(name, file, memoryCache);
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
        Guid? FwProjectId = null,
        string? AuthenticatedUser = null,
        string? AuthenticatedUserId = null);

    public async Task<CrdtProject> CreateExampleProject(string name)
    {
        return await CreateProject(new(name, AfterCreate: SampleProjectData, SeedNewProjectData: true));
    }

    public async Task<CrdtProject> CreateProject(CreateProjectRequest request)
    {
        using var activity = LcmCrdtActivitySource.Value.StartActivity();
        activity?.SetTag("app.project_id", request.Id);
        if (!ProjectName().IsMatch(request.Name))
        {
            var nameIsInvalid = $"Project name '{request.Name}' is invalid";
            activity?.SetStatus(ActivityStatusCode.Error, nameIsInvalid);
            throw new InvalidOperationException(nameIsInvalid);
        }

        //poor man's sanitation
        var name = Path.GetFileName(request.Name);
        var sqliteFile = Path.Combine(request.Path ?? config.Value.ProjectPath, $"{name}.sqlite");
        if (File.Exists(sqliteFile))
        {
            var alreadyExists = $"Project already exists at '{sqliteFile}'";
            activity?.SetStatus(ActivityStatusCode.Error, alreadyExists);
            throw new InvalidOperationException(alreadyExists);
        }

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
                Guid.NewGuid(), request.FwProjectId, request.AuthenticatedUser, request.AuthenticatedUserId);
            crdtProject.Data = projectData;
            await InitProjectDb(db, projectData);
            await currentProjectService.RefreshProjectData();
            if (request.SeedNewProjectData)
                await SeedSystemData(serviceScope.ServiceProvider.GetRequiredService<DataModel>(), projectData.ClientId);
            await (request.AfterCreate?.Invoke(serviceScope.ServiceProvider, crdtProject) ?? Task.CompletedTask);
        }
        catch(Exception e)
        {
            logger.LogError(e, "Failed to create project {Project}, deleting database", crdtProject.Name);
            activity?.AddException(e);
            await db.Database.CloseConnectionAsync();
            EnsureDeleteProject(sqliteFile);
            throw;
        }
        return crdtProject;
    }

    private void EnsureDeleteProject(string sqliteFile)
    {
        _ = Task.Run(async () =>
        {
            var counter = 0;
            while (File.Exists(sqliteFile) && counter < 10)
            {
                await Task.Delay(1000);
                try
                {
                    File.Delete(sqliteFile);
                    return;
                }
                catch (IOException)
                {
                    //inuse, try again
                }
                catch (Exception exception)
                {
                    logger.LogError(exception, "Failed to delete sqlite file {SqliteFile}", sqliteFile);
                    return;
                }
                counter++;
            }
            logger.LogError("Failed to delete sqlite file {SqliteFile} after 10 attempts", sqliteFile);
        });
    }

    public async Task DeleteProject(string name)
    {
        var project = GetProject(name) ?? throw new InvalidOperationException($"Project {name} not found");
        await using var serviceScope = provider.CreateAsyncScope();
        var currentProjectService = serviceScope.ServiceProvider.GetRequiredService<CurrentProjectService>();
        currentProjectService.SetupProjectContextForNewDb(project);
        var db = serviceScope.ServiceProvider.GetRequiredService<LcmCrdtDbContext>();
        await db.Database.EnsureDeletedAsync();
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
        await lexboxApi.CreateWritingSystem(WritingSystemType.Vernacular,
            new()
            {
                Id = Guid.NewGuid(),
                Type = WritingSystemType.Vernacular,
                WsId = "de",
                Name = "German",
                Abbreviation = "de",
                Font = "Arial",
                Exemplars = WritingSystem.LatinExemplars
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
        await lexboxApi.CreateWritingSystem(WritingSystemType.Analysis,
            new()
            {
                Id = Guid.NewGuid(),
                Type = WritingSystemType.Analysis,
                WsId = "fr",
                Name = "French",
                Abbreviation = "fr",
                Font = "Arial",
                Exemplars = WritingSystem.LatinExemplars
            });

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
    }
}
