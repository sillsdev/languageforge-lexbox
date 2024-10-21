using MiniLcm.Models;

namespace MiniLcm.Tests;

public abstract class BasicApiTestsBase : MiniLcmTestBase
{
    private readonly Guid _entry1Id = new Guid("a3f5aa5a-578f-4181-8f38-eaaf27f01f1c");
    private readonly Guid _entry2Id = new Guid("2de6c334-58fa-4844-b0fd-0bc2ce4ef835");

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await Api.CreateWritingSystem(WritingSystemType.Analysis,
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
        await Api.CreateWritingSystem(WritingSystemType.Vernacular,
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
        await Api.CreateEntry(new Entry
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
        await Api.CreateEntry(new()
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
    }


    [Fact]
    public async Task GetWritingSystems()
    {
        var writingSystems = await Api.GetWritingSystems();
        writingSystems.Analysis.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CreatingMultipleWritingSystems_DoesNotHaveDuplicateOrders()
    {
        await Api.CreateWritingSystem(WritingSystemType.Vernacular,
            new WritingSystem()
            {
                Id = Guid.NewGuid(),
                Type = WritingSystemType.Vernacular,
                WsId = "en",
                Name = "test",
                Abbreviation = "test",
                Font = "Arial",
                Exemplars = ["test"]
            });
        var writingSystems = (await Api.GetWritingSystems()).Vernacular;
        writingSystems.GroupBy(ws => ws.Order).Should().NotContain(g => g.Count() > 1);
    }

    [Fact]
    public async Task GetEntriesByExemplar()
    {
        var entries = await Api.GetEntries(new QueryOptions(SortOptions.Default, new("a", "default"))).ToArrayAsync();
        entries.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetEntriesWithOptions()
    {
        var entries = await Api.GetEntries(QueryOptions.Default).ToArrayAsync();
        entries.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetEntries()
    {
        var entries = await Api.GetEntries().ToArrayAsync();
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
        var entries = await Api.SearchEntries("a").ToArrayAsync();
        entries.Should().NotBeEmpty();
        entries.Should().NotContain(e => e.Id == default);
        entries.Should().NotContain(e => e.LexemeForm.Values["en"] == "Kevin");
    }
    [Fact]
    public async Task SearchEntries_MatchesGloss()
    {
        var entries = await Api.SearchEntries("fruit").ToArrayAsync();
        entries.Should().NotBeEmpty();
        entries.Should().NotContain(e => e.Id == default);
        entries.Should().Contain(e => e.LexemeForm.Values["en"] == "apple");
    }

    [Fact]
    public async Task GetEntry()
    {
        var entry = await Api.GetEntry(_entry1Id);
        entry.Should().NotBeNull();
        entry!.LexemeForm.Values.Should().NotBeEmpty();
        var sense = entry.Senses.Should()
            .NotBeEmpty($"because '{entry.LexemeForm.Values.First().Value}' should have a sense").And.Subject.First();
        sense.Gloss.Values.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CreateEntry()
    {
        var entry = await Api.CreateEntry(new Entry
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
        var updatedEntry = await Api.UpdateEntry(_entry1Id,
            new UpdateObjectInput<Entry>()
                .Set(e => e.LexemeForm["en"], "updated"));
        updatedEntry.LexemeForm.Values["en"].Should().Be("updated");
    }

    [Fact]
    public async Task UpdateEntryNote()
    {
        var entry = await Api.CreateEntry(new Entry
        {
            LexemeForm = new MultiString
            {
                Values =
                {
                    { "en", "test" }
                }
            }
        });
        var updatedEntry = await Api.UpdateEntry(entry.Id,
            new UpdateObjectInput<Entry>()
                .Set(e => e.Note["en"], "updated"));
        updatedEntry.Note.Values["en"].Should().Be("updated");
    }

    [Fact]
    public async Task UpdateSense()
    {
        var entry = await Api.CreateEntry(new Entry
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
        var updatedSense = await Api.UpdateSense(entry.Id,
            entry.Senses[0].Id,
            new UpdateObjectInput<Sense>()
                .Set(e => e.Definition["en"], "updated"));
        updatedSense.Definition.Values["en"].Should().Be("updated");
    }

    [Fact]
    public async Task CreateSense_WontCreateMissingDomains()
    {
        var senseId = Guid.NewGuid();
        var createdSense = await Api.CreateSense(_entry1Id, new Sense()
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
        await Api.CreateSemanticDomain(new SemanticDomain() { Id = semanticDomainId, Code = "test", Name = new MultiString() { { "en", "test" } } });
        var semanticDomain = await Api.GetSemanticDomains().SingleOrDefaultAsync(sd => sd.Id == semanticDomainId);
        ArgumentNullException.ThrowIfNull(semanticDomain);
        var createdSense = await Api.CreateSense(_entry1Id, new Sense()
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
        var createdSense = await Api.CreateSense(_entry1Id,
            new Sense() { Id = senseId, PartOfSpeech = "test", PartOfSpeechId = Guid.NewGuid(), });
        createdSense.Id.Should().Be(senseId);
        createdSense.PartOfSpeechId.Should().BeNull("because the part of speech does not exist (or was deleted)");
    }

    [Fact]
    public async Task CreateSense_WillCreateWthExistingPartOfSpeech()
    {
        var senseId = Guid.NewGuid();
        var partOfSpeechId = Guid.NewGuid();
        await Api.CreatePartOfSpeech(new PartOfSpeech() { Id = partOfSpeechId, Name = new MultiString() { { "en", "test" } } });
        var partOfSpeech = await Api.GetPartsOfSpeech().SingleOrDefaultAsync(pos => pos.Id == partOfSpeechId);
        ArgumentNullException.ThrowIfNull(partOfSpeech);
        var createdSense = await Api.CreateSense(_entry1Id,
            new Sense() { Id = senseId, PartOfSpeech = "test", PartOfSpeechId = partOfSpeechId, });
        createdSense.Id.Should().Be(senseId);
        createdSense.PartOfSpeechId.Should().Be(partOfSpeechId, "because the part of speech does exist");
    }

    [Fact]
    public async Task UpdateSensePartOfSpeech()
    {
        var partOfSpeechId = Guid.NewGuid();
        await Api.CreatePartOfSpeech(new PartOfSpeech() { Id = partOfSpeechId, Name = new MultiString() { { "en", "Adverb" } } });
        var entry = await Api.CreateEntry(new Entry
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
        var updatedSense = await Api.UpdateSense(entry.Id,
            entry.Senses[0].Id,
            new UpdateObjectInput<Sense>()
                .Set(e => e.PartOfSpeech, "updated")//should be ignored
                .Set(e => e.PartOfSpeechId, partOfSpeechId));
        updatedSense.PartOfSpeech.Should().Be("Adverb");
        updatedSense.PartOfSpeechId.Should().Be(partOfSpeechId);
    }

    [Fact]
    public async Task UpdateSenseSemanticDomain()
    {
        var newDomainId = Guid.NewGuid();
        await Api.CreateSemanticDomain(new SemanticDomain() { Id = newDomainId, Code = "updated", Name = new MultiString() { { "en", "test" } } });
        var newSemanticDomain = await Api.GetSemanticDomains().SingleOrDefaultAsync(sd => sd.Id == newDomainId);
        ArgumentNullException.ThrowIfNull(newSemanticDomain);
        var entry = await Api.CreateEntry(new Entry
        {
            LexemeForm = new MultiString
            {
                Values = { { "en", "test" } }
            },
            Senses =
            [
                new Sense()
                {
                    SemanticDomains =
                        [new SemanticDomain() { Id = Guid.Empty, Code = "test", Name = new MultiString() }],
                    Definition = new MultiString { Values = { { "en", "test" } } }
                }
            ]
        });
        var updatedSense = await Api.UpdateSense(entry.Id,
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
        await Api.CreateSemanticDomain(new SemanticDomain() { Id = newDomainId, Code = "updated", Name = new MultiString() { { "en", "test" } } });
        var newSemanticDomain = await Api.GetSemanticDomains().SingleOrDefaultAsync(sd => sd.Id == newDomainId);
        ArgumentNullException.ThrowIfNull(newSemanticDomain);
        var entry = await Api.CreateEntry(new Entry
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
        var updatedSense = await Api.UpdateSense(entry.Id,
            entry.Senses[0].Id,
            new UpdateObjectInput<Sense>()
                .Remove(e => e.SemanticDomains, 0));
        updatedSense.SemanticDomains.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateExampleSentence()
    {
        var entry = await Api.CreateEntry(new Entry
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
        var updatedExample = await Api.UpdateExampleSentence(entry.Id,
            entry.Senses[0].Id,
            entry.Senses[0].ExampleSentences[0].Id,
            new UpdateObjectInput<ExampleSentence>()
                .Set(e => e.Sentence["en"], "updated"));
        updatedExample.Sentence.Values["en"].Should().Be("updated");
    }

    [Fact]
    public async Task UpdateExampleSentenceTranslation()
    {
        var entry = await Api.CreateEntry(new Entry
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
        var updatedExample = await Api.UpdateExampleSentence(entry.Id,
            entry.Senses[0].Id,
            entry.Senses[0].ExampleSentences[0].Id,
            new UpdateObjectInput<ExampleSentence>()
                .Set(e => e.Translation["en"], "updated"));
        updatedExample.Translation.Values["en"].Should().Be("updated");
    }

    [Fact]
    public async Task DeleteEntry()
    {
        var entry = await Api.CreateEntry(new Entry
        {
            LexemeForm = new MultiString
            {
                Values =
                {
                    { "en", "test" }
                }
            }
        });
        await Api.DeleteEntry(entry.Id);

        var entries = await Api.GetEntries().ToArrayAsync();
        entries.Should().NotContain(e => e.Id == entry.Id);
    }
}
