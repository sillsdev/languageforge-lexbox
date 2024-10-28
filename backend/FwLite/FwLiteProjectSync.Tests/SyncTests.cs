﻿using FwLiteProjectSync.Tests.Fixtures;
using LcmCrdt;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MiniLcm;
using MiniLcm.Models;
using SystemTextJsonPatch;

namespace FwLiteProjectSync.Tests;

public class SyncTests : IClassFixture<SyncFixture>, IAsyncLifetime
{
    private readonly SyncFixture _fixture;
    private readonly CrdtFwdataProjectSyncService _syncService;

    private readonly Guid _complexEntryId = Guid.NewGuid();
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

    public async Task InitializeAsync()
    {
        await _fixture.FwDataApi.CreateEntry(_testEntry);
        await _fixture.FwDataApi.CreateEntry(new Entry()
        {
            Id = _complexEntryId,
            LexemeForm = { { "en", "Pineapple" } },
            Components =
            [
                new ComplexFormComponent()
                {
                    Id = Guid.NewGuid(),
                    ComplexFormEntryId = _complexEntryId,
                    ComplexFormHeadword = "Pineapple",
                    ComponentEntryId = _testEntry.Id,
                    ComponentHeadword = "Apple"
                }
            ]
        });
    }

    public async Task DisposeAsync()
    {
        await foreach (var entry in _fixture.FwDataApi.GetEntries())
        {
            await _fixture.FwDataApi.DeleteEntry(entry.Id);
        }
        await foreach (var entry in _fixture.CrdtApi.GetEntries())
        {
            await _fixture.CrdtApi.DeleteEntry(entry.Id);
        }
    }

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
        await _syncService.Sync(crdtApi, fwdataApi);

        var crdtEntries = await crdtApi.GetEntries().ToArrayAsync();
        var fwdataEntries = await fwdataApi.GetEntries().ToArrayAsync();
        crdtEntries.Should().BeEquivalentTo(fwdataEntries,
            options => options.For(e => e.Components).Exclude(c => c.Id)
                              .For(e => e.ComplexForms).Exclude(c => c.Id));
    }

    [Fact]
    public async Task SyncFailsWithMismatchedProjectIds()
    {
        var crdtApi = _fixture.CrdtApi;
        var fwdataApi = _fixture.FwDataApi;
        await _syncService.Sync(crdtApi, fwdataApi);

        var newFwProjectId = Guid.NewGuid();
        await _fixture.Services.GetRequiredService<LcmCrdtDbContext>().ProjectData.
            ExecuteUpdateAsync(updates => updates.SetProperty(p => p.FwProjectId, newFwProjectId));
        await _fixture.Services.GetRequiredService<CurrentProjectService>().PopulateProjectDataCache(force: true);
        Func<Task> syncTask = async () => await _syncService.Sync(crdtApi, fwdataApi);
        await syncTask.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task CreatingAnEntryInEachProjectSyncsAcrossBoth()
    {
        var crdtApi = _fixture.CrdtApi;
        var fwdataApi = _fixture.FwDataApi;
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
        crdtEntries.Should().BeEquivalentTo(fwdataEntries,
            options => options.For(e => e.Components).Exclude(c => c.Id)
                .For(e => e.ComplexForms).Exclude(c => c.Id));
    }

    [Fact]
    public async Task UpdatingAnEntryInEachProjectSyncsAcrossBoth()
    {
        var crdtApi = _fixture.CrdtApi;
        var fwdataApi = _fixture.FwDataApi;
        await fwdataApi.CreateWritingSystem(WritingSystemType.Vernacular, new WritingSystem() { Id = Guid.NewGuid(), Type = WritingSystemType.Vernacular, WsId = new WritingSystemId("es"), Name = "Spanish", Abbreviation = "es", Font = "Arial" });
        await fwdataApi.CreateWritingSystem(WritingSystemType.Vernacular, new WritingSystem() { Id = Guid.NewGuid(), Type = WritingSystemType.Vernacular, WsId = new WritingSystemId("fr"), Name = "French", Abbreviation = "fr", Font = "Arial" });
        await _syncService.Sync(crdtApi, fwdataApi);

        await crdtApi.UpdateEntry(_testEntry.Id, new UpdateObjectInput<Entry>().Set(entry => entry.LexemeForm["es"],"Manzana"));

        await fwdataApi.UpdateEntry(_testEntry.Id, new UpdateObjectInput<Entry>().Set(entry => entry.LexemeForm["fr"], "Pomme"));
        var results = await _syncService.Sync(crdtApi, fwdataApi);
        results.CrdtChanges.Should().Be(1);
        results.FwdataChanges.Should().Be(1);

        var crdtEntries = await crdtApi.GetEntries().ToArrayAsync();
        var fwdataEntries = await fwdataApi.GetEntries().ToArrayAsync();
        crdtEntries.Should().BeEquivalentTo(fwdataEntries,
            options => options
                .For(e => e.Components).Exclude(c => c.Id)
                //todo the headword should be changed
                .For(e => e.Components).Exclude(c => c.ComponentHeadword)
                .For(e => e.ComplexForms).Exclude(c => c.Id)
                .For(e => e.ComplexForms).Exclude(c => c.ComponentHeadword));
    }

    [Fact]
    public async Task AddingASenseToAnEntryInEachProjectSyncsAcrossBoth()
    {
        var crdtApi = _fixture.CrdtApi;
        var fwdataApi = _fixture.FwDataApi;
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
        crdtEntries.Should().BeEquivalentTo(fwdataEntries,
            options => options.For(e => e.Components).Exclude(c => c.Id)
                .For(e => e.ComplexForms).Exclude(c => c.Id));
    }
}
