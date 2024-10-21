using LcmCrdt.Changes;
using LcmCrdt.Tests.Mocks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using MiniLcm;
using Entry = MiniLcm.Models.Entry;

namespace LcmCrdt.Tests;

public class BasicApiTests : IAsyncLifetime
{
    private CrdtMiniLcmApi _api = null!;
    private Guid _entry1Id = new Guid("a3f5aa5a-578f-4181-8f38-eaaf27f01f1c");
    private Guid _entry2Id = new Guid("2de6c334-58fa-4844-b0fd-0bc2ce4ef835");

    protected readonly AsyncServiceScope _services;
    public DataModel DataModel = null!;
    private readonly LcmCrdtDbContext _crdtDbContext;

    public BasicApiTests()
    {
        var services = new ServiceCollection()
            .AddLcmCrdtClient()
            .AddLogging(builder => builder.AddDebug())
            .RemoveAll(typeof(ProjectContext))
            .AddSingleton<ProjectContext>(new MockProjectContext(new CrdtProject("sena-3", ":memory:")))
            .BuildServiceProvider();
        _services = services.CreateAsyncScope();
        _crdtDbContext = _services.ServiceProvider.GetRequiredService<LcmCrdtDbContext>();
    }

    public virtual async Task InitializeAsync()
    {
        await _crdtDbContext.Database.OpenConnectionAsync();
        //can't use ProjectsService.CreateProject because it opens and closes the db context, this would wipe out the in memory db.
        await ProjectsService.InitProjectDb(_crdtDbContext, new ProjectData("Sena 3", Guid.NewGuid(), null, Guid.NewGuid()));
        await _services.ServiceProvider.GetRequiredService<CurrentProjectService>().PopulateProjectDataCache();
        DataModel = _services.ServiceProvider.GetRequiredService<DataModel>();
        _api = ActivatorUtilities.CreateInstance<CrdtMiniLcmApi>(_services.ServiceProvider);
        await _api.CreateWritingSystem(WritingSystemType.Analysis,
            new WritingSystem()
            {
                Id = Guid.NewGuid(),
                Type = WritingSystemType.Analysis,
                WsId = "en",
                Name = "English",
                Abbreviation = "En",
                Font = "Arial",
                Exemplars = []
            });
        await _api.CreateWritingSystem(WritingSystemType.Vernacular,
            new WritingSystem()
            {
                Id = Guid.NewGuid(),
                Type = WritingSystemType.Vernacular,
                WsId = "en",
                Name = "English",
                Abbreviation = "En",
                Font = "Arial",
                Exemplars = []
            });
        await _api.CreateEntry(new Entry
        {
            Id = _entry1Id,
            LexemeForm =
            {
                Values =
                {
                    { "en", "Kevin" }
                }
            },
            Note =
            {
                Values =
                {
                    { "en", "this is a test note from Kevin" }
                }
            },
            CitationForm =
            {
                Values =
                {
                    { "en", "Kevin" }
                }
            },
            LiteralMeaning =
            {
                Values =
                {
                    { "en", "Kevin" }
                }
            },
            Senses =
            [
                new Sense
                {
                    Gloss =
                    {
                        Values =
                        {
                            { "en", "Kevin" }
                        }
                    },
                    Definition =
                    {
                        Values =
                        {
                            { "en", "Kevin" }
                        }
                    },
                    ExampleSentences =
                    [
                        new ExampleSentence
                        {
                            Sentence =
                            {
                                Values =
                                {
                                    { "en", "Kevin is a good guy" }
                                }
                            }
                        }
                    ]
                }
            ]
        });
        await _api.CreateEntry(new()
        {
            Id = _entry2Id,
            LexemeForm =
            {
                Values = { { "en", "apple" } }
            },
            Senses =
            [
                new Sense
                {
                    Gloss =
                    {
                        Values =
                        {
                            { "en", "fruit" }
                        }
                    },
                    Definition =
                    {
                        Values =
                        {
                            { "en", "a round fruit, red or yellow" }
                        }
                    },
                }
            ],
        });
        _crdtDbContext.ChangeTracker.Clear();
    }

    public async Task DisposeAsync()
    {
        await _services.DisposeAsync();
    }

    [Fact]
    public async Task GetWritingSystems()
    {
        var writingSystems = await _api.GetWritingSystems();
        writingSystems.Analysis.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CreatingMultipleWritingSystems_DoesNotHaveDuplicateOrders()
    {
        await _api.CreateWritingSystem(WritingSystemType.Vernacular, new WritingSystem() { Id = Guid.NewGuid(), Type = WritingSystemType.Vernacular, WsId = "test-2", Name = "test", Abbreviation = "test", Font = "Arial", Exemplars = new[] { "test" } });
        var writingSystems = await DataModel.GetLatestObjects<WritingSystem>().Where(ws => ws.Type == WritingSystemType.Vernacular).ToArrayAsync();
        writingSystems.GroupBy(ws => ws.Order).Should().NotContain(g => g.Count() > 1);
    }

    [Fact]
    public async Task GetEntriesByExemplar()
    {
        var entries = await _api.GetEntries(new QueryOptions(SortOptions.Default, new("a", "default"))).ToArrayAsync();
        entries.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetEntriesWithOptions()
    {
        var entries = await _api.GetEntries(QueryOptions.Default).ToArrayAsync();
        entries.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetEntries()
    {
        var entries = await _api.GetEntries().ToArrayAsync();
        entries.Should().NotBeEmpty();
        var entry1 = entries.First(e => e.Id == _entry1Id);
        entry1.LexemeForm.Values.Should().NotBeEmpty();
        var sense1 = entry1.Senses.Should().NotBeEmpty().And.Subject.First();
        sense1.ExampleSentences.Should().NotBeEmpty();

        var entry2 = entries.First(e => e.Id == _entry2Id);
        entry2.LexemeForm.Values.Should().NotBeEmpty();
        entry2.Senses.Should().NotBeEmpty();
    }

    [Fact]
    public async Task SearchEntries()
    {
        var entries = await _api.SearchEntries("a").ToArrayAsync();
        entries.Should().NotBeEmpty();
        entries.Should().NotContain(e => e.Id == default);
        entries.Should().NotContain(e => e.LexemeForm.Values["en"] == "Kevin");
    }
    [Fact]
    public async Task SearchEntries_MatchesGloss()
    {
        var entries = await _api.SearchEntries("fruit").ToArrayAsync();
        entries.Should().NotBeEmpty();
        entries.Should().NotContain(e => e.Id == default);
        entries.Should().Contain(e => e.LexemeForm.Values["en"] == "apple");
    }

    [Fact]
    public async Task GetEntry()
    {
        var entry = await _api.GetEntry(_entry1Id);
        entry.Should().NotBeNull();
        entry!.LexemeForm.Values.Should().NotBeEmpty();
        var sense = entry.Senses.Should()
            .NotBeEmpty($"because '{entry.LexemeForm.Values.First().Value}' should have a sense").And.Subject.First();
        sense.Gloss.Values.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CreateEntry()
    {
        var entry = await _api.CreateEntry(new Entry
        {
            LexemeForm =
            {
                Values =
                {
                    { "en", "Kevin" }
                }
            },
            Note =
            {
                Values =
                {
                    { "en", "this is a test note from Kevin" }
                }
            },
            CitationForm =
            {
                Values =
                {
                    { "en", "Kevin" }
                }
            },
            LiteralMeaning =
            {
                Values =
                {
                    { "en", "Kevin" }
                }
            },
            Senses =
            [
                new Sense
                {
                    Gloss =
                    {
                        Values =
                        {
                            { "en", "Kevin" }
                        }
                    },
                    Definition =
                    {
                        Values =
                        {
                            { "en", "Kevin" }
                        }
                    },
                    ExampleSentences =
                    [
                        new ExampleSentence
                        {
                            Sentence =
                            {
                                Values =
                                {
                                    { "en", "Kevin is a good guy" }
                                }
                            }
                        }
                    ]
                }
            ]
        });
        entry.Should().NotBeNull();
        entry.LexemeForm.Values["en"].Should().Be("Kevin");
        entry.LiteralMeaning.Values["en"].Should().Be("Kevin");
        entry.CitationForm.Values["en"].Should().Be("Kevin");
        entry.Note.Values["en"].Should().Be("this is a test note from Kevin");
        var sense = entry.Senses.Should().ContainSingle().Subject;
        sense.Gloss.Values["en"].Should().Be("Kevin");
        sense.Definition.Values["en"].Should().Be("Kevin");
        var example = sense.ExampleSentences.Should().ContainSingle().Subject;
        example.Sentence.Values["en"].Should().Be("Kevin is a good guy");
    }

    [Fact]
    public async Task UpdateEntry()
    {
        var updatedEntry = await _api.UpdateEntry(_entry1Id,
            new UpdateObjectInput<Entry>()
                .Set(e => e.LexemeForm["en"], "updated"));
        updatedEntry.LexemeForm.Values["en"].Should().Be("updated");
    }

    [Fact]
    public async Task UpdateEntryNote()
    {
        var entry = await _api.CreateEntry(new Entry
        {
            LexemeForm = new MultiString
            {
                Values =
                {
                    { "en", "test" }
                }
            }
        });
        var updatedEntry = await _api.UpdateEntry(entry.Id,
            new UpdateObjectInput<Entry>()
                .Set(e => e.Note["en"], "updated"));
        updatedEntry.Note.Values["en"].Should().Be("updated");
    }

    [Fact]
    public async Task UpdateSense()
    {
        var entry = await _api.CreateEntry(new Entry
        {
            LexemeForm = new MultiString
            {
                Values =
                {
                    { "en", "test" }
                }
            },
            Senses = new List<Sense>
            {
                new Sense()
                {
                    Definition = new MultiString
                    {
                        Values =
                        {
                            { "en", "test" }
                        }
                    }
                }
            }
        });
        var updatedSense = await _api.UpdateSense(entry.Id,
            entry.Senses[0].Id,
            new UpdateObjectInput<Sense>()
                .Set(e => e.Definition["en"], "updated"));
        updatedSense.Definition.Values["en"].Should().Be("updated");
    }

    [Fact]
    public async Task CreateSense_WontCreateMissingDomains()
    {
        var senseId = Guid.NewGuid();
        var createdSense = await _api.CreateSense(_entry1Id, new Sense()
        {
            Id = senseId,
            SemanticDomains = [new SemanticDomain() { Id = Guid.NewGuid(), Code = "test", Name = new MultiString() }],
        });
        createdSense.Id.Should().Be(senseId);
        createdSense.SemanticDomains.Should().BeEmpty("because the domain does not exist (or was deleted)");
    }


    [Fact]
    public async Task CreateSense_WillCreateWithExistingDomains()
    {
        var senseId = Guid.NewGuid();
        var semanticDomainId = Guid.NewGuid();
        await DataModel.AddChange(Guid.NewGuid(), new CreateSemanticDomainChange(semanticDomainId, new MultiString() { { "en", "test" } }, "test"));
        var semanticDomain = await DataModel.GetLatest<SemanticDomain>(semanticDomainId);
        ArgumentNullException.ThrowIfNull(semanticDomain);
        var createdSense = await _api.CreateSense(_entry1Id, new Sense()
        {
            Id = senseId,
            SemanticDomains = [semanticDomain],
        });
        createdSense.Id.Should().Be(senseId);
        createdSense.SemanticDomains.Should().ContainSingle(s => s.Id == semanticDomainId);
    }

    [Fact]
    public async Task CreateSense_WontCreateMissingPartOfSpeech()
    {
        var senseId = Guid.NewGuid();
        var createdSense = await _api.CreateSense(_entry1Id,
            new Sense() { Id = senseId, PartOfSpeech = "test", PartOfSpeechId = Guid.NewGuid(), });
        createdSense.Id.Should().Be(senseId);
        createdSense.PartOfSpeechId.Should().BeNull("because the part of speech does not exist (or was deleted)");
    }

    [Fact]
    public async Task CreateSense_WillCreateWthExistingPartOfSpeech()
    {
        var senseId = Guid.NewGuid();
        var partOfSpeechId = Guid.NewGuid();
        await DataModel.AddChange(Guid.NewGuid(), new CreatePartOfSpeechChange(partOfSpeechId, new MultiString() { { "en", "test" } }));
        var partOfSpeech = await DataModel.GetLatest<PartOfSpeech>(partOfSpeechId);
        ArgumentNullException.ThrowIfNull(partOfSpeech);
        var createdSense = await _api.CreateSense(_entry1Id,
            new Sense() { Id = senseId, PartOfSpeech = "test", PartOfSpeechId = partOfSpeechId, });
        createdSense.Id.Should().Be(senseId);
        createdSense.PartOfSpeechId.Should().Be(partOfSpeechId, "because the part of speech does  exist");
    }

    [Fact]
    public async Task UpdateSensePartOfSpeech()
    {
        var partOfSpeechId = Guid.NewGuid();
        await DataModel.AddChange(Guid.NewGuid(), new CreatePartOfSpeechChange(partOfSpeechId, new MultiString() { { "en", "Adverb" } }));
        var entry = await _api.CreateEntry(new Entry
        {
            LexemeForm = new MultiString
            {
                Values =
                {
                    { "en", "test" }
                }
            },
            Senses = new List<Sense>
            {
                new Sense()
                {
                    PartOfSpeech = "test",
                    Definition = new MultiString
                    {
                        Values =
                        {
                            { "en", "test" }
                        }
                    }
                }
            }
        });
        var updatedSense = await _api.UpdateSense(entry.Id,
            entry.Senses[0].Id,
            new UpdateObjectInput<Sense>()
                .Set(e => e.PartOfSpeech, "updated")
                .Set(e => e.PartOfSpeechId, partOfSpeechId));
        updatedSense.PartOfSpeech.Should().Be("updated");
        updatedSense.PartOfSpeechId.Should().Be(partOfSpeechId);
    }

    [Fact]
    public async Task UpdateSenseSemanticDomain()
    {
        var newDomainId = Guid.NewGuid();
        await DataModel.AddChange(Guid.NewGuid(), new CreateSemanticDomainChange(newDomainId, new MultiString() { { "en", "test" } }, "updated"));
        var newSemanticDomain = await DataModel.GetLatest<SemanticDomain>(newDomainId);
        ArgumentNullException.ThrowIfNull(newSemanticDomain);
        var entry = await _api.CreateEntry(new Entry
        {
            LexemeForm = new MultiString
            {
                Values =
                {
                    { "en", "test" }
                }
            },
            Senses = new List<Sense>
            {
                new Sense()
                {
                    SemanticDomains = [new SemanticDomain() { Id = Guid.Empty, Code = "test", Name = new MultiString() }],
                    Definition = new MultiString
                    {
                        Values =
                        {
                            { "en", "test" }
                        }
                    }
                }
            }
        });
        var updatedSense = await _api.UpdateSense(entry.Id,
            entry.Senses[0].Id,
            new UpdateObjectInput<Sense>()
                .Add(e => e.SemanticDomains, newSemanticDomain));
        var semanticDomain = updatedSense.SemanticDomains.Should().ContainSingle(s => s.Id == newDomainId).Subject;
        semanticDomain.Code.Should().Be("updated");
        semanticDomain.Id.Should().Be(newDomainId);
    }

    [Fact]
    public async Task RemoveSenseSemanticDomain()
    {
        var newDomainId = Guid.NewGuid();
        await DataModel.AddChange(Guid.NewGuid(), new CreateSemanticDomainChange(newDomainId, new MultiString() { { "en", "test" } }, "updated"));
        var newSemanticDomain = await DataModel.GetLatest<SemanticDomain>(newDomainId);
        ArgumentNullException.ThrowIfNull(newSemanticDomain);
        var entry = await _api.CreateEntry(new Entry
        {
            LexemeForm = new MultiString
            {
                Values =
                {
                    { "en", "test" }
                }
            },
            Senses = new List<Sense>
            {
                new Sense()
                {
                    SemanticDomains = [newSemanticDomain],
                    Definition = new MultiString
                    {
                        Values =
                        {
                            { "en", "test" }
                        }
                    }
                }
            }
        });
        var updatedSense = await _api.UpdateSense(entry.Id,
            entry.Senses[0].Id,
            new UpdateObjectInput<Sense>()
                .Remove(e => e.SemanticDomains, 0));
        updatedSense.SemanticDomains.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateExampleSentence()
    {
        var entry = await _api.CreateEntry(new Entry
        {
            LexemeForm = new MultiString
            {
                Values =
                {
                    { "en", "test" }
                }
            },
            Senses = new List<Sense>
            {
                new Sense()
                {
                    Definition = new MultiString
                    {
                        Values =
                        {
                            { "en", "test" }
                        }
                    },
                    ExampleSentences = new List<ExampleSentence>
                    {
                        new ExampleSentence()
                        {
                            Sentence = new MultiString
                            {
                                Values =
                                {
                                    { "en", "test" }
                                }
                            }
                        }
                    }
                }
            }
        });
        entry.Senses.Should().ContainSingle().Which.ExampleSentences.Should().ContainSingle();
        var updatedExample = await _api.UpdateExampleSentence(entry.Id,
            entry.Senses[0].Id,
            entry.Senses[0].ExampleSentences[0].Id,
            new UpdateObjectInput<ExampleSentence>()
                .Set(e => e.Sentence["en"], "updated"));
        updatedExample.Sentence.Values["en"].Should().Be("updated");
    }

    [Fact]
    public async Task UpdateExampleSentenceTranslation()
    {
        var entry = await _api.CreateEntry(new Entry
        {
            LexemeForm = new MultiString
            {
                Values =
                {
                    { "en", "test" }
                }
            },
            Senses = new List<Sense>
            {
                new Sense()
                {
                    Definition = new MultiString
                    {
                        Values =
                        {
                            { "en", "test" }
                        }
                    },
                    ExampleSentences = new List<ExampleSentence>
                    {
                        new ExampleSentence()
                        {
                            Sentence = new MultiString
                            {
                                Values =
                                {
                                    { "en", "test" }
                                }
                            },
                            Translation =
                            {
                                Values =
                                {
                                    { "en", "test" }
                                }
                            }
                        }
                    }
                }
            }
        });
        entry.Senses.Should().ContainSingle().Which.ExampleSentences.Should().ContainSingle();
        var updatedExample = await _api.UpdateExampleSentence(entry.Id,
            entry.Senses[0].Id,
            entry.Senses[0].ExampleSentences[0].Id,
            new UpdateObjectInput<ExampleSentence>()
                .Set(e => e.Translation["en"], "updated"));
        updatedExample.Translation.Values["en"].Should().Be("updated");
    }

    [Fact]
    public async Task DeleteEntry()
    {
        var entry = await _api.CreateEntry(new Entry
        {
            LexemeForm = new MultiString
            {
                Values =
                {
                    { "en", "test" }
                }
            }
        });
        await _api.DeleteEntry(entry.Id);

        var entries = await _api.GetEntries().ToArrayAsync();
        entries.Should().NotContain(e => e.Id == entry.Id);
    }
}
