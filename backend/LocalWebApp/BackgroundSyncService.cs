﻿using System.Threading.Channels;
using CrdtLib;
using MiniLcm;

namespace LocalWebApp;

public class BackgroundSyncService(IServiceProvider serviceProvider) : BackgroundService
{
    private readonly Channel<object> _syncResultsChannel = Channel.CreateUnbounded<object>();

    public void TriggerSync()
    {
        _syncResultsChannel.Writer.TryWrite(new());
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(1_000, stoppingToken);
        using var serviceScope = serviceProvider.CreateScope();


        var syncService = serviceScope.ServiceProvider.GetRequiredService<SyncService>();
        await syncService.ExecuteSync();
        //try to seed after sync so we don't create duplicates
        await SeedDb(serviceScope.ServiceProvider.GetRequiredService<ILexboxApi>());
        await foreach (var o in _syncResultsChannel.Reader.ReadAllAsync(stoppingToken))
        {
            await Task.Delay(100, stoppingToken);
            await syncService.ExecuteSync();
        }
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
            await lexboxApi.CreateWritingSystem(WritingSystemType.Analysis, new()
            {
                Id = "en",
                Name = "English",
                Abbreviation = "en",
                Font = "Arial"
            });
        }
        if (writingSystems.Vernacular.Length == 0)
        {
            await lexboxApi.CreateWritingSystem(WritingSystemType.Vernacular, new()
            {
                Id = "en",
                Name = "English",
                Abbreviation = "en",
                Font = "Arial"
            });
        }
    }
}
