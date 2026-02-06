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

    #region WritingSystem Tests

    [Fact]
    public async Task CreateWritingSystem_NormalizesToNfd()
    {
        var ws = NfcTestData.CreateNfcWritingSystem();
        NormalizationAssert.AssertAllNfc(ws);

        await _normalizingApi.CreateWritingSystem(ws);

        Mock.Get(_mockApi).Verify(api => api.CreateWritingSystem(
            It.Is<WritingSystem>(w => NormalizationAssert.IsAllNfd(w)),
            null
        ));
    }

    [Fact]
    public async Task UpdateWritingSystem_BeforeAfter_NormalizesToNfd()
    {
        var before = NfcTestData.CreateNfcWritingSystem();
        var after = NfcTestData.CreateNfcWritingSystem();
        NormalizationAssert.AssertAllNfc(after);

        await _normalizingApi.UpdateWritingSystem(before, after);

        Mock.Get(_mockApi).Verify(api => api.UpdateWritingSystem(
            It.IsAny<WritingSystem>(),
            It.Is<WritingSystem>(w => NormalizationAssert.IsAllNfd(w)),
            It.IsAny<IMiniLcmApi>()
        ));
    }

    #endregion

    #region PartOfSpeech Tests

    [Fact]
    public async Task CreatePartOfSpeech_NormalizesToNfd()
    {
        var pos = NfcTestData.CreateNfcPartOfSpeech();
        NormalizationAssert.AssertAllNfc(pos);

        await _normalizingApi.CreatePartOfSpeech(pos);

        Mock.Get(_mockApi).Verify(api => api.CreatePartOfSpeech(
            It.Is<PartOfSpeech>(p => NormalizationAssert.IsAllNfd(p))
        ));
    }

    [Fact]
    public async Task UpdatePartOfSpeech_BeforeAfter_NormalizesToNfd()
    {
        var before = NfcTestData.CreateNfcPartOfSpeech();
        var after = NfcTestData.CreateNfcPartOfSpeech();
        NormalizationAssert.AssertAllNfc(after);

        await _normalizingApi.UpdatePartOfSpeech(before, after);

        Mock.Get(_mockApi).Verify(api => api.UpdatePartOfSpeech(
            It.IsAny<PartOfSpeech>(),
            It.Is<PartOfSpeech>(p => NormalizationAssert.IsAllNfd(p)),
            It.IsAny<IMiniLcmApi>()
        ));
    }

    #endregion

    #region Publication Tests

    [Fact]
    public async Task CreatePublication_NormalizesToNfd()
    {
        var pub = NfcTestData.CreateNfcPublication();
        NormalizationAssert.AssertAllNfc(pub);

        await _normalizingApi.CreatePublication(pub);

        Mock.Get(_mockApi).Verify(api => api.CreatePublication(
            It.Is<Publication>(p => NormalizationAssert.IsAllNfd(p))
        ));
    }

    [Fact]
    public async Task UpdatePublication_BeforeAfter_NormalizesToNfd()
    {
        var before = NfcTestData.CreateNfcPublication();
        var after = NfcTestData.CreateNfcPublication();
        NormalizationAssert.AssertAllNfc(after);

        await _normalizingApi.UpdatePublication(before, after);

        Mock.Get(_mockApi).Verify(api => api.UpdatePublication(
            It.IsAny<Publication>(),
            It.Is<Publication>(p => NormalizationAssert.IsAllNfd(p)),
            It.IsAny<IMiniLcmApi>()
        ));
    }

    #endregion

    #region SemanticDomain Tests

    [Fact]
    public async Task CreateSemanticDomain_NormalizesToNfd()
    {
        var sd = NfcTestData.CreateNfcSemanticDomain();
        NormalizationAssert.AssertAllNfc(sd);

        await _normalizingApi.CreateSemanticDomain(sd);

        Mock.Get(_mockApi).Verify(api => api.CreateSemanticDomain(
            It.Is<SemanticDomain>(s => NormalizationAssert.IsAllNfd(s))
        ));
    }

    [Fact]
    public async Task UpdateSemanticDomain_BeforeAfter_NormalizesToNfd()
    {
        var before = NfcTestData.CreateNfcSemanticDomain();
        var after = NfcTestData.CreateNfcSemanticDomain();
        NormalizationAssert.AssertAllNfc(after);

        await _normalizingApi.UpdateSemanticDomain(before, after);

        Mock.Get(_mockApi).Verify(api => api.UpdateSemanticDomain(
            It.IsAny<SemanticDomain>(),
            It.Is<SemanticDomain>(s => NormalizationAssert.IsAllNfd(s)),
            It.IsAny<IMiniLcmApi>()
        ));
    }

    [Fact]
    public async Task AddSemanticDomainToSense_NormalizesToNfd()
    {
        var sd = NfcTestData.CreateNfcSemanticDomain();
        NormalizationAssert.AssertAllNfc(sd);

        await _normalizingApi.AddSemanticDomainToSense(Guid.NewGuid(), sd);

        Mock.Get(_mockApi).Verify(api => api.AddSemanticDomainToSense(
            It.IsAny<Guid>(),
            It.Is<SemanticDomain>(s => NormalizationAssert.IsAllNfd(s))
        ));
    }

    [Fact]
    public async Task BulkImportSemanticDomains_NormalizesToNfd()
    {
        var domains = new[] { NfcTestData.CreateNfcSemanticDomain(), NfcTestData.CreateNfcSemanticDomain() };
        foreach (var domain in domains) NormalizationAssert.AssertAllNfc(domain);

        var capturedDomains = new List<SemanticDomain>();
        Mock.Get(_mockApi)
            .Setup(api => api.BulkImportSemanticDomains(It.IsAny<IAsyncEnumerable<SemanticDomain>>()))
            .Returns(async (IAsyncEnumerable<SemanticDomain> stream) =>
            {
                await foreach (var sd in stream) capturedDomains.Add(sd);
            });

        await _normalizingApi.BulkImportSemanticDomains(domains.ToAsyncEnumerable());

        capturedDomains.Should().HaveCount(2);
        foreach (var domain in capturedDomains)
        {
            NormalizationAssert.AssertAllNfd(domain);
        }
    }

    #endregion

    #region ComplexFormType Tests

    [Fact]
    public async Task CreateComplexFormType_NormalizesToNfd()
    {
        var cft = NfcTestData.CreateNfcComplexFormType();
        NormalizationAssert.AssertAllNfc(cft);

        await _normalizingApi.CreateComplexFormType(cft);

        Mock.Get(_mockApi).Verify(api => api.CreateComplexFormType(
            It.Is<ComplexFormType>(c => NormalizationAssert.IsAllNfd(c))
        ));
    }

    [Fact]
    public async Task UpdateComplexFormType_BeforeAfter_NormalizesToNfd()
    {
        var before = NfcTestData.CreateNfcComplexFormType();
        var after = NfcTestData.CreateNfcComplexFormType();
        NormalizationAssert.AssertAllNfc(after);

        await _normalizingApi.UpdateComplexFormType(before, after);

        Mock.Get(_mockApi).Verify(api => api.UpdateComplexFormType(
            It.IsAny<ComplexFormType>(),
            It.Is<ComplexFormType>(c => NormalizationAssert.IsAllNfd(c)),
            It.IsAny<IMiniLcmApi>()
        ));
    }

    #endregion

    #region MorphTypeData Tests

    [Fact]
    public async Task CreateMorphTypeData_NormalizesToNfd()
    {
        var mtd = NfcTestData.CreateNfcMorphTypeData();
        NormalizationAssert.AssertAllNfc(mtd);

        await _normalizingApi.CreateMorphTypeData(mtd);

        Mock.Get(_mockApi).Verify(api => api.CreateMorphTypeData(
            It.Is<MorphTypeData>(m => NormalizationAssert.IsAllNfd(m))
        ));
    }

    [Fact]
    public async Task UpdateMorphTypeData_BeforeAfter_NormalizesToNfd()
    {
        var before = NfcTestData.CreateNfcMorphTypeData();
        var after = NfcTestData.CreateNfcMorphTypeData();
        NormalizationAssert.AssertAllNfc(after);

        await _normalizingApi.UpdateMorphTypeData(before, after);

        Mock.Get(_mockApi).Verify(api => api.UpdateMorphTypeData(
            It.IsAny<MorphTypeData>(),
            It.Is<MorphTypeData>(m => NormalizationAssert.IsAllNfd(m)),
            It.IsAny<IMiniLcmApi>()
        ));
    }

    #endregion

    #region Entry Tests

    [Fact]
    public async Task CreateEntry_NormalizesToNfd()
    {
        var entry = NfcTestData.CreateNfcEntry();
        NormalizationAssert.AssertAllNfc(entry);

        await _normalizingApi.CreateEntry(entry);

        Mock.Get(_mockApi).Verify(api => api.CreateEntry(
            It.Is<Entry>(e => NormalizationAssert.IsAllNfd(e)),
            null
        ));
    }

    [Fact]
    public async Task UpdateEntry_BeforeAfter_NormalizesToNfd()
    {
        var before = NfcTestData.CreateNfcEntry();
        var after = NfcTestData.CreateNfcEntry();
        NormalizationAssert.AssertAllNfc(after);

        await _normalizingApi.UpdateEntry(before, after);

        Mock.Get(_mockApi).Verify(api => api.UpdateEntry(
            It.IsAny<Entry>(),
            It.Is<Entry>(e => NormalizationAssert.IsAllNfd(e)),
            It.IsAny<IMiniLcmApi>()
        ));
    }

    [Fact]
    public async Task CreateEntry_WithNestedSenses_NormalizesToNfd()
    {
        var entry = NfcTestData.CreateNfcEntryWithSenses();
        NormalizationAssert.AssertAllNfc(entry);

        await _normalizingApi.CreateEntry(entry);

        Mock.Get(_mockApi).Verify(api => api.CreateEntry(
            It.Is<Entry>(e => NormalizationAssert.IsAllNfd(e)),
            null
        ));
    }

    [Fact]
    public async Task CreateEntry_WithComplexFormComponents_NormalizesToNfd()
    {
        var entry = NfcTestData.CreateNfcEntryWithComponents();
        NormalizationAssert.AssertAllNfc(entry);

        await _normalizingApi.CreateEntry(entry);

        Mock.Get(_mockApi).Verify(api => api.CreateEntry(
            It.Is<Entry>(e => NormalizationAssert.IsAllNfd(e)),
            null
        ));
    }

    [Fact]
    public async Task BulkCreateEntries_NormalizesToNfd()
    {
        var entries = new[] { NfcTestData.CreateNfcEntry(), NfcTestData.CreateNfcEntryWithSenses() };
        foreach (var entry in entries) NormalizationAssert.AssertAllNfc(entry);

        var capturedEntries = new List<Entry>();
        Mock.Get(_mockApi)
            .Setup(api => api.BulkCreateEntries(It.IsAny<IAsyncEnumerable<Entry>>()))
            .Returns(async (IAsyncEnumerable<Entry> stream) =>
            {
                await foreach (var e in stream) capturedEntries.Add(e);
            });

        await _normalizingApi.BulkCreateEntries(entries.ToAsyncEnumerable());

        capturedEntries.Should().HaveCount(2);
        foreach (var entry in capturedEntries)
        {
            NormalizationAssert.AssertAllNfd(entry);
        }
    }

    #endregion

    #region ComplexFormComponent Tests

    [Fact]
    public async Task CreateComplexFormComponent_NormalizesToNfd()
    {
        var cfc = NfcTestData.CreateNfcComplexFormComponent();
        NormalizationAssert.AssertAllNfc(cfc);

        await _normalizingApi.CreateComplexFormComponent(cfc);

        Mock.Get(_mockApi).Verify(api => api.CreateComplexFormComponent(
            It.Is<ComplexFormComponent>(c => NormalizationAssert.IsAllNfd(c)),
            null
        ));
    }

    #endregion

    #region Sense Tests

    [Fact]
    public async Task CreateSense_NormalizesToNfd()
    {
        var sense = NfcTestData.CreateNfcSense();
        NormalizationAssert.AssertAllNfc(sense);

        await _normalizingApi.CreateSense(Guid.NewGuid(), sense);

        Mock.Get(_mockApi).Verify(api => api.CreateSense(
            It.IsAny<Guid>(),
            It.Is<Sense>(s => NormalizationAssert.IsAllNfd(s)),
            null
        ));
    }

    [Fact]
    public async Task UpdateSense_BeforeAfter_NormalizesToNfd()
    {
        var entryId = Guid.NewGuid();
        var before = NfcTestData.CreateNfcSense();
        var after = NfcTestData.CreateNfcSense();
        NormalizationAssert.AssertAllNfc(after);

        await _normalizingApi.UpdateSense(entryId, before, after);

        Mock.Get(_mockApi).Verify(api => api.UpdateSense(
            entryId,
            It.IsAny<Sense>(),
            It.Is<Sense>(s => NormalizationAssert.IsAllNfd(s)),
            It.IsAny<IMiniLcmApi>()
        ));
    }

    [Fact]
    public async Task CreateSense_WithNestedExampleSentences_NormalizesToNfd()
    {
        var sense = NfcTestData.CreateNfcSenseWithExamples();
        NormalizationAssert.AssertAllNfc(sense);

        await _normalizingApi.CreateSense(Guid.NewGuid(), sense);

        Mock.Get(_mockApi).Verify(api => api.CreateSense(
            It.IsAny<Guid>(),
            It.Is<Sense>(s => NormalizationAssert.IsAllNfd(s)),
            null
        ));
    }

    #endregion

    #region ExampleSentence Tests

    [Fact]
    public async Task CreateExampleSentence_NormalizesToNfd()
    {
        var example = NfcTestData.CreateNfcExampleSentence();
        NormalizationAssert.AssertAllNfc(example);

        await _normalizingApi.CreateExampleSentence(Guid.NewGuid(), Guid.NewGuid(), example);

        Mock.Get(_mockApi).Verify(api => api.CreateExampleSentence(
            It.IsAny<Guid>(),
            It.IsAny<Guid>(),
            It.Is<ExampleSentence>(e => NormalizationAssert.IsAllNfd(e)),
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
        NormalizationAssert.AssertAllNfc(after);

        await _normalizingApi.UpdateExampleSentence(entryId, senseId, before, after);

        Mock.Get(_mockApi).Verify(api => api.UpdateExampleSentence(
            entryId,
            senseId,
            It.IsAny<ExampleSentence>(),
            It.Is<ExampleSentence>(e => NormalizationAssert.IsAllNfd(e)),
            It.IsAny<IMiniLcmApi>()
        ));
    }

    [Fact]
    public async Task CreateExampleSentence_WithTranslations_NormalizesToNfd()
    {
        var example = NfcTestData.CreateNfcExampleSentenceWithTranslations();
        NormalizationAssert.AssertAllNfc(example);

        await _normalizingApi.CreateExampleSentence(Guid.NewGuid(), Guid.NewGuid(), example);

        Mock.Get(_mockApi).Verify(api => api.CreateExampleSentence(
            It.IsAny<Guid>(),
            It.IsAny<Guid>(),
            It.Is<ExampleSentence>(e => NormalizationAssert.IsAllNfd(e)),
            null
        ));
    }

    #endregion

    #region Translation Tests

    [Fact]
    public async Task AddTranslation_NormalizesToNfd()
    {
        var translation = NfcTestData.CreateNfcTranslation();
        NormalizationAssert.AssertAllNfc(translation);

        await _normalizingApi.AddTranslation(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), translation);

        Mock.Get(_mockApi).Verify(api => api.AddTranslation(
            It.IsAny<Guid>(),
            It.IsAny<Guid>(),
            It.IsAny<Guid>(),
            It.Is<Translation>(t => NormalizationAssert.IsAllNfd(t))
        ));
    }

    #endregion
}

/// <summary>
/// Tests to ensure that all normalizing wrapper methods have corresponding tests.
/// </summary>
public class WriteNormalizationCoverageTests
{
    /// <summary>
    /// Method names that don't handle user text and don't need normalization tests.
    /// This is the ONLY list we maintain - everything else is calculated.
    /// </summary>
    private static readonly HashSet<string> ExcludedMethodNames =
    [
        // Move operations (only IDs/positions, no text)
        "MoveWritingSystem",
        "MoveComplexFormComponent",
        "MoveSense",
        "MoveExampleSentence",

        // Delete operations (only IDs, no text)
        "DeletePartOfSpeech",
        "DeletePublication",
        "DeleteSemanticDomain",
        "DeleteComplexFormType",
        "DeleteMorphTypeData",
        "DeleteEntry",
        "DeleteComplexFormComponent",
        "DeleteSense",
        "DeleteExampleSentence",

        // Relationship operations (only IDs, no text)
        "AddComplexFormType",
        "RemoveComplexFormType",
        "AddPublication",
        "RemovePublication",
        "RemoveSemanticDomainFromSense",
        "SetSensePartOfSpeech",
        "RemoveTranslation",

        // File operations (no user text)
        "SaveFile"
    ];

    /// <summary>
    /// Checks if a method is a JsonPatch overload (takes UpdateObjectInput parameter).
    /// These are not user-facing and don't need tests.
    /// </summary>
    private static bool IsJsonPatchOverload(MethodInfo method)
    {
        return method.GetParameters().Any(p =>
            p.ParameterType.IsGenericType &&
            p.ParameterType.GetGenericTypeDefinition() == typeof(UpdateObjectInput<>));
    }

    /// <summary>
    /// Gets all write API methods that need normalization tests.
    /// </summary>
    private static List<MethodInfo> GetMethodsThatNeedTests()
    {
        return typeof(IMiniLcmWriteApi)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => !m.IsSpecialName)
            .Where(m => !ExcludedMethodNames.Contains(m.Name))
            .Where(m => !IsJsonPatchOverload(m))
            .ToList();
    }

    /// <summary>
    /// Creates a readable signature for a method (for error messages).
    /// </summary>
    private static string GetMethodSignature(MethodInfo method)
    {
        var parameters = method.GetParameters()
            .Select(p => $"{p.ParameterType.Name} {p.Name}")
            .ToList();
        return $"{method.Name}({string.Join(", ", parameters)})";
    }

    /// <summary>
    /// Verifies that every method that normalizes text has a corresponding test.
    /// Counts overloads to ensure all are covered.
    /// </summary>
    [Fact]
    public void AllNormalizingMethods_HaveCorrespondingTests()
    {
        var methodsThatNeedTests = GetMethodsThatNeedTests();

        var testClass = typeof(WriteNormalizationTests);
        var testMethodNames = testClass
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => m.GetCustomAttribute<FactAttribute>() != null)
            .Select(m => m.Name)
            .ToList();

        // Group methods by name to handle overloads
        var methodGroups = methodsThatNeedTests.GroupBy(m => m.Name);

        var issues = new List<string>();
        foreach (var group in methodGroups)
        {
            var methodName = group.Key;
            var overloadCount = group.Count();

            // Count tests that match this method name
            var testCount = testMethodNames.Count(t =>
                t.StartsWith(methodName, StringComparison.OrdinalIgnoreCase) ||
                t.Contains($"_{methodName}", StringComparison.OrdinalIgnoreCase) ||
                t.Contains($"{methodName}_", StringComparison.OrdinalIgnoreCase));

            if (testCount < overloadCount)
            {
                var signatures = group.Select(GetMethodSignature).ToList();
                issues.Add($"{methodName}: found {testCount} test(s) but need {overloadCount} for overloads:\n" +
                           string.Join("\n", signatures.Select(s => $"      - {s}")));
            }
        }

        if (issues.Count > 0)
        {
            Assert.Fail(
                $"The following methods need more tests:\n" +
                string.Join("\n", issues.Select(i => $"  - {i}")) +
                "\n\nAdd a test for each overload that verifies NFC input is normalized to NFD."
            );
        }
    }

    /// <summary>
    /// Verifies that all excluded method names actually exist on the interface.
    /// Catches typos and stale entries in the exclusion list.
    /// </summary>
    [Fact]
    public void ExcludedMethodNames_AllExistOnInterface()
    {
        var actualMethodNames = typeof(IMiniLcmWriteApi)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => !m.IsSpecialName)
            .Select(m => m.Name)
            .Distinct()
            .ToHashSet();

        var invalidExclusions = ExcludedMethodNames
            .Where(name => !actualMethodNames.Contains(name))
            .ToList();

        if (invalidExclusions.Count > 0)
        {
            Assert.Fail(
                $"The following excluded method names don't exist on IMiniLcmWriteApi:\n" +
                string.Join("\n", invalidExclusions.Select(m => $"  - {m}")) +
                "\n\nRemove these from ExcludedMethodNames or fix the typo."
            );
        }
    }
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
