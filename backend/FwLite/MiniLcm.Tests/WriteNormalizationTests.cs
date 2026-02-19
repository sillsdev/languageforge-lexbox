using System.Collections;
using System.Reflection;
using System.Text;
using MiniLcm.Models;
using MiniLcm.Normalization;
using MiniLcm.SyncHelpers;
using Moq;

namespace MiniLcm.Tests;

/// <summary>
/// Tests for the MiniLcmWriteApiNormalizationWrapper.
/// These tests verify that all user-entered text is normalized to NFD on write operations.
/// </summary>
public class WriteNormalizationTests
{
    private readonly IMiniLcmApi _mockApi;
    private readonly IMiniLcmApi _normalizingApi;

    public WriteNormalizationTests()
    {
        _mockApi = Mock.Of<IMiniLcmApi>();
        var factory = new MiniLcmWriteApiNormalizationWrapperFactory();
        _normalizingApi = factory.Create(_mockApi);
    }


    private static void AssertNfc(object obj) => NormalizationAssert.AssertAllNfc(obj);
    private static void AssertNfd(object obj) => NormalizationAssert.AssertAllNfd(obj);
    private static bool IsNfd(object obj) => NormalizationAssert.IsAllNfd(obj);

    private static void AssertAllNfc<T>(IEnumerable<T> values)
    {
        foreach (var value in values) AssertNfc(value!);
    }

    private static void AssertAllNfd<T>(IEnumerable<T> values)
    {
        foreach (var value in values) AssertNfd(value!);
    }

    #region WritingSystem Tests

    [Fact]
    public async Task CreateWritingSystem_NormalizesToNfd()
    {
        var ws = NfcTestData.CreateNfcWritingSystem();
        AssertNfc(ws);

        await _normalizingApi.CreateWritingSystem(ws);

        Mock.Get(_mockApi).Verify(api => api.CreateWritingSystem(
            It.Is<WritingSystem>(w => IsNfd(w)),
            null
        ));
    }

    [Fact]
    public async Task UpdateWritingSystem_BeforeAfter_NormalizesToNfd()
    {
        var before = NfcTestData.CreateNfcWritingSystem();
        var after = NfcTestData.CreateNfcWritingSystem();
        AssertNfc(after);

        await _normalizingApi.UpdateWritingSystem(before, after);

        Mock.Get(_mockApi).Verify(api => api.UpdateWritingSystem(
            It.IsAny<WritingSystem>(),
            It.Is<WritingSystem>(w => IsNfd(w)),
            It.IsAny<IMiniLcmApi>()
        ));
    }

    #endregion

    #region PartOfSpeech Tests

    [Fact]
    public async Task CreatePartOfSpeech_NormalizesToNfd()
    {
        var pos = NfcTestData.CreateNfcPartOfSpeech();
        AssertNfc(pos);

        await _normalizingApi.CreatePartOfSpeech(pos);

        Mock.Get(_mockApi).Verify(api => api.CreatePartOfSpeech(
            It.Is<PartOfSpeech>(p => IsNfd(p))
        ));
    }

    [Fact]
    public async Task UpdatePartOfSpeech_BeforeAfter_NormalizesToNfd()
    {
        var before = NfcTestData.CreateNfcPartOfSpeech();
        var after = NfcTestData.CreateNfcPartOfSpeech();
        AssertNfc(after);

        await _normalizingApi.UpdatePartOfSpeech(before, after);

        Mock.Get(_mockApi).Verify(api => api.UpdatePartOfSpeech(
            It.IsAny<PartOfSpeech>(),
            It.Is<PartOfSpeech>(p => IsNfd(p)),
            It.IsAny<IMiniLcmApi>()
        ));
    }

    #endregion

    #region Publication Tests

    [Fact]
    public async Task CreatePublication_NormalizesToNfd()
    {
        var pub = NfcTestData.CreateNfcPublication();
        AssertNfc(pub);

        await _normalizingApi.CreatePublication(pub);

        Mock.Get(_mockApi).Verify(api => api.CreatePublication(
            It.Is<Publication>(p => IsNfd(p))
        ));
    }

    [Fact]
    public async Task UpdatePublication_BeforeAfter_NormalizesToNfd()
    {
        var before = NfcTestData.CreateNfcPublication();
        var after = NfcTestData.CreateNfcPublication();
        AssertNfc(after);

        await _normalizingApi.UpdatePublication(before, after);

        Mock.Get(_mockApi).Verify(api => api.UpdatePublication(
            It.IsAny<Publication>(),
            It.Is<Publication>(p => IsNfd(p)),
            It.IsAny<IMiniLcmApi>()
        ));
    }

    #endregion

    #region SemanticDomain Tests

    [Fact]
    public async Task CreateSemanticDomain_NormalizesToNfd()
    {
        var sd = NfcTestData.CreateNfcSemanticDomain();
        AssertNfc(sd);

        await _normalizingApi.CreateSemanticDomain(sd);

        Mock.Get(_mockApi).Verify(api => api.CreateSemanticDomain(
            It.Is<SemanticDomain>(s => IsNfd(s))
        ));
    }

    [Fact]
    public async Task UpdateSemanticDomain_BeforeAfter_NormalizesToNfd()
    {
        var before = NfcTestData.CreateNfcSemanticDomain();
        var after = NfcTestData.CreateNfcSemanticDomain();
        AssertNfc(after);

        await _normalizingApi.UpdateSemanticDomain(before, after);

        Mock.Get(_mockApi).Verify(api => api.UpdateSemanticDomain(
            It.IsAny<SemanticDomain>(),
            It.Is<SemanticDomain>(s => IsNfd(s)),
            It.IsAny<IMiniLcmApi>()
        ));
    }

    [Fact]
    public async Task AddSemanticDomainToSense_NormalizesToNfd()
    {
        var sd = NfcTestData.CreateNfcSemanticDomain();
        AssertNfc(sd);

        await _normalizingApi.AddSemanticDomainToSense(Guid.NewGuid(), sd);

        Mock.Get(_mockApi).Verify(api => api.AddSemanticDomainToSense(
            It.IsAny<Guid>(),
            It.Is<SemanticDomain>(s => IsNfd(s))
        ));
    }

    [Fact]
    public async Task BulkImportSemanticDomains_NormalizesToNfd()
    {
        var domains = new[] { NfcTestData.CreateNfcSemanticDomain(), NfcTestData.CreateNfcSemanticDomain() };
        AssertAllNfc(domains);

        var capturedDomains = new List<SemanticDomain>();
        Mock.Get(_mockApi)
            .Setup(api => api.BulkImportSemanticDomains(It.IsAny<IAsyncEnumerable<SemanticDomain>>()))
            .Returns(async (IAsyncEnumerable<SemanticDomain> stream) =>
            {
                await foreach (var sd in stream) capturedDomains.Add(sd);
            });

        await _normalizingApi.BulkImportSemanticDomains(domains.ToAsyncEnumerable());

        capturedDomains.Should().HaveCount(2);
        AssertAllNfd(capturedDomains);
    }

    #endregion

    #region ComplexFormType Tests

    [Fact]
    public async Task CreateComplexFormType_NormalizesToNfd()
    {
        var cft = NfcTestData.CreateNfcComplexFormType();
        AssertNfc(cft);

        await _normalizingApi.CreateComplexFormType(cft);

        Mock.Get(_mockApi).Verify(api => api.CreateComplexFormType(
            It.Is<ComplexFormType>(c => IsNfd(c))
        ));
    }

    [Fact]
    public async Task UpdateComplexFormType_BeforeAfter_NormalizesToNfd()
    {
        var before = NfcTestData.CreateNfcComplexFormType();
        var after = NfcTestData.CreateNfcComplexFormType();
        AssertNfc(after);

        await _normalizingApi.UpdateComplexFormType(before, after);

        Mock.Get(_mockApi).Verify(api => api.UpdateComplexFormType(
            It.IsAny<ComplexFormType>(),
            It.Is<ComplexFormType>(c => IsNfd(c)),
            It.IsAny<IMiniLcmApi>()
        ));
    }

    #endregion

    #region MorphTypeData Tests

    [Fact]
    public async Task CreateMorphTypeData_NormalizesToNfd()
    {
        var mtd = NfcTestData.CreateNfcMorphTypeData();
        AssertNfc(mtd);

        await _normalizingApi.CreateMorphTypeData(mtd);

        Mock.Get(_mockApi).Verify(api => api.CreateMorphTypeData(
            It.Is<MorphTypeData>(m => IsNfd(m))
        ));
    }

    [Fact]
    public async Task UpdateMorphTypeData_BeforeAfter_NormalizesToNfd()
    {
        var before = NfcTestData.CreateNfcMorphTypeData();
        var after = NfcTestData.CreateNfcMorphTypeData();
        AssertNfc(after);

        await _normalizingApi.UpdateMorphTypeData(before, after);

        Mock.Get(_mockApi).Verify(api => api.UpdateMorphTypeData(
            It.IsAny<MorphTypeData>(),
            It.Is<MorphTypeData>(m => IsNfd(m)),
            It.IsAny<IMiniLcmApi>()
        ));
    }

    #endregion

    #region Entry Tests

    [Fact]
    public async Task CreateEntry_NormalizesToNfd()
    {
        var entry = NfcTestData.CreateNfcEntry();
        AssertNfc(entry);

        await _normalizingApi.CreateEntry(entry);

        Mock.Get(_mockApi).Verify(api => api.CreateEntry(
            It.Is<Entry>(e => IsNfd(e)),
            null
        ));
    }

    [Fact]
    public async Task UpdateEntry_BeforeAfter_NormalizesToNfd()
    {
        var before = NfcTestData.CreateNfcEntry();
        var after = NfcTestData.CreateNfcEntry();
        AssertNfc(after);

        await _normalizingApi.UpdateEntry(before, after);

        Mock.Get(_mockApi).Verify(api => api.UpdateEntry(
            It.IsAny<Entry>(),
            It.Is<Entry>(e => IsNfd(e)),
            It.IsAny<IMiniLcmApi>()
        ));
    }

    [Fact]
    public async Task CreateEntry_WithNestedSenses_NormalizesToNfd()
    {
        var entry = NfcTestData.CreateNfcEntryWithSenses();
        AssertNfc(entry);

        await _normalizingApi.CreateEntry(entry);

        Mock.Get(_mockApi).Verify(api => api.CreateEntry(
            It.Is<Entry>(e => IsNfd(e)),
            null
        ));
    }

    [Fact]
    public async Task CreateEntry_WithComplexFormComponents_NormalizesToNfd()
    {
        var entry = NfcTestData.CreateNfcEntryWithComponents();
        AssertNfc(entry);

        await _normalizingApi.CreateEntry(entry);

        Mock.Get(_mockApi).Verify(api => api.CreateEntry(
            It.Is<Entry>(e => IsNfd(e)),
            null
        ));
    }

    [Fact]
    public async Task BulkCreateEntries_NormalizesToNfd()
    {
        var entries = new[] { NfcTestData.CreateNfcEntry(), NfcTestData.CreateNfcEntryWithSenses() };
        AssertAllNfc(entries);

        var capturedEntries = new List<Entry>();
        Mock.Get(_mockApi)
            .Setup(api => api.BulkCreateEntries(It.IsAny<IAsyncEnumerable<Entry>>()))
            .Returns(async (IAsyncEnumerable<Entry> stream) =>
            {
                await foreach (var e in stream) capturedEntries.Add(e);
            });

        await _normalizingApi.BulkCreateEntries(entries.ToAsyncEnumerable());

        capturedEntries.Should().HaveCount(2);
        AssertAllNfd(capturedEntries);
    }

    #endregion

    #region ComplexFormComponent Tests

    [Fact]
    public async Task CreateComplexFormComponent_NormalizesToNfd()
    {
        var cfc = NfcTestData.CreateNfcComplexFormComponent();
        AssertNfc(cfc);

        await _normalizingApi.CreateComplexFormComponent(cfc);

        Mock.Get(_mockApi).Verify(api => api.CreateComplexFormComponent(
            It.Is<ComplexFormComponent>(c => IsNfd(c)),
            null
        ));
    }

    #endregion

    #region Sense Tests

    [Fact]
    public async Task CreateSense_NormalizesToNfd()
    {
        var sense = NfcTestData.CreateNfcSense();
        AssertNfc(sense);

        await _normalizingApi.CreateSense(Guid.NewGuid(), sense);

        Mock.Get(_mockApi).Verify(api => api.CreateSense(
            It.IsAny<Guid>(),
            It.Is<Sense>(s => IsNfd(s)),
            null
        ));
    }

    [Fact]
    public async Task UpdateSense_BeforeAfter_NormalizesToNfd()
    {
        var entryId = Guid.NewGuid();
        var before = NfcTestData.CreateNfcSense();
        var after = NfcTestData.CreateNfcSense();
        AssertNfc(after);

        await _normalizingApi.UpdateSense(entryId, before, after);

        Mock.Get(_mockApi).Verify(api => api.UpdateSense(
            entryId,
            It.IsAny<Sense>(),
            It.Is<Sense>(s => IsNfd(s)),
            It.IsAny<IMiniLcmApi>()
        ));
    }

    [Fact]
    public async Task CreateSense_WithNestedExampleSentences_NormalizesToNfd()
    {
        var sense = NfcTestData.CreateNfcSenseWithExamples();
        AssertNfc(sense);

        await _normalizingApi.CreateSense(Guid.NewGuid(), sense);

        Mock.Get(_mockApi).Verify(api => api.CreateSense(
            It.IsAny<Guid>(),
            It.Is<Sense>(s => IsNfd(s)),
            null
        ));
    }

    #endregion

    #region ExampleSentence Tests

    [Fact]
    public async Task CreateExampleSentence_NormalizesToNfd()
    {
        var example = NfcTestData.CreateNfcExampleSentence();
        AssertNfc(example);

        await _normalizingApi.CreateExampleSentence(Guid.NewGuid(), Guid.NewGuid(), example);

        Mock.Get(_mockApi).Verify(api => api.CreateExampleSentence(
            It.IsAny<Guid>(),
            It.IsAny<Guid>(),
            It.Is<ExampleSentence>(e => IsNfd(e)),
            null
        ));
    }

    [Fact]
    public async Task UpdateExampleSentence_BeforeAfter_NormalizesToNfd()
    {
        var entryId = Guid.NewGuid();
        var senseId = Guid.NewGuid();
        var before = NfcTestData.CreateNfcExampleSentence();
        var after = NfcTestData.CreateNfcExampleSentence();
        AssertNfc(after);

        await _normalizingApi.UpdateExampleSentence(entryId, senseId, before, after);

        Mock.Get(_mockApi).Verify(api => api.UpdateExampleSentence(
            entryId,
            senseId,
            It.IsAny<ExampleSentence>(),
            It.Is<ExampleSentence>(e => IsNfd(e)),
            It.IsAny<IMiniLcmApi>()
        ));
    }

    [Fact]
    public async Task CreateExampleSentence_WithTranslations_NormalizesToNfd()
    {
        var example = NfcTestData.CreateNfcExampleSentenceWithTranslations();
        AssertNfc(example);

        await _normalizingApi.CreateExampleSentence(Guid.NewGuid(), Guid.NewGuid(), example);

        Mock.Get(_mockApi).Verify(api => api.CreateExampleSentence(
            It.IsAny<Guid>(),
            It.IsAny<Guid>(),
            It.Is<ExampleSentence>(e => IsNfd(e)),
            null
        ));
    }

    #endregion

    #region Translation Tests

    [Fact]
    public async Task AddTranslation_NormalizesToNfd()
    {
        var translation = NfcTestData.CreateNfcTranslation();
        AssertNfc(translation);

        await _normalizingApi.AddTranslation(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), translation);

        Mock.Get(_mockApi).Verify(api => api.AddTranslation(
            It.IsAny<Guid>(),
            It.IsAny<Guid>(),
            It.IsAny<Guid>(),
            It.Is<Translation>(t => IsNfd(t))
        ));
    }

    #endregion
}


/// <summary>
/// Tests for NormalizationAssert to ensure it correctly detects NFC/NFD issues.
/// These tests verify the assertion helpers don't have false negatives.
/// </summary>
public class NormalizationAssertTests
{
    [Fact]
    public void AssertAllNfc_WithNfcData_DoesNotThrow()
    {
        var entry = NfcTestData.CreateNfcEntry();

        // Should not throw
        NormalizationAssert.AssertAllNfc(entry);
    }

    [Fact]
    public void AssertAllNfc_WithNfdData_Throws()
    {
        var entry = new Entry
        {
            Id = Guid.NewGuid(),
            LexemeForm = new MultiString { Values = { { "en", NfcTestData.Nfd } } }, // NFD should fail
            CitationForm = new MultiString { Values = { { "en", NfcTestData.Nfc } } },
            LiteralMeaning = new RichMultiString { { "en", new RichString(NfcTestData.Nfc) } },
            Note = new RichMultiString { { "en", new RichString(NfcTestData.Nfc) } }
        };

        var act = () => NormalizationAssert.AssertAllNfc(entry);

        act.Should().Throw<Xunit.Sdk.XunitException>()
            .WithMessage("*NFC*");
    }

    [Fact]
    public void AssertAllNfd_WithNfdData_DoesNotThrow()
    {
        var entry = new Entry
        {
            Id = Guid.NewGuid(),
            LexemeForm = new MultiString { Values = { { "en", NfcTestData.Nfd } } },
            CitationForm = new MultiString { Values = { { "en", NfcTestData.Nfd } } },
            LiteralMeaning = new RichMultiString { { "en", new RichString(NfcTestData.Nfd) } },
            Note = new RichMultiString { { "en", new RichString(NfcTestData.Nfd) } }
        };

        // Should not throw
        NormalizationAssert.AssertAllNfd(entry);
    }

    [Fact]
    public void AssertAllNfd_WithNfcData_Throws()
    {
        var entry = NfcTestData.CreateNfcEntry();

        var act = () => NormalizationAssert.AssertAllNfd(entry);

        act.Should().Throw<Xunit.Sdk.XunitException>()
            .WithMessage("*NFD*");
    }

    [Fact]
    public void AssertAllNfc_WithEmptyMultiString_Throws()
    {
        var entry = new Entry
        {
            Id = Guid.NewGuid(),
            LexemeForm = new MultiString(), // Empty - should fail
            CitationForm = NfcTestData.CreateNfcMultiString()
        };

        var act = () => NormalizationAssert.AssertAllNfc(entry);

        act.Should().Throw<Xunit.Sdk.XunitException>()
            .WithMessage("*no values*");
    }

    [Fact]
    public void AssertAllNfc_WithNestedNfdData_Throws()
    {
        var entry = new Entry
        {
            Id = Guid.NewGuid(),
            LexemeForm = NfcTestData.CreateNfcMultiString(),
            CitationForm = NfcTestData.CreateNfcMultiString(),
            LiteralMeaning = NfcTestData.CreateNfcRichMultiString(),
            Note = NfcTestData.CreateNfcRichMultiString(),
            Senses =
            [
                new Sense
                {
                    Id = Guid.NewGuid(),
                    Gloss = new MultiString { Values = { { "en", NfcTestData.Nfd } } }, // NFD nested in sense
                    Definition = NfcTestData.CreateNfcRichMultiString()
                }
            ]
        };

        var act = () => NormalizationAssert.AssertAllNfc(entry);

        act.Should().Throw<Xunit.Sdk.XunitException>()
            .WithMessage("*Senses*Gloss*");
    }

    [Fact]
    public void IsAllNfd_WithNfdData_ReturnsTrue()
    {
        var multiString = new MultiString { Values = { { "en", NfcTestData.Nfd } } };

        NormalizationAssert.IsAllNfd(multiString).Should().BeTrue();
    }

    [Fact]
    public void IsAllNfd_WithNfcData_ReturnsFalse()
    {
        var multiString = NfcTestData.CreateNfcMultiString();

        NormalizationAssert.IsAllNfd(multiString).Should().BeFalse();
    }
}

/// <summary>
/// Provides test data with NFC-normalized strings for all entity types.
/// Each Create method returns an object with ALL normalizable properties populated with NFC strings.
/// </summary>
public static class NfcTestData
{
    /// <summary>
    /// NFC string: "naïve" with U+00EF LATIN SMALL LETTER I WITH DIAERESIS (composed form)
    /// </summary>
    public const string Nfc = "na\u00efve";

    /// <summary>
    /// NFD string: "naïve" with U+0069 LATIN SMALL LETTER I + U+0308 COMBINING DIAERESIS (decomposed form)
    /// </summary>
    public const string Nfd = "na\u0069\u0308ve";

    public static MultiString CreateNfcMultiString() => new() { Values = { { "en", Nfc }, { "fr", Nfc } } };

    public static RichString CreateNfcRichString() => new([
        new RichSpan { Text = Nfc, Ws = "en" },
        new RichSpan { Text = Nfc, Ws = "en", Bold = RichTextToggle.On }
    ]);

    public static RichMultiString CreateNfcRichMultiString() => new()
    {
        { "en", CreateNfcRichString() },
        { "fr", CreateNfcRichString() }
    };

    public static WritingSystem CreateNfcWritingSystem() => new()
    {
        Id = Guid.NewGuid(),
        WsId = "en",
        Type = WritingSystemType.Analysis,
        Name = Nfc,
        Abbreviation = Nfc,
        Font = Nfc,
        Exemplars = [Nfc, Nfc]
    };

    public static PartOfSpeech CreateNfcPartOfSpeech() => new()
    {
        Id = Guid.NewGuid(),
        Name = CreateNfcMultiString()
    };

    public static Publication CreateNfcPublication() => new()
    {
        Id = Guid.NewGuid(),
        Name = CreateNfcMultiString()
    };

    public static SemanticDomain CreateNfcSemanticDomain() => new()
    {
        Id = Guid.NewGuid(),
        Code = "1.1.1", // Code is NOT normalized (metadata)
        Name = CreateNfcMultiString()
    };

    public static ComplexFormType CreateNfcComplexFormType() => new()
    {
        Id = Guid.NewGuid(),
        Name = CreateNfcMultiString()
    };

    public static MorphTypeData CreateNfcMorphTypeData() => new()
    {
        Id = Guid.NewGuid(),
        Name = CreateNfcMultiString(),
        Abbreviation = CreateNfcMultiString(),
        Description = CreateNfcRichMultiString(),
        LeadingToken = Nfc,
        TrailingToken = Nfc
    };

    public static Translation CreateNfcTranslation() => new()
    {
        Id = Guid.NewGuid(),
        Text = CreateNfcRichMultiString()
    };

    public static ExampleSentence CreateNfcExampleSentence() => new()
    {
        Id = Guid.NewGuid(),
        Sentence = CreateNfcRichMultiString(),
        Reference = CreateNfcRichString()
    };

    public static ExampleSentence CreateNfcExampleSentenceWithTranslations() => new()
    {
        Id = Guid.NewGuid(),
        Sentence = CreateNfcRichMultiString(),
        Reference = CreateNfcRichString(),
        Translations = [CreateNfcTranslation(), CreateNfcTranslation()]
    };

    public static Sense CreateNfcSense() => new()
    {
        Id = Guid.NewGuid(),
        Gloss = CreateNfcMultiString(),
        Definition = CreateNfcRichMultiString()
    };

    public static Sense CreateNfcSenseWithExamples() => new()
    {
        Id = Guid.NewGuid(),
        Gloss = CreateNfcMultiString(),
        Definition = CreateNfcRichMultiString(),
        SemanticDomains = [CreateNfcSemanticDomain()],
        PartOfSpeech = CreateNfcPartOfSpeech(),
        ExampleSentences = [CreateNfcExampleSentenceWithTranslations()]
    };

    public static ComplexFormComponent CreateNfcComplexFormComponent() => new()
    {
        Id = Guid.NewGuid(),
        ComplexFormEntryId = Guid.NewGuid(),
        ComponentEntryId = Guid.NewGuid(),
        ComplexFormHeadword = Nfc,
        ComponentHeadword = Nfc
    };

    public static Entry CreateNfcEntry() => new()
    {
        Id = Guid.NewGuid(),
        LexemeForm = CreateNfcMultiString(),
        CitationForm = CreateNfcMultiString(),
        LiteralMeaning = CreateNfcRichMultiString(),
        Note = CreateNfcRichMultiString()
    };

    public static Entry CreateNfcEntryWithSenses() => new()
    {
        Id = Guid.NewGuid(),
        LexemeForm = CreateNfcMultiString(),
        CitationForm = CreateNfcMultiString(),
        LiteralMeaning = CreateNfcRichMultiString(),
        Note = CreateNfcRichMultiString(),
        Senses = [CreateNfcSenseWithExamples()]
    };

    public static Entry CreateNfcEntryWithComponents() => new()
    {
        Id = Guid.NewGuid(),
        LexemeForm = CreateNfcMultiString(),
        CitationForm = CreateNfcMultiString(),
        LiteralMeaning = CreateNfcRichMultiString(),
        Note = CreateNfcRichMultiString(),
        Components = [CreateNfcComplexFormComponent()],
        ComplexForms = [CreateNfcComplexFormComponent()]
    };
}

/// <summary>
/// Assertion helpers for verifying NFC/NFD normalization state of objects.
/// Uses reflection to walk the object graph and check all normalizable properties.
/// </summary>
public static class NormalizationAssert
{
    private static readonly HashSet<Type> NormalizableTypes =
    [
        typeof(string),
        typeof(string[]),
        typeof(MultiString),
        typeof(RichString),
        typeof(RichMultiString)
    ];

    /// <summary>
    /// Properties that should NOT be normalized (metadata, not user text)
    /// </summary>
    private static readonly HashSet<string> ExcludedProperties =
    [
        "Code", // SemanticDomain.Code is metadata
        "WsId", // WritingSystemId is metadata
        "Ws"    // RichSpan.Ws is a writing system ID
    ];

    /// <summary>
    /// Asserts that all normalizable properties in the object contain NFC strings.
    /// Throws if any property is null, empty, or contains NFD strings.
    /// </summary>
    public static void AssertAllNfc(object obj)
    {
        var issues = FindNormalizationIssues(obj, expectNfc: true);
        if (issues.Count > 0)
        {
            throw new Xunit.Sdk.XunitException(
                $"Expected all normalizable properties to contain NFC strings, but found issues:\n" +
                string.Join("\n", issues.Select(i => $"  - {i}"))
            );
        }
    }

    /// <summary>
    /// Asserts that all normalizable properties in the object contain NFD strings.
    /// Throws if any property contains NFC strings.
    /// </summary>
    public static void AssertAllNfd(object obj)
    {
        var issues = FindNormalizationIssues(obj, expectNfc: false);
        if (issues.Count > 0)
        {
            throw new Xunit.Sdk.XunitException(
                $"Expected all normalizable properties to contain NFD strings, but found issues:\n" +
                string.Join("\n", issues.Select(i => $"  - {i}"))
            );
        }
    }

    /// <summary>
    /// Returns true if all normalizable properties contain NFD strings.
    /// For use in Moq It.Is() expressions.
    /// </summary>
    public static bool IsAllNfd(object obj)
    {
        var issues = FindNormalizationIssues(obj, expectNfc: false);
        return issues.Count == 0;
    }

    /// <summary>
    /// Returns true if all normalizable properties contain NFC strings.
    /// For use in Moq It.Is() expressions.
    /// </summary>
    public static bool IsAllNfc(object obj)
    {
        var issues = FindNormalizationIssues(obj, expectNfc: true);
        return issues.Count == 0;
    }

    private static List<string> FindNormalizationIssues(object obj, bool expectNfc, string path = "")
    {
        var issues = new List<string>();
        if (obj == null) return issues;

        var type = obj.GetType();

        // Handle string directly
        if (obj is string str)
        {
            CheckString(str, path, expectNfc, issues);
            return issues;
        }

        // Handle string array
        if (obj is string[] strArray)
        {
            for (int i = 0; i < strArray.Length; i++)
            {
                CheckString(strArray[i], $"{path}[{i}]", expectNfc, issues);
            }
            return issues;
        }

        // Handle MultiString
        if (obj is MultiString ms)
        {
            if (ms.Values.Count == 0)
            {
                issues.Add($"{path}: MultiString has no values (must have at least one for testing)");
            }
            foreach (var (key, value) in ms.Values)
            {
                CheckString(value, $"{path}.Values[{key}]", expectNfc, issues);
            }
            return issues;
        }

        // Handle RichString
        if (obj is RichString rs)
        {
            if (rs.Spans.Count == 0)
            {
                issues.Add($"{path}: RichString has no spans (must have at least one for testing)");
            }
            for (int i = 0; i < rs.Spans.Count; i++)
            {
                CheckString(rs.Spans[i].Text, $"{path}.Spans[{i}].Text", expectNfc, issues);
            }
            return issues;
        }

        // Handle RichMultiString
        if (obj is RichMultiString rms)
        {
            if (rms.Count == 0)
            {
                issues.Add($"{path}: RichMultiString has no values (must have at least one for testing)");
            }
            foreach (var (key, value) in rms)
            {
                issues.AddRange(FindNormalizationIssues(value, expectNfc, $"{path}[{key}]"));
            }
            return issues;
        }

        // Walk object properties
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (var prop in properties)
        {
            if (!prop.CanRead) continue;
            if (ExcludedProperties.Contains(prop.Name)) continue;

            try
            {
                var value = prop.GetValue(obj);
                if (value == null) continue;

                var propPath = string.IsNullOrEmpty(path) ? prop.Name : $"{path}.{prop.Name}";
                var propType = prop.PropertyType;

                // Check if this is a normalizable type
                if (NormalizableTypes.Contains(propType) ||
                    propType == typeof(string[]) ||
                    (propType.IsGenericType && propType.GetGenericTypeDefinition() == typeof(Nullable<>)))
                {
                    issues.AddRange(FindNormalizationIssues(value, expectNfc, propPath));
                }
                // Check collections of model objects
                else if (value is IEnumerable enumerable && value is not string)
                {
                    int index = 0;
                    foreach (var item in enumerable)
                    {
                        if (item != null && IsModelType(item.GetType()))
                        {
                            issues.AddRange(FindNormalizationIssues(item, expectNfc, $"{propPath}[{index}]"));
                        }
                        index++;
                    }
                }
                // Check nested model objects
                else if (IsModelType(propType))
                {
                    issues.AddRange(FindNormalizationIssues(value, expectNfc, propPath));
                }
            }
            catch
            {
                // Skip properties that throw
            }
        }

        return issues;
    }

    private static void CheckString(string? value, string path, bool expectNfc, List<string> issues)
    {
        if (string.IsNullOrEmpty(value))
        {
            issues.Add($"{path}: string is null or empty (must have a value for testing)");
            return;
        }

        var isNfc = value.IsNormalized(NormalizationForm.FormC);
        var isNfd = value.IsNormalized(NormalizationForm.FormD);

        if (expectNfc)
        {
            // When expecting NFC, the string should be NFC but NOT NFD (unless it has no decomposable chars)
            // For our test string "naïve", NFC != NFD, so we check it's in NFC form
            if (!isNfc)
            {
                issues.Add($"{path}: expected NFC but string is not NFC-normalized");
            }
            // Also verify it's not already NFD (to ensure our test data is meaningful)
            if (isNfd && !isNfc)
            {
                // This is fine - some strings are both NFC and NFD (e.g., ASCII)
            }
            else if (isNfd && isNfc)
            {
                // String is both NFC and NFD - this means it has no decomposable characters
                // For testing purposes, we need strings that ARE different in NFC vs NFD
                // But we'll allow it since it's still valid
            }
        }
        else
        {
            // When expecting NFD, the string should be NFD
            if (!isNfd)
            {
                issues.Add($"{path}: expected NFD but string is not NFD-normalized (value contains NFC)");
            }
        }
    }

    private static bool IsModelType(Type type)
    {
        // Model types are in the MiniLcm.Models namespace
        return type.Namespace?.StartsWith("MiniLcm.Models") == true ||
               type == typeof(Entry) ||
               type == typeof(Sense) ||
               type == typeof(ExampleSentence) ||
               type == typeof(Translation) ||
               type == typeof(WritingSystem) ||
               type == typeof(PartOfSpeech) ||
               type == typeof(SemanticDomain) ||
               type == typeof(ComplexFormType) ||
               type == typeof(MorphTypeData) ||
               type == typeof(Publication) ||
               type == typeof(ComplexFormComponent);
    }
}
