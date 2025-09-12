namespace MiniLcm.Tests;

public abstract class BasicApiTestsBase : MiniLcmTestBase
{
    protected readonly Guid Entry1Id = new("a3f5aa5a-578f-4181-8f38-eaaf27f01f1c");
    protected readonly Guid Entry2Id = new("2de6c334-58fa-4844-b0fd-0bc2ce4ef835");

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await Api.CreateEntry(new Entry
        {
            Id = Entry1Id,
            LexemeForm = { { "en", "Kevin" } },
            Note =
            {
                { "en", new RichString("this is a test note from Kevin") }
            },
            CitationForm = { { "en", "Kevin" } },
            LiteralMeaning =
            {
                { "en", new RichString("Kevin") }
            },
            Senses =
            [
                new Sense
                {
                    Gloss = { { "en", "Kevin" } },
                    Definition =
                    {
                        { "en", new RichString("Kevin") }
                    },
                    ExampleSentences =
                    [
                        new ExampleSentence
                        {
                            Sentence =
                            {
                                { "en", new RichString("Kevin is a good guy") }
                            }
                        }
                    ]
                }
            ]
        });
        await Api.CreateEntry(new()
        {
            Id = Entry2Id,
            LexemeForm = { { "en", "apple" } },
            Senses =
            [
                new Sense
                {
                    Gloss = { { "en", "fruit" } },
                    Definition =
                    {
                        { "en", new RichString("a round fruit, red or yellow") }
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
        await Api.CreateWritingSystem(new WritingSystem()
        {
            Id = Guid.NewGuid(),
            Type = WritingSystemType.Vernacular,
            WsId = "es",
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
        var entry1 = entries.First(e => e.Id == Entry1Id);
        entry1.LexemeForm.Values.Should().NotBeEmpty();
        var sense1 = entry1.Senses.Should().NotBeEmpty().And.Subject.First();
        sense1.ExampleSentences.Should().NotBeEmpty();

        var entry2 = entries.First(e => e.Id == Entry2Id);
        entry2.LexemeForm.Values.Should().NotBeEmpty();
        entry2.Senses.Should().NotBeEmpty();
    }

    [Fact]
    public async Task SearchEntries()
    {
        var entries = await Api.SearchEntries("a").ToArrayAsync();
        entries.Should().NotBeEmpty();
        entries.Should().NotContain(e => e.Id == default);
        entries.Should().NotContain(e => e.LexemeForm["en"] == "Kevin");
    }
    [Fact]
    public async Task SearchEntries_MatchesGloss()
    {
        var entries = await Api.SearchEntries("fruit").ToArrayAsync();
        entries.Should().NotBeEmpty();
        entries.Should().NotContain(e => e.Id == default);
        entries.Should().Contain(e => e.LexemeForm["en"] == "apple");
    }

    [Fact]
    public async Task GetEntry()
    {
        var entry = await Api.GetEntry(Entry1Id);
        entry.Should().NotBeNull();
        entry.LexemeForm.Values.Should().NotBeEmpty();
        var sense = entry.Senses.Should()
            .NotBeEmpty($"because '{entry.LexemeForm.Values.First().Value}' should have a sense").And.Subject.First();
        sense.Gloss.Values.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CreateEntry()
    {
        var entry = await Api.CreateEntry(new()
        {
            LexemeForm = { { "en", "Kevin" } },
            Note =
            {
                { "en", new RichString("this is a test note from Kevin") }
            },
            CitationForm = { { "en", "Kevin" } },
            LiteralMeaning =
            {
                { "en", new RichString("Kevin") }
            },
            Senses =
            [
                new Sense
                {
                    Gloss = { { "en", "Kevin" } },
                    Definition =
                    {
                        { "en", new RichString("Kevin") }
                    },
                    ExampleSentences =
                    [
                        new ExampleSentence
                        {
                            Sentence =
                            {
                                { "en", new RichString("Kevin is a good guy") }
                            }
                        }
                    ]
                }
            ]
        });
        entry.Should().NotBeNull();
        entry.LexemeForm["en"].Should().Be("Kevin");
        entry.LiteralMeaning["en"].Should().BeEquivalentTo(new RichString("Kevin", "en"));
        entry.CitationForm["en"].Should().Be("Kevin");
        entry.Note["en"].Should().BeEquivalentTo(new RichString("this is a test note from Kevin", "en"));
        var sense = entry.Senses.Should().ContainSingle().Subject;
        sense.Gloss["en"].Should().Be("Kevin");
        sense.Definition["en"].Should().BeEquivalentTo(new RichString("Kevin", "en"));
        var example = sense.ExampleSentences.Should().ContainSingle().Subject;
        example.Sentence["en"].Should().BeEquivalentTo(new RichString("Kevin is a good guy", "en"));
    }

    [Fact]
    public async Task UpdateEntry()
    {
        var updatedEntry = await Api.UpdateEntry(Entry1Id,
            new UpdateObjectInput<Entry>()
                .Set(e => e.LexemeForm["en"], "updated"));
        updatedEntry.LexemeForm["en"].Should().Be("updated");
    }


    [Fact]
    public async Task UpdateEntryNote()
    {
        var entry = await Api.CreateEntry(new Entry
        {
            LexemeForm = new MultiString { { "en", "test" } }
        });
        var updatedEntry = await Api.UpdateEntry(entry.Id,
            new UpdateObjectInput<Entry>()
                .Set(e => e.Note["en"], new RichString("updated", "en")));
        updatedEntry.Note["en"].Should().BeEquivalentTo(new RichString("updated", "en"));
    }

    [Fact]
    public async Task UpdateSense()
    {
        var entry = await Api.CreateEntry(new Entry
        {
            LexemeForm = new MultiString { { "en", "test" } },
            Senses =
            [
                new Sense
                {
                    Definition = new() { { "en", new RichString("test") } }
                }
            ]
        });
        var updatedSense = await Api.UpdateSense(entry.Id,
            entry.Senses[0].Id,
            new UpdateObjectInput<Sense>()
                .Set(e => e.Definition["en"], new RichString("updated", "en")));
        updatedSense.Definition["en"].Should().BeEquivalentTo(new RichString("updated", "en"));
    }

    [Fact]
    public async Task CreateSense_WontCreateMissingDomains()
    {
        var senseId = Guid.NewGuid();
        var createdSense = await Api.CreateSense(Entry1Id, new Sense()
        {
            Id = senseId,
            SemanticDomains = [new SemanticDomain() { Id = Guid.NewGuid(), Code = "test", Name = new MultiString { { "en", "semdom" } } }],
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
        var createdSense = await Api.CreateSense(Entry1Id, new Sense()
        {
            Id = senseId,
            SemanticDomains = [semanticDomain],
        });
        createdSense.Id.Should().Be(senseId);
        createdSense.SemanticDomains.Should().ContainSingle(s => s.Id == semanticDomainId);
    }

    [Fact]
    public async Task CreateSense_WillThrowExceptionWithMissingPartOfSpeech()
    {
        var senseId = Guid.NewGuid();
        var partOfSpeechId = Guid.NewGuid();
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await Api.CreateSense(Entry1Id, new Sense() { Id = senseId, PartOfSpeech = null, PartOfSpeechId = partOfSpeechId, })
        );
        exception.Should().NotBeNull("because the part of speech does not exist (or was deleted)");
    }

    [Fact]
    public async Task CreateSense_WillCreateWthExistingPartOfSpeech()
    {
        var senseId = Guid.NewGuid();
        var partOfSpeechId = Guid.NewGuid();
        await Api.CreatePartOfSpeech(new PartOfSpeech() { Id = partOfSpeechId, Name = new MultiString() { { "en", "test" } } });
        var partOfSpeech = await Api.GetPartsOfSpeech().SingleOrDefaultAsync(pos => pos.Id == partOfSpeechId);
        ArgumentNullException.ThrowIfNull(partOfSpeech);
        var createdSense = await Api.CreateSense(Entry1Id,
            new Sense() { Id = senseId, PartOfSpeech = partOfSpeech, PartOfSpeechId = partOfSpeechId, });
        createdSense.Id.Should().Be(senseId);
        createdSense.PartOfSpeechId.Should().Be(partOfSpeechId, "because the part of speech does exist");
    }

    [Fact]
    public async Task UpdateExampleSentence()
    {
        var entry = await Api.CreateEntry(new Entry
        {
            LexemeForm = new MultiString { { "en", "test" } },
            Senses =
            [
                new Sense()
                {
                    Definition = new()
                    {
                        { "en", new RichString("test") }
                    },
                    ExampleSentences =
                    [
                        new ExampleSentence()
                        {
                            Sentence = new()
                            {
                                { "en", new RichString("test") }
                            }
                        }
                    ]
                }
            ]
        });
        entry.Senses.Should().ContainSingle().Which.ExampleSentences.Should().ContainSingle();
        var updatedExample = await Api.UpdateExampleSentence(entry.Id,
            entry.Senses[0].Id,
            entry.Senses[0].ExampleSentences[0].Id,
            new UpdateObjectInput<ExampleSentence>()
                .Set(e => e.Sentence["en"], new RichString("updated", "en")));
        updatedExample.Sentence["en"].Should().BeEquivalentTo(new RichString("updated", "en"));
    }

    [Fact]
    public async Task UpdateExampleSentenceTranslation()
    {
        var entryId = Guid.NewGuid();
        var senseId = Guid.NewGuid();
        var exampleId = Guid.NewGuid();
        var translationId = Guid.NewGuid();
        var entry = await Api.CreateEntry(new Entry
        {
            Id = entryId,
            LexemeForm = new MultiString { { "en", "test" } },
            Senses =
            [
                new Sense()
                {
                    Id = senseId,
                    Definition = new()
                    {
                        { "en", new RichString("test") }
                    },
                    ExampleSentences =
                    [
                        new ExampleSentence()
                        {
                            Id = exampleId,
                            Sentence = new()
                            {
                                { "en", new RichString("test") }
                            },
                            Translations =
                            [
                                new Translation() { Id = translationId, Text = { { "en", new RichString("test") } } }
                            ]
                        }
                    ]
                }
            ]
        });
        entry.Senses.Should().ContainSingle().Which.ExampleSentences.Should().ContainSingle().Which.Translations
            .Should().ContainSingle();


        await Api.UpdateTranslation(entryId,
            senseId,
            exampleId,
            translationId,
            new UpdateObjectInput<Translation>().Set(t => t.Text["en"], new RichString("updated", "en"))
        );


        var exampleSentence = await Api.GetExampleSentence(entryId, senseId, exampleId);
        exampleSentence.Should().NotBeNull();
        var updatedTranslation = exampleSentence.Translations.Should().ContainSingle().Subject;
        updatedTranslation.Id.Should().Be(translationId);
        updatedTranslation.Text["en"].Should().BeEquivalentTo(new RichString("updated", "en"));
    }

    [Fact]
    public async Task DeleteEntry()
    {
        var entry = await Api.CreateEntry(new Entry
        {
            LexemeForm = new MultiString { { "en", "test" } }
        });
        await Api.DeleteEntry(entry.Id);

        var entries = await Api.GetEntries().ToArrayAsync();
        entries.Should().NotContain(e => e.Id == entry.Id);
    }
}
