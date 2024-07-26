﻿using FwLiteProjectSync.Tests.Fixtures;
using MiniLcm;
using SystemTextJsonPatch;

namespace FwLiteProjectSync.Tests;

public class SyncTests : IClassFixture<SyncFixture>
{
    private readonly SyncFixture _fixture;
    private readonly CrdtFwdataProjectSyncService _syncService;

    private Entry _testEntry = new Entry
    {
        Id = Guid.NewGuid(),
        LexemeForm = { Values = { { "en", "Apple" } } },
        Note = { Values = { { "en", "this is a test note" } } },
        Senses =
        [
            new Sense
            {
                Gloss = { Values = { { "en", "Apple" } } },
                Definition = { Values = { { "en", "a round fruit with a hard, crisp skin" } } },
                ExampleSentences =
                [
                    new ExampleSentence { Sentence = { Values = { { "en", "I went to the store to buy an apple." } } } }
                ]
            }
        ]
    };

    public SyncTests(SyncFixture fixture)
    {
        _fixture = fixture;
        _syncService = _fixture.SyncService;
    }

    [Fact]
    public async Task FirstSyncJustDoesAnImport()
    {
        var crdtApi = _fixture.CrdtApi;
        var fwdataApi = _fixture.FwDataApi;
        await fwdataApi.CreateEntry(_testEntry);
        await _syncService.Sync(crdtApi, fwdataApi);

        var crdtEntries = await crdtApi.GetEntries().ToArrayAsync();
        var fwdataEntries = await fwdataApi.GetEntries().ToArrayAsync();
        crdtEntries.Should().BeEquivalentTo(fwdataEntries);
    }

    [Fact]
    public async Task CreatingAnEntryInEachProjectSyncsAcrossBoth()
    {
        var crdtApi = _fixture.CrdtApi;
        var fwdataApi = _fixture.FwDataApi;
        await fwdataApi.CreateEntry(_testEntry);
        await _syncService.Sync(crdtApi, fwdataApi);

        await fwdataApi.CreateEntry(new Entry()
        {
            LexemeForm = { { "en", "Pear" } },
            Senses =
            [
                new Sense() { Gloss = { { "en", "Pear" } }, }
            ]
        });
        await crdtApi.CreateEntry(new Entry()
        {
            LexemeForm = { { "en", "Banana" } },
            Senses =
            [
                new Sense() { Gloss = { { "en", "Banana" } }, }
            ]
        });
        await _syncService.Sync(crdtApi, fwdataApi);

        var crdtEntries = await crdtApi.GetEntries().ToArrayAsync();
        var fwdataEntries = await fwdataApi.GetEntries().ToArrayAsync();
        crdtEntries.Should().BeEquivalentTo(fwdataEntries);
    }

    [Fact]
    public async Task UpdatingAnEntryInEachProjectSyncsAcrossBoth()
    {
        var crdtApi = _fixture.CrdtApi;
        var fwdataApi = _fixture.FwDataApi;
        await fwdataApi.CreateEntry(_testEntry);
        await fwdataApi.CreateWritingSystem(WritingSystemType.Vernacular, new WritingSystem() { Id = new WritingSystemId("es"), Name = "Spanish", Abbreviation = "es", Font = "Arial" });
        await fwdataApi.CreateWritingSystem(WritingSystemType.Vernacular, new WritingSystem() { Id = new WritingSystemId("fr"), Name = "French", Abbreviation = "fr", Font = "Arial" });
        await _syncService.Sync(crdtApi, fwdataApi);

        var jsonPatchDocument = new JsonPatchDocument<Entry>();
        jsonPatchDocument.Replace(entry => entry.LexemeForm["es"], "Manzana");
        await crdtApi.UpdateEntry(_testEntry.Id, new JsonPatchUpdateInput<Entry>(jsonPatchDocument));

        var fwdataPatch = new JsonPatchDocument<Entry>();
        fwdataPatch.Replace(entry => entry.LexemeForm["fr"], "Pomme");
        await fwdataApi.UpdateEntry(_testEntry.Id, new JsonPatchUpdateInput<Entry>(fwdataPatch));
        await _syncService.Sync(crdtApi, fwdataApi);

        var crdtEntries = await crdtApi.GetEntries().ToArrayAsync();
        var fwdataEntries = await fwdataApi.GetEntries().ToArrayAsync();
        crdtEntries.Should().BeEquivalentTo(fwdataEntries);
    }

    [Fact]
    public async Task AddingASenseToAnEntryInEachProjectSyncsAcrossBoth()
    {
        var crdtApi = _fixture.CrdtApi;
        var fwdataApi = _fixture.FwDataApi;
        await fwdataApi.CreateEntry(_testEntry);
        await _syncService.Sync(crdtApi, fwdataApi);

        await fwdataApi.CreateSense(_testEntry.Id, new Sense()
        {
            Gloss = { { "en", "Fruit" } },
            Definition = { { "en", "a round fruit, red or yellow" } },
        });
        await crdtApi.CreateSense(_testEntry.Id, new Sense()
        {
            Gloss = { { "en", "Tree" } },
            Definition = { { "en", "a tall, woody plant, which grows fruit" } },
        });

        await _syncService.Sync(crdtApi, fwdataApi);

        var crdtEntries = await crdtApi.GetEntries().ToArrayAsync();
        var fwdataEntries = await fwdataApi.GetEntries().ToArrayAsync();
        crdtEntries.Should().BeEquivalentTo(fwdataEntries);
    }
}
