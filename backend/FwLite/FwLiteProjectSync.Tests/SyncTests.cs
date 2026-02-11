using FluentAssertions.Equivalency;
using FwLiteProjectSync.Tests.Fixtures;
using LcmCrdt;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MiniLcm;
using MiniLcm.Models;

namespace FwLiteProjectSync.Tests;

public class SyncTests : IClassFixture<SyncFixture>, IAsyncLifetime
{
    private readonly SyncFixture _fixture;
    private readonly CrdtFwdataProjectSyncService _syncService;

    private readonly Guid _complexEntryId = Guid.NewGuid();
    private readonly Entry _testEntry = new()
    {
        Id = Guid.NewGuid(),
        LexemeForm = { { "en", "Apple" } },
        Note = { { "en", new RichString("this is a test note") } },
        Senses =
        [
            new Sense
            {
                Gloss = { { "en", "Apple" } },
                Definition = { { "en", new RichString("a round fruit with a hard, crisp skin") } },
                ExampleSentences =
                [
                    new ExampleSentence { Sentence = { { "en", new RichString("I went to the store to buy an apple.") } } }
                ]
            }
        ]
    };

    public async Task InitializeAsync()
    {
        _fixture.DeleteSyncSnapshot();
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
        await foreach (var pos in _fixture.FwDataApi.GetPartsOfSpeech())
        {
            await _fixture.FwDataApi.DeletePartOfSpeech(pos.Id);
        }
        foreach (var pos in await _fixture.CrdtApi.GetPartsOfSpeech().ToArrayAsync())
        {
            await _fixture.CrdtApi.DeletePartOfSpeech(pos.Id);
        }
        _fixture.DeleteSyncSnapshot();
    }

    public SyncTests(SyncFixture fixture)
    {
        _fixture = fixture;
        _syncService = _fixture.SyncService;
    }

    internal static EquivalencyOptions<Entry> SyncExclusions(EquivalencyOptions<Entry> options)
    {
        options = options
            .For(e => e.Senses).Exclude(s => s.Order)
            .For(e => e.Senses).For(s => s.ExampleSentences).Exclude(s => s.Order)
            .For(e => e.Components).Exclude(c => c.Id)
            .For(e => e.Components).Exclude(c => c.Order)
            .For(e => e.ComplexForms).Exclude(c => c.Id)
            .For(e => e.ComplexForms).Exclude(c => c.Order);
        return options;
    }

    internal static void AssertSnapshotsAreEquivalent(ProjectSnapshot expected, ProjectSnapshot actual)
    {
        var excludeOrderTypes = new[] { typeof(Sense), typeof(ExampleSentence), typeof(ComplexFormComponent), typeof(WritingSystem) };
        var excludeIds = new[] { typeof(ComplexFormComponent), typeof(WritingSystem) };
        actual.Should().BeEquivalentTo(expected,
            options =>
                options
                    .WithStrictOrdering()
                    .WithoutStrictOrderingFor(x => x.PartsOfSpeech)
                    .WithoutStrictOrderingFor(x => x.Publications)
                    .WithoutStrictOrderingFor(x => x.SemanticDomains)
                    .WithoutStrictOrderingFor(x => x.ComplexFormTypes)
                    //when excluding properties consider https://github.com/sillsdev/languageforge-lexbox/issues/1912
                    .Using<double>(Exclude)
                    .When(info => info.RuntimeType == typeof(double) && info.Path.EndsWith(".Order") && excludeOrderTypes.Contains(info.ParentType))
                    .Using<Guid>(Exclude)
                    .When(info => info.RuntimeType == typeof(Guid) && info.Path.EndsWith(".Id") && excludeIds.Contains(info.ParentType))
                    .Using<Guid?>(Exclude)
                    .When(info => info.RuntimeType == typeof(Guid?) && info.Path.EndsWith(".MaybeId") && excludeIds.Contains(info.ParentType))

                    //exclude exemplars since they're not updated properly and we don't really support them for now.
                    .Using<string[]>(Exclude)
                    .When(info => info.RuntimeType == typeof(string[]) && info.Path.EndsWith($".{nameof(WritingSystem.Exemplars)}") && info.ParentType == typeof(WritingSystem))
        );

        void Exclude<T>(IAssertionContext<T> context)
        {
            //don't compare
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task FirstSyncJustDoesAnImport()
    {
        var crdtApi = _fixture.CrdtApi;
        var fwdataApi = _fixture.FwDataApi;
        await _syncService.Import(crdtApi, fwdataApi);
        await _fixture.RegenerateAndGetSnapshot();

        AssertSnapshotsAreEquivalent(await fwdataApi.TakeProjectSnapshot(), await crdtApi.TakeProjectSnapshot());
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task SecondSyncDoesNothing()
    {
        var crdtApi = _fixture.CrdtApi;
        var fwdataApi = _fixture.FwDataApi;
        await _syncService.Import(crdtApi, fwdataApi);
        var projectSnapshot = await _fixture.RegenerateAndGetSnapshot();
        var secondSync = await _syncService.Sync(crdtApi, fwdataApi, projectSnapshot);
        secondSync.CrdtChanges.Should().Be(0);
        secondSync.FwdataChanges.Should().Be(0);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task SyncFailsWithMismatchedProjectIds()
    {
        var fixture = SyncFixture.Create();
        await fixture.InitializeAsync();
        var crdtApi = fixture.CrdtApi;
        var fwdataApi = fixture.FwDataApi;
        await fixture.SyncService.Import(crdtApi, fwdataApi);
        var projectSnapshot = await fixture.RegenerateAndGetSnapshot();

        var newFwProjectId = Guid.NewGuid();
        await using var dbContext = await fixture.Services.GetRequiredService<IDbContextFactory<LcmCrdtDbContext>>().CreateDbContextAsync();
        await dbContext.ProjectData.ExecuteUpdateAsync(updates => updates.SetProperty(p => p.FwProjectId, newFwProjectId));
        await fixture.Services.GetRequiredService<CurrentProjectService>().RefreshProjectData();

        Func<Task> syncTask = async () => await fixture.SyncService.Sync(crdtApi, fwdataApi, projectSnapshot);
        await syncTask.Should().ThrowAsync<InvalidOperationException>();
        fixture.DeleteSyncSnapshot();
        await fixture.DisposeAsync();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task CreatingAnEntryInEachProjectSyncsAcrossBoth()
    {
        var crdtApi = _fixture.CrdtApi;
        var fwdataApi = _fixture.FwDataApi;
        await _syncService.Import(crdtApi, fwdataApi);
        var projectSnapshot = await _fixture.RegenerateAndGetSnapshot();

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
        await _syncService.Sync(crdtApi, fwdataApi, projectSnapshot);

        AssertSnapshotsAreEquivalent(await fwdataApi.TakeProjectSnapshot(), await crdtApi.TakeProjectSnapshot());
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task SyncDryRun_NoChangesAreSynced()
    {
        var crdtApi = _fixture.CrdtApi;
        var fwdataApi = _fixture.FwDataApi;
        await _syncService.Import(crdtApi, fwdataApi);
        var projectSnapshot = await _fixture.RegenerateAndGetSnapshot();
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
        await _syncService.SyncDryRun(crdtApi, fwdataApi, projectSnapshot);

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
        await _syncService.Import(crdtApi, fwdataApi);
        var projectSnapshot = await _fixture.RegenerateAndGetSnapshot();

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
        var hatstand = new Entry()
        {
            Id = Guid.NewGuid(),
            LexemeForm = { { "en", "Hatstand" } },
            Senses =
            [
                new Sense() { Gloss = { { "en", "Hatstand" } }, }
            ],
        };
        var component1 = ComplexFormComponent.FromEntries(hatstand, hat);
        var component2 = ComplexFormComponent.FromEntries(hatstand, stand);
        hatstand.Components = [component1, component2];
        await fwdataApi.CreateEntry(hatstand);

        await _syncService.Sync(crdtApi, fwdataApi, projectSnapshot);

        AssertSnapshotsAreEquivalent(await fwdataApi.TakeProjectSnapshot(), await crdtApi.TakeProjectSnapshot());

        // Sync again, ensure no problems or changes
        var secondSnapshot = await _fixture.RegenerateAndGetSnapshot();
        var secondSync = await _syncService.Sync(crdtApi, fwdataApi, secondSnapshot);
        secondSync.CrdtChanges.Should().Be(0);
        secondSync.FwdataChanges.Should().Be(0);
    }


    [Fact]
    [Trait("Category", "Integration")]
    public async Task PartsOfSpeechSyncBothWays()
    {
        var crdtApi = _fixture.CrdtApi;
        var fwdataApi = _fixture.FwDataApi;
        await _syncService.Import(crdtApi, fwdataApi);
        var projectSnapshot = await _fixture.RegenerateAndGetSnapshot();

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

        await _syncService.Sync(crdtApi, fwdataApi, projectSnapshot);

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
        await _syncService.Import(crdtApi, fwdataApi);
        var projectSnapshot = await _fixture.RegenerateAndGetSnapshot();

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
        await _syncService.Sync(crdtApi, fwdataApi, projectSnapshot);

        AssertSnapshotsAreEquivalent(await fwdataApi.TakeProjectSnapshot(), await crdtApi.TakeProjectSnapshot());
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task SemanticDomainsSyncBothWays()
    {
        var crdtApi = _fixture.CrdtApi;
        var fwdataApi = _fixture.FwDataApi;
        await _syncService.Import(crdtApi, fwdataApi);
        var projectSnapshot = await _fixture.RegenerateAndGetSnapshot();

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

        await _syncService.Sync(crdtApi, fwdataApi, projectSnapshot);

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
        await _syncService.Import(crdtApi, fwdataApi);
        var projectSnapshot = await _fixture.RegenerateAndGetSnapshot();

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
        await _syncService.Sync(crdtApi, fwdataApi, projectSnapshot);

        AssertSnapshotsAreEquivalent(await fwdataApi.TakeProjectSnapshot(), await crdtApi.TakeProjectSnapshot());
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task UpdatingAnEntryInEachProjectSyncsAcrossBoth()
    {
        var crdtApi = _fixture.CrdtApi;
        var fwdataApi = _fixture.FwDataApi;
        await fwdataApi.CreateWritingSystem(new WritingSystem() { Id = Guid.NewGuid(), Type = WritingSystemType.Vernacular, WsId = new WritingSystemId("es"), Name = "Spanish", Abbreviation = "es", Font = "Arial" });
        await fwdataApi.CreateWritingSystem(new WritingSystem() { Id = Guid.NewGuid(), Type = WritingSystemType.Vernacular, WsId = new WritingSystemId("fr"), Name = "French", Abbreviation = "fr", Font = "Arial" });
        await _syncService.Import(crdtApi, fwdataApi);
        var projectSnapshot = await _fixture.RegenerateAndGetSnapshot();

        await crdtApi.UpdateEntry(_testEntry.Id, new UpdateObjectInput<Entry>().Set(entry => entry.LexemeForm["es"], "Manzana"));

        await fwdataApi.UpdateEntry(_testEntry.Id, new UpdateObjectInput<Entry>().Set(entry => entry.LexemeForm["fr"], "Pomme"));
        var results = await _syncService.Sync(crdtApi, fwdataApi, projectSnapshot);
        results.CrdtChanges.Should().Be(1);
        results.FwdataChanges.Should().Be(1);

        var crdtEntries = await crdtApi.GetAllEntries().ToArrayAsync();
        var fwdataEntries = await fwdataApi.GetAllEntries().ToArrayAsync();
        crdtEntries.Should().BeEquivalentTo(fwdataEntries,
            options => SyncExclusions(options)
                //todo the headwords should be changed
                .For(e => e.Components).Exclude(c => c.ComponentHeadword)
                .For(e => e.ComplexForms).Exclude(c => c.ComponentHeadword));
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task CanSyncAnyEntryWithDeletedComplexForm()
    {
        var crdtApi = _fixture.CrdtApi;
        var fwdataApi = _fixture.FwDataApi;
        await _syncService.Import(crdtApi, fwdataApi);
        var projectSnapshot = await _fixture.RegenerateAndGetSnapshot();
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
                    Definition = { { "en", new RichString("a citris fruit") } },
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
        await _syncService.Sync(crdtApi, fwdataApi, projectSnapshot);

    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task AddingASenseToAnEntryInEachProjectSyncsAcrossBoth()
    {
        var crdtApi = _fixture.CrdtApi;
        var fwdataApi = _fixture.FwDataApi;
        await _syncService.Import(crdtApi, fwdataApi);
        var projectSnapshot = await _fixture.RegenerateAndGetSnapshot();

        await fwdataApi.CreateSense(_testEntry.Id, new Sense()
            {
            Gloss = { { "en", "Fruit" } },
            Definition = { { "en", new RichString("a round fruit, red or yellow") } },
        });
        await crdtApi.CreateSense(_testEntry.Id, new Sense()
        {
            Gloss = { { "en", "Tree" } },
            Definition = { { "en", new RichString("a tall, woody plant, which grows fruit") } },
            });

        await _syncService.Sync(crdtApi, fwdataApi, projectSnapshot);

        AssertSnapshotsAreEquivalent(await fwdataApi.TakeProjectSnapshot(), await crdtApi.TakeProjectSnapshot());
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task CanCreateAComplexFormAndItsComponentInOneSync()
    {
        //ensure they are synced so a real sync will happen when we want it to
        await _fixture.SyncService.Import(_fixture.CrdtApi, _fixture.FwDataApi);
        var projectSnapshot = await _fixture.RegenerateAndGetSnapshot();

        var complexFormEntry = await _fixture.CrdtApi.CreateEntry(new() { LexemeForm = { { "en", "complexForm" } } });
        await _fixture.CrdtApi.CreateEntry(new()
        {
            LexemeForm = { { "en", "component" } },
            ComplexForms =
            [
                new ComplexFormComponent() { ComplexFormEntryId = complexFormEntry.Id, ComponentEntryId = Guid.Empty }
            ]
        });

        //one of the entries will be created first, it will try to create the reference to the other but it won't exist yet
        await _fixture.SyncService.Sync(_fixture.CrdtApi, _fixture.FwDataApi, projectSnapshot);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task CanCreateAComplexFormTypeAndSyncsIt()
    {
        //ensure they are synced so a real sync will happen when we want it to
        await _fixture.SyncService.Import(_fixture.CrdtApi, _fixture.FwDataApi);
        var projectSnapshot = await _fixture.RegenerateAndGetSnapshot();

        var complexFormEntry = await _fixture.CrdtApi.CreateComplexFormType(new() { Name = new() { { "en", "complexFormType" } } });

        //one of the entries will be created first, it will try to create the reference to the other but it won't exist yet
        await _fixture.SyncService.Sync(_fixture.CrdtApi, _fixture.FwDataApi, projectSnapshot);

        _fixture.FwDataApi.GetComplexFormTypes().ToBlockingEnumerable().Should().ContainEquivalentOf(complexFormEntry);
    }
}
