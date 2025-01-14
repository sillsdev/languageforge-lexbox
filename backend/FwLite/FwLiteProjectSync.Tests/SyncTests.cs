using FluentAssertions.Equivalency;
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
        await foreach (var entry in _fixture.FwDataApi.GetAllEntries())
        {
            await _fixture.FwDataApi.DeleteEntry(entry.Id);
        }
        foreach (var entry in await _fixture.CrdtApi.GetAllEntries().ToArrayAsync())
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
    [Trait("Category", "Integration")]
    public async Task FirstSyncJustDoesAnImport()
    {
        var crdtApi = _fixture.CrdtApi;
        var fwdataApi = _fixture.FwDataApi;
        await _syncService.Sync(crdtApi, fwdataApi);

        var crdtEntries = await crdtApi.GetAllEntries().ToArrayAsync();
        var fwdataEntries = await fwdataApi.GetAllEntries().ToArrayAsync();
        crdtEntries.Should().BeEquivalentTo(fwdataEntries,
            options => options
                .For(e => e.Senses).Exclude(s => s.Order)
                .For(e => e.Senses).For(s => s.ExampleSentences).Exclude(s => s.Order)
                .For(e => e.Components).Exclude(c => c.Id)
                .For(e => e.ComplexForms).Exclude(c => c.Id));
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task SecondSyncDoesNothing()
    {
        var crdtApi = _fixture.CrdtApi;
        var fwdataApi = _fixture.FwDataApi;
        await _syncService.Sync(crdtApi, fwdataApi);
        var secondSync = await _syncService.Sync(crdtApi, fwdataApi);
        secondSync.CrdtChanges.Should().Be(0);
        secondSync.FwdataChanges.Should().Be(0);
    }

    [Fact]
    [Trait("Category", "Integration")]
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
        await fixture.Services.GetRequiredService<CurrentProjectService>().RefreshProjectData();

        Func<Task> syncTask = async () => await fixture.SyncService.Sync(crdtApi, fwdataApi);
        await syncTask.Should().ThrowAsync<InvalidOperationException>();
        await fixture.DisposeAsync();
    }

    [Fact]
    [Trait("Category", "Integration")]
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

        var crdtEntries = await crdtApi.GetAllEntries().ToArrayAsync();
        var fwdataEntries = await fwdataApi.GetAllEntries().ToArrayAsync();
        crdtEntries.Should().BeEquivalentTo(fwdataEntries,
            options => options
                .For(e => e.Senses).Exclude(s => s.Order)
                .For(e => e.Senses).For(s => s.ExampleSentences).Exclude(s => s.Order)
                .For(e => e.Components).Exclude(c => c.Id)
                .For(e => e.ComplexForms).Exclude(c => c.Id));
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task SyncDryRun_NoChangesAreSynced()
    {
        var crdtApi = _fixture.CrdtApi;
        var fwdataApi = _fixture.FwDataApi;
        await _syncService.Sync(crdtApi, fwdataApi);
        var fwDataEntryId = Guid.NewGuid();
        var crdtEntryId = Guid.NewGuid();

        await fwdataApi.CreateEntry(new Entry()
        {
            Id = fwDataEntryId,
            LexemeForm = { { "en", "Pear" } },
            Senses =
            [
                new Sense() { Gloss = { { "en", "Pear" } }, }
            ]
        });
        await crdtApi.CreateEntry(new Entry()
        {
            Id = crdtEntryId,
            LexemeForm = { { "en", "Banana" } },
            Senses =
            [
                new Sense() { Gloss = { { "en", "Banana" } }, }
            ]
        });
        await _syncService.SyncDryRun(crdtApi, fwdataApi);

        var crdtEntries = await crdtApi.GetAllEntries().ToArrayAsync();
        var fwdataEntries = await fwdataApi.GetAllEntries().ToArrayAsync();
        crdtEntries.Select(e => e.Id).Should().NotContain(fwDataEntryId);
        fwdataEntries.Select(e => e.Id).Should().NotContain(crdtEntryId);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task CreatingAComplexEntryInFwDataSyncsWithoutIssue()
    {
        var crdtApi = _fixture.CrdtApi;
        var fwdataApi = _fixture.FwDataApi;
        await _syncService.Sync(crdtApi, fwdataApi);

        var hat = await fwdataApi.CreateEntry(new Entry()
        {
            LexemeForm = { { "en", "Hat" } },
            Senses =
            [
                new Sense() { Gloss = { { "en", "Hat" } }, }
            ]
        });
        var stand = await fwdataApi.CreateEntry(new Entry()
        {
            LexemeForm = { { "en", "Stand" } },
            Senses =
            [
                new Sense() { Gloss = { { "en", "Stand" } }, }
            ]
        });
        var hatstand = await fwdataApi.CreateEntry(new Entry()
        {
            LexemeForm = { { "en", "Hatstand" } },
            Senses =
            [
                new Sense() { Gloss = { { "en", "Hatstand" } }, }
            ],
        });
        var component1 = ComplexFormComponent.FromEntries(hatstand, hat);
        var component2 = ComplexFormComponent.FromEntries(hatstand, stand);
        hatstand.Components = [component1, component2];
        await _syncService.Sync(crdtApi, fwdataApi);

        var crdtEntries = await crdtApi.GetAllEntries().ToArrayAsync();
        var fwdataEntries = await fwdataApi.GetAllEntries().ToArrayAsync();
        crdtEntries.Should().BeEquivalentTo(fwdataEntries,
            options => options
                .For(e => e.Senses).Exclude(s => s.Order)
                .For(e => e.Senses).For(s => s.ExampleSentences).Exclude(s => s.Order)
                .For(e => e.Components).Exclude(c => c.Id)
                .For(e => e.ComplexForms).Exclude(c => c.Id));

        // Sync again, ensure no problems or changes
        var secondSync = await _syncService.Sync(crdtApi, fwdataApi);
        secondSync.CrdtChanges.Should().Be(0);
        secondSync.FwdataChanges.Should().Be(0);
    }


    [Fact]
    [Trait("Category", "Integration")]
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
    [Trait("Category", "Integration")]
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

        var verb = new PartOfSpeech()
        {
            Id = new Guid("86ff66f6-0774-407a-a0dc-3eeaf873daf7"),
            Name = { { "en", "verb" } },
            Predefined = true,
        };
        await crdtApi.CreatePartOfSpeech(verb);

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
            LexemeForm = { { "en", "Eat" } },
            Senses =
            [
                new Sense() { Gloss = { { "en", "Eat" } }, PartOfSpeechId = verb.Id }
            ]
        });
        await _syncService.Sync(crdtApi, fwdataApi);

        var crdtEntries = await crdtApi.GetAllEntries().ToArrayAsync();
        var fwdataEntries = await fwdataApi.GetAllEntries().ToArrayAsync();
        crdtEntries.Should().BeEquivalentTo(fwdataEntries,
            options => options
                .For(e => e.Senses).Exclude(s => s.Order)
                .For(e => e.Senses).For(s => s.ExampleSentences).Exclude(s => s.Order)
                .For(e => e.Components).Exclude(c => c.Id)
                .For(e => e.ComplexForms).Exclude(c => c.Id));
    }

    [Fact]
    [Trait("Category", "Integration")]
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
    [Trait("Category", "Integration")]
    public async Task SemanticDomainsSyncInEntries()
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
        // Note we do *not* call crdtApi.CreateSemanticDomain(semdom3);

        await fwdataApi.CreateEntry(new Entry()
        {
            LexemeForm = { { "en", "Pear" } },
            Senses =
            [
                new Sense() { Gloss = { { "en", "Pear" } }, SemanticDomains = [semdom3] }
            ]
        });
        await crdtApi.CreateEntry(new Entry()
        {
            LexemeForm = { { "en", "Banana" } },
            Senses =
            [
                new Sense() { Gloss = { { "en", "Banana" } }, SemanticDomains = [semdom3] }
            ]
        });
        await _syncService.Sync(crdtApi, fwdataApi);

        var crdtEntries = await crdtApi.GetAllEntries().ToArrayAsync();
        var fwdataEntries = await fwdataApi.GetAllEntries().ToArrayAsync();
        crdtEntries.Should().BeEquivalentTo(fwdataEntries,
            options => options
                .For(e => e.Senses).Exclude(s => s.Order)
                .For(e => e.Senses).For(s => s.ExampleSentences).Exclude(s => s.Order)
                .For(e => e.Components).Exclude(c => c.Id)
                .For(e => e.ComplexForms).Exclude(c => c.Id));
    }

    [Fact]
    [Trait("Category", "Integration")]
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

        var crdtEntries = await crdtApi.GetAllEntries().ToArrayAsync();
        var fwdataEntries = await fwdataApi.GetAllEntries().ToArrayAsync();
        crdtEntries.Should().BeEquivalentTo(fwdataEntries,
            options => options
                .For(e => e.Senses).Exclude(s => s.Order)
                .For(e => e.Senses).For(s => s.ExampleSentences).Exclude(s => s.Order)
                .For(e => e.Components).Exclude(c => c.Id)
                //todo the headword should be changed
                .For(e => e.Components).Exclude(c => c.ComponentHeadword)
                .For(e => e.ComplexForms).Exclude(c => c.Id)
                .For(e => e.ComplexForms).Exclude(c => c.ComponentHeadword));
    }

    [Fact]
    [Trait("Category", "Integration")]
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
    [Trait("Category", "Integration")]
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

        var crdtEntries = await crdtApi.GetAllEntries().ToArrayAsync();
        var fwdataEntries = await fwdataApi.GetAllEntries().ToArrayAsync();
        crdtEntries.Should().BeEquivalentTo(fwdataEntries,
            options => options
                .For(e => e.Senses).Exclude(s => s.Order)
                .For(e => e.Senses).For(s => s.ExampleSentences).Exclude(s => s.Order)
                .For(e => e.Components).Exclude(c => c.Id)
                .For(e => e.ComplexForms).Exclude(c => c.Id));
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task CanCreateAComplexFormAndItsComponentInOneSync()
    {
        //ensure they are synced so a real sync will happen when we want it to
        await _fixture.SyncService.Sync(_fixture.CrdtApi, _fixture.FwDataApi);

        var complexFormEntry = await _fixture.CrdtApi.CreateEntry(new() { LexemeForm = { { "en", "complexForm" } } });
        var componentEntry = await _fixture.CrdtApi.CreateEntry(new()
        {
            LexemeForm = { { "en", "component" } },
            ComplexForms =
            [
                new ComplexFormComponent() { ComplexFormEntryId = complexFormEntry.Id, ComponentEntryId = Guid.Empty }
            ]
        });

        //one of the entries will be created first, it will try to create the reference to the other but it won't exist yet
        await _fixture.SyncService.Sync(_fixture.CrdtApi, _fixture.FwDataApi);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task CanCreateAComplexFormTypeAndSyncsIt()
    {
        //ensure they are synced so a real sync will happen when we want it to
        await _fixture.SyncService.Sync(_fixture.CrdtApi, _fixture.FwDataApi);

        var complexFormEntry = await _fixture.CrdtApi.CreateComplexFormType(new() { Name = new() { { "en", "complexFormType" } } });

        //one of the entries will be created first, it will try to create the reference to the other but it won't exist yet
        await _fixture.SyncService.Sync(_fixture.CrdtApi, _fixture.FwDataApi);

        _fixture.FwDataApi.GetComplexFormTypes().ToBlockingEnumerable().Should().ContainEquivalentOf(complexFormEntry);
    }
}
