using FwLiteProjectSync.Tests.Fixtures;
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
        foreach (var entry in await _fixture.CrdtApi.GetEntries().ToArrayAsync())
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
    public static async Task SyncFailsWithMismatchedProjectIds()
    {
        var fixture = SyncFixture.Create();
        await fixture.InitializeAsync();
        var crdtApi = fixture.CrdtApi;
        var fwdataApi = fixture.FwDataApi;
        await fixture.SyncService.Sync(crdtApi, fwdataApi);

        var newFwProjectId = Guid.NewGuid();
        await fixture.Services.GetRequiredService<LcmCrdtDbContext>().ProjectData.
            ExecuteUpdateAsync(updates => updates.SetProperty(p => p.FwProjectId, newFwProjectId));
        await fixture.Services.GetRequiredService<CurrentProjectService>().PopulateProjectDataCache(force: true);

        Func<Task> syncTask = async () => await fixture.SyncService.Sync(crdtApi, fwdataApi);
        await syncTask.Should().ThrowAsync<InvalidOperationException>();
        await fixture.DisposeAsync();
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
    public async Task PartsOfSpeechSyncBothWays()
    {
        var crdtApi = _fixture.CrdtApi;
        var fwdataApi = _fixture.FwDataApi;
        await _syncService.Sync(crdtApi, fwdataApi);

        var noun = new PartOfSpeech()
        {
            Id = new Guid("a8e41fd3-e343-4c7c-aa05-01ea3dd5cfb5"),
            Name = { { "en", "noun" } },
            Predefined = true,
        };
        await fwdataApi.CreatePartOfSpeech(noun);

        var verb = new PartOfSpeech()
        {
            Id = new Guid("86ff66f6-0774-407a-a0dc-3eeaf873daf7"),
            Name = { { "en", "verb" } },
            Predefined = true,
        };
        await crdtApi.CreatePartOfSpeech(verb);

        await _syncService.Sync(crdtApi, fwdataApi);

        var crdtPartsOfSpeech = await crdtApi.GetPartsOfSpeech().ToArrayAsync();
        var fwdataPartsOfSpeech = await fwdataApi.GetPartsOfSpeech().ToArrayAsync();
        crdtPartsOfSpeech.Should().ContainEquivalentOf(noun);
        crdtPartsOfSpeech.Should().ContainEquivalentOf(verb);
        fwdataPartsOfSpeech.Should().ContainEquivalentOf(noun);
        fwdataPartsOfSpeech.Should().ContainEquivalentOf(verb);

        crdtPartsOfSpeech.Should().BeEquivalentTo(fwdataPartsOfSpeech);
    }

    [Fact]
    public async Task PartsOfSpeechSyncInEntries()
    {
        var crdtApi = _fixture.CrdtApi;
        var fwdataApi = _fixture.FwDataApi;
        await _syncService.Sync(crdtApi, fwdataApi);

        var noun = new PartOfSpeech()
        {
            Id = new Guid("a8e41fd3-e343-4c7c-aa05-01ea3dd5cfb5"),
            Name = { { "en", "noun" } },
            Predefined = true,
        };
        await fwdataApi.CreatePartOfSpeech(noun);
        // Note we do *not* call crdtApi.CreatePartOfSpeech(noun);

        await fwdataApi.CreateEntry(new Entry()
        {
            LexemeForm = { { "en", "Pear" } },
            Senses =
            [
                new Sense() { Gloss = { { "en", "Pear" } }, PartOfSpeechId = noun.Id }
            ]
        });
        await crdtApi.CreateEntry(new Entry()
        {
            LexemeForm = { { "en", "Banana" } },
            Senses =
            [
                new Sense() { Gloss = { { "en", "Banana" } }, PartOfSpeechId = noun.Id }
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
    public async Task SemanticDomainsSyncBothWays()
    {
        var crdtApi = _fixture.CrdtApi;
        var fwdataApi = _fixture.FwDataApi;
        await _syncService.Sync(crdtApi, fwdataApi);

        var semdom3 = new SemanticDomain()
        {
            Id = new Guid("f4491f9b-3c5e-42ab-afc0-f22e19d0fff5"),
            Name = new MultiString() { { "en", "Language and thought" } },
            Code = "3",
            Predefined = true,
        };
        await fwdataApi.CreateSemanticDomain(semdom3);

        var semdom4 = new SemanticDomain()
        {
            Id = new Guid("62b4ae33-f3c2-447a-9ef7-7e41805b6a02"),
            Name = new MultiString() { { "en", "Social behavior" } },
            Code = "4",
            Predefined = true,
        };
        await crdtApi.CreateSemanticDomain(semdom4);

        await _syncService.Sync(crdtApi, fwdataApi);

        var crdtSemanticDomains = await crdtApi.GetSemanticDomains().ToArrayAsync();
        var fwdataSemanticDomains = await fwdataApi.GetSemanticDomains().ToArrayAsync();
        crdtSemanticDomains.Should().ContainEquivalentOf(semdom3);
        crdtSemanticDomains.Should().ContainEquivalentOf(semdom4);
        fwdataSemanticDomains.Should().ContainEquivalentOf(semdom3);
        fwdataSemanticDomains.Should().ContainEquivalentOf(semdom4);

        crdtSemanticDomains.Should().BeEquivalentTo(fwdataSemanticDomains);
    }

    [Fact]
    public async Task UpdatingAnEntryInEachProjectSyncsAcrossBoth()
    {
        var crdtApi = _fixture.CrdtApi;
        var fwdataApi = _fixture.FwDataApi;
        await fwdataApi.CreateWritingSystem(WritingSystemType.Vernacular, new WritingSystem() { Id = Guid.NewGuid(), Type = WritingSystemType.Vernacular, WsId = new WritingSystemId("es"), Name = "Spanish", Abbreviation = "es", Font = "Arial" });
        await fwdataApi.CreateWritingSystem(WritingSystemType.Vernacular, new WritingSystem() { Id = Guid.NewGuid(), Type = WritingSystemType.Vernacular, WsId = new WritingSystemId("fr"), Name = "French", Abbreviation = "fr", Font = "Arial" });
        await _syncService.Sync(crdtApi, fwdataApi);

        await crdtApi.UpdateEntry(_testEntry.Id, new UpdateObjectInput<Entry>().Set(entry => entry.LexemeForm["es"], "Manzana"));

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
    public async Task CanSyncAnyEntryWithDeletedComplexForm()
    {
        var crdtApi = _fixture.CrdtApi;
        var fwdataApi = _fixture.FwDataApi;
        await _syncService.Sync(crdtApi, fwdataApi);
        await crdtApi.DeleteEntry(_testEntry.Id);
        var newEntryId = Guid.NewGuid();
        await fwdataApi.CreateEntry(new Entry()
        {
            Id = newEntryId,
            LexemeForm = { { "en", "pineapple" } },
            Senses =
            [
                new Sense
                {
                    Gloss = { { "en", "fruit" } },
                    Definition = { { "en", "a citris fruit" } },
                }
            ],
            Components =
            [
                new ComplexFormComponent()
                {
                    ComponentEntryId = _testEntry.Id,
                    ComponentHeadword = "apple",
                    ComplexFormEntryId = newEntryId,
                    ComplexFormHeadword = "pineapple"
                }
            ]
        });

        //sync may fail because it will try to create a complex form for an entry which was deleted
        await _syncService.Sync(crdtApi, fwdataApi);

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
