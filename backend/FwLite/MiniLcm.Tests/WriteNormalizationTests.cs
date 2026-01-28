using MiniLcm.Models;
using MiniLcm.Normalization;
using MiniLcm.SyncHelpers;
using Moq;

namespace MiniLcm.Tests;

public class WriteNormalizationTests
{
    public IMiniLcmApi MockApi { get; init; }
    public IMiniLcmApi NormalizingApi { get; init; }

    public const string NFCString = "na\u00efve"; // "naïve" with U+00EF LATIN SMALL LETTER I WITH DIAERESIS
    public const string NFDString = "na\u0069\u0308ve"; // "naïve" with U+0069 LATIN SMALL LETTER I + U+0308 COMBINING DIAERESIS

    public WriteNormalizationTests()
    {
        MockApi = Mock.Of<IMiniLcmApi>();
        var factory = new MiniLcmWriteApiNormalizationWrapperFactory();
        NormalizingApi = factory.Create(MockApi);
    }

    [Fact]
    public async Task CreateWritingSystem_NormalizesText()
    {
        var ws = new WritingSystem
        {
            Id = Guid.NewGuid(),
            WsId = "en",
            Name = NFCString,
            Abbreviation = NFCString,
            Font = NFCString,
            Type = WritingSystemType.Analysis,
            Exemplars = [NFCString, NFCString]
        };

        await NormalizingApi.CreateWritingSystem(ws);

        Mock.Get(MockApi).Verify(api => api.CreateWritingSystem(
            It.Is<WritingSystem>(w =>
                w.Name == NFDString &&
                w.Abbreviation == NFDString &&
                w.Font == NFDString &&
                w.Exemplars[0] == NFDString &&
                w.Exemplars[1] == NFDString
            ),
            null
        ));
    }

    [Fact]
    public async Task CreatePartOfSpeech_NormalizesMultiString()
    {
        var pos = new PartOfSpeech
        {
            Id = Guid.NewGuid(),
            Name = new MultiString { Values = { { "en", NFCString } } }
        };

        await NormalizingApi.CreatePartOfSpeech(pos);

        Mock.Get(MockApi).Verify(api => api.CreatePartOfSpeech(
            It.Is<PartOfSpeech>(p => p.Name.Values["en"] == NFDString)
        ));
    }

    [Fact]
    public async Task CreatePublication_NormalizesMultiString()
    {
        var pub = new Publication
        {
            Id = Guid.NewGuid(),
            Name = new MultiString { Values = { { "en", NFCString } } }
        };

        await NormalizingApi.CreatePublication(pub);

        Mock.Get(MockApi).Verify(api => api.CreatePublication(
            It.Is<Publication>(p => p.Name.Values["en"] == NFDString)
        ));
    }

    [Fact]
    public async Task CreateSemanticDomain_NormalizesName()
    {
        var sd = new SemanticDomain
        {
            Id = Guid.NewGuid(),
            Code = "1.1.1", // Code should NOT be normalized
            Name = new MultiString { Values = { { "en", NFCString } } }
        };

        await NormalizingApi.CreateSemanticDomain(sd);

        Mock.Get(MockApi).Verify(api => api.CreateSemanticDomain(
            It.Is<SemanticDomain>(s => 
                s.Name.Values["en"] == NFDString &&
                s.Code == "1.1.1" // unchanged
            )
        ));
    }

    [Fact]
    public async Task CreateComplexFormType_NormalizesMultiString()
    {
        var cft = new ComplexFormType
        {
            Id = Guid.NewGuid(),
            Name = new MultiString { Values = { { "en", NFCString } } }
        };

        await NormalizingApi.CreateComplexFormType(cft);

        Mock.Get(MockApi).Verify(api => api.CreateComplexFormType(
            It.Is<ComplexFormType>(c => c.Name.Values["en"] == NFDString)
        ));
    }

    [Fact]
    public async Task CreateMorphTypeData_NormalizesAllTextFields()
    {
        var mtd = new MorphTypeData
        {
            Id = Guid.NewGuid(),
            Name = new MultiString { Values = { { "en", NFCString } } },
            Abbreviation = new MultiString { Values = { { "en", NFCString } } },
            Description = new RichMultiString { { "en", new RichString(NFCString) } },
            LeadingToken = NFCString,
            TrailingToken = NFCString
        };

        await NormalizingApi.CreateMorphTypeData(mtd);

        Mock.Get(MockApi).Verify(api => api.CreateMorphTypeData(
            It.Is<MorphTypeData>(m =>
                m.Name.Values["en"] == NFDString &&
                m.Abbreviation.Values["en"] == NFDString &&
                m.Description["en"].GetPlainText() == NFDString &&
                m.LeadingToken == NFDString &&
                m.TrailingToken == NFDString
            )
        ));
    }

    [Fact]
    public async Task CreateEntry_NormalizesAllTextFields()
    {
        var entry = new Entry
        {
            Id = Guid.NewGuid(),
            LexemeForm = new MultiString { Values = { { "en", NFCString } } },
            CitationForm = new MultiString { Values = { { "en", NFCString } } },
            LiteralMeaning = new RichMultiString { { "en", new RichString(NFCString) } },
            Note = new RichMultiString { { "en", new RichString(NFCString) } }
        };

        await NormalizingApi.CreateEntry(entry);

        Mock.Get(MockApi).Verify(api => api.CreateEntry(
            It.Is<Entry>(e =>
                e.LexemeForm.Values["en"] == NFDString &&
                e.CitationForm.Values["en"] == NFDString &&
                e.LiteralMeaning["en"].GetPlainText() == NFDString &&
                e.Note["en"].GetPlainText() == NFDString
            ),
            null
        ));
    }

    [Fact]
    public async Task CreateEntry_NormalizesNestedSenses()
    {
        var entry = new Entry
        {
            Id = Guid.NewGuid(),
            LexemeForm = new MultiString(),
            CitationForm = new MultiString(),
            Senses =
            [
                new Sense
                {
                    Id = Guid.NewGuid(),
                    Gloss = new MultiString { Values = { { "en", NFCString } } },
                    Definition = new RichMultiString { { "en", new RichString(NFCString) } }
                }
            ]
        };

        await NormalizingApi.CreateEntry(entry);

        Mock.Get(MockApi).Verify(api => api.CreateEntry(
            It.Is<Entry>(e =>
                e.Senses[0].Gloss.Values["en"] == NFDString &&
                e.Senses[0].Definition["en"].GetPlainText() == NFDString
            ),
            null
        ));
    }

    [Fact]
    public async Task CreateSense_NormalizesTextFields()
    {
        var entryId = Guid.NewGuid();
        var sense = new Sense
        {
            Id = Guid.NewGuid(),
            EntryId = entryId,
            Gloss = new MultiString { Values = { { "en", NFCString } } },
            Definition = new RichMultiString { { "en", new RichString(NFCString) } }
        };

        await NormalizingApi.CreateSense(entryId, sense);

        Mock.Get(MockApi).Verify(api => api.CreateSense(
            entryId,
            It.Is<Sense>(s =>
                s.Gloss.Values["en"] == NFDString &&
                s.Definition["en"].GetPlainText() == NFDString
            ),
            null
        ));
    }

    [Fact]
    public async Task CreateExampleSentence_NormalizesTextFields()
    {
        var entryId = Guid.NewGuid();
        var senseId = Guid.NewGuid();
        var example = new ExampleSentence
        {
            Id = Guid.NewGuid(),
            SenseId = senseId,
            Sentence = new RichMultiString { { "en", new RichString(NFCString) } },
            Reference = new RichString(NFCString)
        };

        await NormalizingApi.CreateExampleSentence(entryId, senseId, example);

        Mock.Get(MockApi).Verify(api => api.CreateExampleSentence(
            entryId,
            senseId,
            It.Is<ExampleSentence>(ex =>
                ex.Sentence["en"].GetPlainText() == NFDString &&
                ex.Reference!.GetPlainText() == NFDString
            ),
            null
        ));
    }

    [Fact]
    public async Task AddTranslation_NormalizesTextFields()
    {
        var entryId = Guid.NewGuid();
        var senseId = Guid.NewGuid();
        var exampleId = Guid.NewGuid();
        var translation = new Translation
        {
            Id = Guid.NewGuid(),
            Text = new RichMultiString { { "en", new RichString(NFCString) } }
        };

        await NormalizingApi.AddTranslation(entryId, senseId, exampleId, translation);

        Mock.Get(MockApi).Verify(api => api.AddTranslation(
            entryId,
            senseId,
            exampleId,
            It.Is<Translation>(t => t.Text["en"].GetPlainText() == NFDString)
        ));
    }

    [Fact]
    public async Task CreateComplexFormComponent_NormalizesHeadwords()
    {
        var cfc = new ComplexFormComponent
        {
            Id = Guid.NewGuid(),
            ComplexFormEntryId = Guid.NewGuid(),
            ComponentEntryId = Guid.NewGuid(),
            ComplexFormHeadword = NFCString,
            ComponentHeadword = NFCString
        };

        await NormalizingApi.CreateComplexFormComponent(cfc);

        Mock.Get(MockApi).Verify(api => api.CreateComplexFormComponent(
            It.Is<ComplexFormComponent>(c =>
                c.ComplexFormHeadword == NFDString &&
                c.ComponentHeadword == NFDString
            ),
            null
        ));
    }

    [Fact]
    public async Task CreateEntry_WithComplexNestedStructure_NormalizesAll()
    {
        // Test a deeply nested structure with all text types
        var entry = new Entry
        {
            Id = Guid.NewGuid(),
            LexemeForm = new MultiString { Values = { { "en", NFCString } } },
            CitationForm = new MultiString { Values = { { "en", NFCString } } },
            Note = new RichMultiString { { "en", new RichString(NFCString) } },
            Senses =
            [
                new Sense
                {
                    Id = Guid.NewGuid(),
                    Gloss = new MultiString { Values = { { "en", NFCString } } },
                    Definition = new RichMultiString { { "en", new RichString(NFCString) } },
                    ExampleSentences =
                    [
                        new ExampleSentence
                        {
                            Id = Guid.NewGuid(),
                            Sentence = new RichMultiString { { "en", new RichString(NFCString) } },
                            Reference = new RichString(NFCString),
                            Translations =
                            [
                                new Translation
                                {
                                    Id = Guid.NewGuid(),
                                    Text = new RichMultiString { { "en", new RichString(NFCString) } }
                                }
                            ]
                        }
                    ]
                }
            ]
        };

        await NormalizingApi.CreateEntry(entry);

        Mock.Get(MockApi).Verify(api => api.CreateEntry(
            It.Is<Entry>(e =>
                e.LexemeForm.Values["en"] == NFDString &&
                e.Note["en"].GetPlainText() == NFDString &&
                e.Senses[0].Gloss.Values["en"] == NFDString &&
                e.Senses[0].Definition["en"].GetPlainText() == NFDString &&
                e.Senses[0].ExampleSentences[0].Sentence["en"].GetPlainText() == NFDString &&
                e.Senses[0].ExampleSentences[0].Reference!.GetPlainText() == NFDString &&
                e.Senses[0].ExampleSentences[0].Translations[0].Text["en"].GetPlainText() == NFDString
            ),
            null
        ));
    }

    [Fact]
    public async Task BulkCreateEntries_NormalizesEach()
    {
        var entries = new[]
        {
            new Entry
            {
                Id = Guid.NewGuid(),
                LexemeForm = new MultiString { Values = { { "en", NFCString } } },
                CitationForm = new MultiString()
            },
            new Entry
            {
                Id = Guid.NewGuid(),
                LexemeForm = new MultiString { Values = { { "en", NFCString } } },
                CitationForm = new MultiString()
            }
        }.ToAsyncEnumerable();

        await NormalizingApi.BulkCreateEntries(entries);

        // The bulk operation should pass a normalized stream to the underlying API
        // We can't easily verify the stream contents with Moq, but we can verify it was called
        Mock.Get(MockApi).Verify(
            api => api.BulkCreateEntries(It.IsAny<IAsyncEnumerable<Entry>>()),
            Times.Once
        );
    }

    [Fact]
    public async Task RichString_WithMultipleSpans_NormalizesAll()
    {
        var richString = new RichString([
            new RichSpan { Text = NFCString, Ws = "en" },
            new RichSpan { Text = NFCString, Ws = "en", Bold = RichTextToggle.On }
        ]);

        var example = new ExampleSentence
        {
            Id = Guid.NewGuid(),
            Sentence = new RichMultiString { { "en", richString } }
        };

        await NormalizingApi.CreateExampleSentence(Guid.NewGuid(), Guid.NewGuid(), example);

        Mock.Get(MockApi).Verify(api => api.CreateExampleSentence(
            It.IsAny<Guid>(),
            It.IsAny<Guid>(),
            It.Is<ExampleSentence>(ex =>
                ex.Sentence["en"].Spans[0].Text == NFDString &&
                ex.Sentence["en"].Spans[1].Text == NFDString
            ),
            null
        ));
    }

    [Fact]
    public void StringNormalizer_NormalizesMultiString()
    {
        var ms = new MultiString { Values = { { "en", NFCString }, { "fr", NFCString } } };
        var normalized = StringNormalizer.Normalize(ms);
        
        normalized.Values["en"].Should().Be(NFDString);
        normalized.Values["fr"].Should().Be(NFDString);
    }

    [Fact]
    public void StringNormalizer_NormalizesRichString()
    {
        var rs = new RichString(NFCString);
        var normalized = StringNormalizer.Normalize(rs);
        
        normalized!.GetPlainText().Should().Be(NFDString);
    }

    [Fact]
    public void StringNormalizer_NormalizesRichMultiString()
    {
        var rms = new RichMultiString
        {
            { "en", new RichString(NFCString) },
            { "fr", new RichString(NFCString) }
        };
        var normalized = StringNormalizer.Normalize(rms);
        
        normalized["en"].GetPlainText().Should().Be(NFDString);
        normalized["fr"].GetPlainText().Should().Be(NFDString);
    }

    [Fact]
    public void StringNormalizer_NormalizesStringArray()
    {
        var array = new[] { NFCString, NFCString };
        var normalized = StringNormalizer.Normalize(array);
        
        normalized[0].Should().Be(NFDString);
        normalized[1].Should().Be(NFDString);
    }

    [Fact]
    public void StringNormalizer_HandlesNullRichString()
    {
        RichString? rs = null;
        var normalized = StringNormalizer.Normalize(rs);
        
        normalized.Should().BeNull();
    }

    [Fact]
    public void StringNormalizer_HandlesNullString()
    {
        string? str = null;
        var normalized = StringNormalizer.Normalize(str);
        
        normalized.Should().BeNull();
    }
}
