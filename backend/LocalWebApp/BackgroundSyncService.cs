﻿using System.Threading.Channels;
using CrdtLib;
using LcmCrdt;
using MiniLcm;

namespace LocalWebApp;

public class BackgroundSyncService(
    IServiceProvider serviceProvider,
    ProjectsService projectsService,
    ProjectContext projectContext) : BackgroundService
{
    private readonly Channel<CrdtProject> _syncResultsChannel = Channel.CreateUnbounded<CrdtProject>();

    public void TriggerSync()
    {
        _syncResultsChannel.Writer.TryWrite(projectContext.Project ??
                                            throw new InvalidOperationException("No project selected"));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(1_000, stoppingToken);
        var crdtProjects = await projectsService.ListProjects();
        foreach (var crdtProject in crdtProjects)
        {
            await SyncProject(crdtProject);
        }

        if (!projectsService.ProjectExists("sena-3"))
        {
            await projectsService.CreateProject("sena-3",
                new Guid("0ebc5976-058d-4447-aaa7-297f8569f968"), //same as sena 3 project id in SeedDatabase
                "http://localhost:5158",
                async (provider, project) =>
                {
                    var (_, _, isSynced) = await SyncProject(project);
                    //seed will only create if missing, so seed anyway since the project should exist on the server always
                    await SeedDb(provider.GetRequiredService<ILexboxApi>());
                });
        }

        await foreach (var project in _syncResultsChannel.Reader.ReadAllAsync(stoppingToken))
        {
            await Task.Delay(100, stoppingToken);
            await SyncProject(project);
        }
    }

    private async Task<SyncResults> SyncProject(CrdtProject crdtProject)
    {
        using var serviceScope = projectsService.CreateProjectScope(crdtProject);
        var syncService = serviceScope.ServiceProvider.GetRequiredService<SyncService>();
        return await syncService.ExecuteSync();
    }

    private async Task SeedDb(ILexboxApi lexboxApi)
    {
        //id is fixed to prevent duplicates
        var id = new Guid("c7328f18-118a-4f83-9d88-c408778b7f63");
        if (await lexboxApi.GetEntry(id) is null)
        {
            await lexboxApi.CreateEntry(new()
            {
                Id = id,
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
                        ExampleSentences =
                        [
                            new() { Sentence = { Values = { { "en", "Kevin is a good guy" } } } }
                        ]
                    }
                ]
            });
        }

        var writingSystems = await lexboxApi.GetWritingSystems();
        if (writingSystems.Analysis.Length == 0)
        {
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

        if (writingSystems.Vernacular.Length == 0)
        {
            await lexboxApi.CreateWritingSystem(WritingSystemType.Vernacular,
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
}
