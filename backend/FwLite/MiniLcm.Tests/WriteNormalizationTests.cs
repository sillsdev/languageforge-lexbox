using System.Text.Json;
using MiniLcm.Normalization;
using MiniLcm.SyncHelpers;
using Moq;
using SystemTextJsonPatch.Operations;
using MiniLcm.Tests.Helpers;
using static MiniLcm.Tests.Helpers.NormalizationAssert;

namespace MiniLcm.Tests;

/// <summary>
/// Tests for the MiniLcmApiWriteNormalizationWrapper.
/// These tests verify that all user-entered text is normalized to NFD on write operations.
/// Each test captures the value passed to the underlying API and asserts via
/// <see cref="NormalizationAssert"/>, which reports the failing property path on mismatch.
/// </summary>
public class WriteNormalizationTests
{
    private readonly IMiniLcmApi _mockApi;
    private readonly IMiniLcmApi _normalizingApi;

    public WriteNormalizationTests()
    {
        _mockApi = Mock.Of<IMiniLcmApi>();
        var factory = new MiniLcmApiWriteNormalizationWrapperFactory();
        _normalizingApi = factory.Create(_mockApi);
    }

    #region WritingSystem Tests

    // WritingSystem.{Name, Abbreviation, Font, Exemplars} are plain strings; in liblcm they are
    // LDML-managed by WritingSystemManager rather than stored as TsString, so the wrapper passes
    // them through unchanged (no NFD normalization).

    [Fact]
    public async Task CreateWritingSystem_DoesNotNormalize()
    {
        var ws = NfcTestData.CreateNfcWritingSystem();

        WritingSystem? captured = null;
        Mock.Get(_mockApi)
            .Setup(api => api.CreateWritingSystem(It.IsAny<WritingSystem>(), It.IsAny<BetweenPosition<WritingSystemId?>?>()))
            .Callback<WritingSystem, BetweenPosition<WritingSystemId?>?>((w, _) => captured = w)
            .ReturnsAsync(ws);

        await _normalizingApi.CreateWritingSystem(ws);

        captured.Should().NotBeNull();
        captured.Name.Should().Be(NfcTestData.Nfc);
        captured.Abbreviation.Should().Be(NfcTestData.Nfc);
        captured.Font.Should().Be(NfcTestData.Nfc);
        captured.Exemplars.Should().Equal(NfcTestData.Nfc, NfcTestData.Nfc);
    }

    [Fact]
    public async Task UpdateWritingSystem_BeforeAfter_DoesNotNormalize()
    {
        var before = NfcTestData.CreateNfcWritingSystem();
        var after = NfcTestData.CreateNfcWritingSystem();

        WritingSystem? captured = null;
        Mock.Get(_mockApi)
            .Setup(api => api.UpdateWritingSystem(It.IsAny<WritingSystem>(), It.IsAny<WritingSystem>(), It.IsAny<IMiniLcmApi>()))
            .Callback<WritingSystem, WritingSystem, IMiniLcmApi?>((_, a, _) => captured = a)
            .ReturnsAsync(after);

        await _normalizingApi.UpdateWritingSystem(before, after);

        captured.Should().NotBeNull();
        captured.Name.Should().Be(NfcTestData.Nfc);
        captured.Abbreviation.Should().Be(NfcTestData.Nfc);
        captured.Font.Should().Be(NfcTestData.Nfc);
        captured.Exemplars.Should().Equal(NfcTestData.Nfc, NfcTestData.Nfc);
    }

    #endregion

    #region PartOfSpeech Tests

    [Fact]
    public async Task CreatePartOfSpeech_NormalizesToNfd()
    {
        var pos = NfcTestData.CreateNfcPartOfSpeech();

        PartOfSpeech? captured = null;
        Mock.Get(_mockApi)
            .Setup(api => api.CreatePartOfSpeech(It.IsAny<PartOfSpeech>()))
            .Callback<PartOfSpeech>(p => captured = p)
            .ReturnsAsync(pos);

        await _normalizingApi.CreatePartOfSpeech(pos);

        captured.Should().NotBeNull();
        AssertAllNfd(captured);
    }

    [Fact]
    public async Task UpdatePartOfSpeech_BeforeAfter_NormalizesToNfd()
    {
        var before = NfcTestData.CreateNfcPartOfSpeech();
        var after = NfcTestData.CreateNfcPartOfSpeech();

        PartOfSpeech? captured = null;
        Mock.Get(_mockApi)
            .Setup(api => api.UpdatePartOfSpeech(It.IsAny<PartOfSpeech>(), It.IsAny<PartOfSpeech>(), It.IsAny<IMiniLcmApi>()))
            .Callback<PartOfSpeech, PartOfSpeech, IMiniLcmApi?>((_, a, _) => captured = a)
            .ReturnsAsync(after);

        await _normalizingApi.UpdatePartOfSpeech(before, after);

        captured.Should().NotBeNull();
        AssertAllNfd(captured);
    }

    #endregion

    #region Publication Tests

    [Fact]
    public async Task CreatePublication_NormalizesToNfd()
    {
        var pub = NfcTestData.CreateNfcPublication();

        Publication? captured = null;
        Mock.Get(_mockApi)
            .Setup(api => api.CreatePublication(It.IsAny<Publication>()))
            .Callback<Publication>(p => captured = p)
            .ReturnsAsync(pub);

        await _normalizingApi.CreatePublication(pub);

        captured.Should().NotBeNull();
        AssertAllNfd(captured);
    }

    [Fact]
    public async Task UpdatePublication_BeforeAfter_NormalizesToNfd()
    {
        var before = NfcTestData.CreateNfcPublication();
        var after = NfcTestData.CreateNfcPublication();

        Publication? captured = null;
        Mock.Get(_mockApi)
            .Setup(api => api.UpdatePublication(It.IsAny<Publication>(), It.IsAny<Publication>(), It.IsAny<IMiniLcmApi>()))
            .Callback<Publication, Publication, IMiniLcmApi?>((_, a, _) => captured = a)
            .ReturnsAsync(after);

        await _normalizingApi.UpdatePublication(before, after);

        captured.Should().NotBeNull();
        AssertAllNfd(captured);
    }

    #endregion

    #region SemanticDomain Tests

    [Fact]
    public async Task CreateSemanticDomain_NormalizesToNfd()
    {
        var sd = NfcTestData.CreateNfcSemanticDomain();

        SemanticDomain? captured = null;
        Mock.Get(_mockApi)
            .Setup(api => api.CreateSemanticDomain(It.IsAny<SemanticDomain>()))
            .Callback<SemanticDomain>(s => captured = s)
            .ReturnsAsync(sd);

        await _normalizingApi.CreateSemanticDomain(sd);

        captured.Should().NotBeNull();
        AssertAllNfd(captured);
    }

    [Fact]
    public async Task UpdateSemanticDomain_BeforeAfter_NormalizesToNfd()
    {
        var before = NfcTestData.CreateNfcSemanticDomain();
        var after = NfcTestData.CreateNfcSemanticDomain();

        SemanticDomain? captured = null;
        Mock.Get(_mockApi)
            .Setup(api => api.UpdateSemanticDomain(It.IsAny<SemanticDomain>(), It.IsAny<SemanticDomain>(), It.IsAny<IMiniLcmApi>()))
            .Callback<SemanticDomain, SemanticDomain, IMiniLcmApi?>((_, a, _) => captured = a)
            .ReturnsAsync(after);

        await _normalizingApi.UpdateSemanticDomain(before, after);

        captured.Should().NotBeNull();
        AssertAllNfd(captured);
    }

    [Fact]
    public async Task AddSemanticDomainToSense_NormalizesToNfd()
    {
        var sd = NfcTestData.CreateNfcSemanticDomain();

        SemanticDomain? captured = null;
        Mock.Get(_mockApi)
            .Setup(api => api.AddSemanticDomainToSense(It.IsAny<Guid>(), It.IsAny<SemanticDomain>()))
            .Callback<Guid, SemanticDomain>((_, s) => captured = s)
            .Returns(Task.CompletedTask);

        await _normalizingApi.AddSemanticDomainToSense(Guid.NewGuid(), sd);

        captured.Should().NotBeNull();
        AssertAllNfd(captured);
    }

    [Fact]
    public async Task BulkImportSemanticDomains_NormalizesToNfd()
    {
        var domains = new[] { NfcTestData.CreateNfcSemanticDomain(), NfcTestData.CreateNfcSemanticDomain() };

        var captured = new List<SemanticDomain>();
        Mock.Get(_mockApi)
            .Setup(api => api.BulkImportSemanticDomains(It.IsAny<IAsyncEnumerable<SemanticDomain>>()))
            .Returns(async (IAsyncEnumerable<SemanticDomain> stream) =>
            {
                await foreach (var sd in stream) captured.Add(sd);
            });

        await _normalizingApi.BulkImportSemanticDomains(domains.ToAsyncEnumerable());

        captured.Should().HaveCount(2);
        AssertAllNfd(captured);
    }

    #endregion

    #region ComplexFormType Tests

    [Fact]
    public async Task CreateComplexFormType_NormalizesToNfd()
    {
        var cft = NfcTestData.CreateNfcComplexFormType();

        ComplexFormType? captured = null;
        Mock.Get(_mockApi)
            .Setup(api => api.CreateComplexFormType(It.IsAny<ComplexFormType>()))
            .Callback<ComplexFormType>(c => captured = c)
            .ReturnsAsync(cft);

        await _normalizingApi.CreateComplexFormType(cft);

        captured.Should().NotBeNull();
        AssertAllNfd(captured);
    }

    [Fact]
    public async Task UpdateComplexFormType_BeforeAfter_NormalizesToNfd()
    {
        var before = NfcTestData.CreateNfcComplexFormType();
        var after = NfcTestData.CreateNfcComplexFormType();

        ComplexFormType? captured = null;
        Mock.Get(_mockApi)
            .Setup(api => api.UpdateComplexFormType(It.IsAny<ComplexFormType>(), It.IsAny<ComplexFormType>(), It.IsAny<IMiniLcmApi>()))
            .Callback<ComplexFormType, ComplexFormType, IMiniLcmApi?>((_, a, _) => captured = a)
            .ReturnsAsync(after);

        await _normalizingApi.UpdateComplexFormType(before, after);

        captured.Should().NotBeNull();
        AssertAllNfd(captured);
    }

    #endregion

    #region MorphTypeData Tests

    // MorphTypeData's MultiString/RichMultiString fields (Name, Abbreviation, Description) are normalized
    // to NFD (matching liblcm); LeadingToken/TrailingToken are punctuation markers and pass through unchanged.

    [Fact]
    public async Task CreateMorphTypeData_NormalizesMultiStringsToNfd_AndPassesTokensThrough()
    {
        var mtd = NfcTestData.CreateNfcMorphTypeData();

        MorphTypeData? captured = null;
        Mock.Get(_mockApi)
            .Setup(api => api.CreateMorphTypeData(It.IsAny<MorphTypeData>()))
            .Callback<MorphTypeData>(m => captured = m)
            .ReturnsAsync(mtd);

        await _normalizingApi.CreateMorphTypeData(mtd);

        captured.Should().NotBeNull();
        AssertAllNfd(captured);
        captured.LeadingToken.Should().Be(NfcTestData.Nfc);
        captured.TrailingToken.Should().Be(NfcTestData.Nfc);
    }

    [Fact]
    public async Task UpdateMorphTypeData_BeforeAfter_NormalizesMultiStringsToNfd_AndPassesTokensThrough()
    {
        var before = NfcTestData.CreateNfcMorphTypeData();
        var after = NfcTestData.CreateNfcMorphTypeData();

        MorphTypeData? captured = null;
        Mock.Get(_mockApi)
            .Setup(api => api.UpdateMorphTypeData(It.IsAny<MorphTypeData>(), It.IsAny<MorphTypeData>(), It.IsAny<IMiniLcmApi>()))
            .Callback<MorphTypeData, MorphTypeData, IMiniLcmApi?>((_, a, _) => captured = a)
            .ReturnsAsync(after);

        await _normalizingApi.UpdateMorphTypeData(before, after);

        captured.Should().NotBeNull();
        AssertAllNfd(captured);
        captured.LeadingToken.Should().Be(NfcTestData.Nfc);
        captured.TrailingToken.Should().Be(NfcTestData.Nfc);
    }

    [Fact]
    public async Task UpdateMorphTypeData_JsonPatch_NormalizesMultiString_AndPassesTokensThrough()
    {
        var update = new UpdateObjectInput<MorphTypeData>()
            .Set(m => m.Name, NfcTestData.CreateNfcMultiString())
            .Set(m => m.LeadingToken, NfcTestData.Nfc)
            .Set(m => m.TrailingToken, NfcTestData.Nfc);

        UpdateObjectInput<MorphTypeData>? captured = null;
        Mock.Get(_mockApi)
            .Setup(api => api.UpdateMorphTypeData(It.IsAny<Guid>(), It.IsAny<UpdateObjectInput<MorphTypeData>>()))
            .Callback<Guid, UpdateObjectInput<MorphTypeData>>((_, patch) => captured = patch)
            .ReturnsAsync(NfcTestData.CreateNfcMorphTypeData());

        await _normalizingApi.UpdateMorphTypeData(Guid.NewGuid(), update);

        captured.Should().NotBeNull();
        var byPath = captured.Patch.Operations.ToDictionary(o => o.Path!, o => o.Value);
        AssertAllNfd(byPath["/Name"].Should().BeOfType<MultiString>().Subject);
        byPath["/LeadingToken"].Should().Be(NfcTestData.Nfc);
        byPath["/TrailingToken"].Should().Be(NfcTestData.Nfc);
    }

    #endregion

    #region Entry Tests

    [Fact]
    public async Task CreateEntry_NormalizesToNfd()
    {
        var entry = NfcTestData.CreateNfcEntry();

        Entry? captured = null;
        Mock.Get(_mockApi)
            .Setup(api => api.CreateEntry(It.IsAny<Entry>(), It.IsAny<CreateEntryOptions?>()))
            .Callback<Entry, CreateEntryOptions?>((e, _) => captured = e)
            .ReturnsAsync(entry);

        await _normalizingApi.CreateEntry(entry);

        captured.Should().NotBeNull();
        AssertAllNfd(captured);
    }

    [Fact]
    public async Task UpdateEntry_BeforeAfter_NormalizesToNfd()
    {
        var before = NfcTestData.CreateNfcEntry();
        var after = NfcTestData.CreateNfcEntry();

        Entry? captured = null;
        Mock.Get(_mockApi)
            .Setup(api => api.UpdateEntry(It.IsAny<Entry>(), It.IsAny<Entry>(), It.IsAny<IMiniLcmApi>()))
            .Callback<Entry, Entry, IMiniLcmApi?>((_, a, _) => captured = a)
            .ReturnsAsync(after);

        await _normalizingApi.UpdateEntry(before, after);

        captured.Should().NotBeNull();
        AssertAllNfd(captured);
    }

    [Fact]
    public async Task CreateEntry_WithNestedSenses_NormalizesToNfd()
    {
        var entry = NfcTestData.CreateNfcEntryWithSenses();

        Entry? captured = null;
        Mock.Get(_mockApi)
            .Setup(api => api.CreateEntry(It.IsAny<Entry>(), It.IsAny<CreateEntryOptions?>()))
            .Callback<Entry, CreateEntryOptions?>((e, _) => captured = e)
            .ReturnsAsync(entry);

        await _normalizingApi.CreateEntry(entry);

        captured.Should().NotBeNull();
        AssertAllNfd(captured);
    }

    [Fact]
    public async Task CreateEntry_WithComplexFormComponents_NormalizesToNfd()
    {
        var entry = NfcTestData.CreateNfcEntryWithComponents();

        Entry? captured = null;
        Mock.Get(_mockApi)
            .Setup(api => api.CreateEntry(It.IsAny<Entry>(), It.IsAny<CreateEntryOptions?>()))
            .Callback<Entry, CreateEntryOptions?>((e, _) => captured = e)
            .ReturnsAsync(entry);

        await _normalizingApi.CreateEntry(entry);

        captured.Should().NotBeNull();
        AssertAllNfd(captured);
    }

    [Fact]
    public async Task BulkCreateEntries_NormalizesToNfd()
    {
        var entries = new[] { NfcTestData.CreateNfcEntry(), NfcTestData.CreateNfcEntryWithSenses() };

        var captured = new List<Entry>();
        Mock.Get(_mockApi)
            .Setup(api => api.BulkCreateEntries(It.IsAny<IAsyncEnumerable<Entry>>()))
            .Returns(async (IAsyncEnumerable<Entry> stream) =>
            {
                await foreach (var e in stream) captured.Add(e);
            });

        await _normalizingApi.BulkCreateEntries(entries.ToAsyncEnumerable());

        captured.Should().HaveCount(2);
        AssertAllNfd(captured);
    }

    #endregion

    #region JsonPatch Tests

    [Fact]
    public async Task UpdateEntry_JsonPatch_NormalizesValuesToNfd()
    {
        var update = new UpdateObjectInput<Entry>()
            .Set(e => e.LexemeForm["en"], NfcTestData.Nfc)
            .Set(e => e.CitationForm, NfcTestData.CreateNfcMultiString())
            .Set(e => e.Note["en"], NfcTestData.CreateNfcRichString())
            .Set(e => e.LiteralMeaning, NfcTestData.CreateNfcRichMultiString());

        UpdateObjectInput<Entry>? captured = null;
        Mock.Get(_mockApi)
            .Setup(api => api.UpdateEntry(It.IsAny<Guid>(), It.IsAny<UpdateObjectInput<Entry>>()))
            .Callback<Guid, UpdateObjectInput<Entry>>((_, patch) => captured = patch)
            .ReturnsAsync(NfcTestData.CreateNfcEntry());

        await _normalizingApi.UpdateEntry(Guid.NewGuid(), update);

        captured.Should().NotBeNull();
        captured.Should().NotBeSameAs(update); // rebuilt because Entry path normalizes
        AssertAllPatchValuesNfd(captured);
    }

    [Fact]
    public async Task UpdateEntry_JsonPatch_NormalizesJsonElementString()
    {
        using var document = JsonDocument.Parse($"\"{NfcTestData.Nfc}\"");
        var update = new UpdateObjectInput<Entry>();
        update.Patch.Operations.Add(new Operation<Entry>("replace", "/LexemeForm/en", null, document.RootElement));

        UpdateObjectInput<Entry>? captured = null;
        Mock.Get(_mockApi)
            .Setup(api => api.UpdateEntry(It.IsAny<Guid>(), It.IsAny<UpdateObjectInput<Entry>>()))
            .Callback<Guid, UpdateObjectInput<Entry>>((_, patch) => captured = patch)
            .ReturnsAsync(NfcTestData.CreateNfcEntry());

        await _normalizingApi.UpdateEntry(Guid.NewGuid(), update);

        captured.Should().NotBeNull();
        captured.Patch.Operations.Single().Value
            .Should().BeOfType<string>()
            .Which.Should().Be(NfcTestData.Nfd);
    }

    [Fact]
    public async Task UpdateWritingSystem_JsonPatch_DoesNotNormalize()
    {
        var update = new UpdateObjectInput<WritingSystem>()
            .Set(ws => ws.Name, NfcTestData.Nfc)
            .Set(ws => ws.Exemplars, [NfcTestData.Nfc, NfcTestData.Nfc]);

        UpdateObjectInput<WritingSystem>? captured = null;
        Mock.Get(_mockApi)
            .Setup(api => api.UpdateWritingSystem(It.IsAny<WritingSystemId>(), It.IsAny<WritingSystemType>(),
                It.IsAny<UpdateObjectInput<WritingSystem>>()))
            .Callback<WritingSystemId, WritingSystemType, UpdateObjectInput<WritingSystem>>((_, _, patch) =>
                captured = patch)
            .ReturnsAsync(NfcTestData.CreateNfcWritingSystem());

        await _normalizingApi.UpdateWritingSystem("en", WritingSystemType.Analysis, update);

        captured.Should().BeSameAs(update); // pass-through, not rebuilt
        var byPath = captured.Patch.Operations.ToDictionary(o => o.Path!, o => o.Value);
        byPath.Should().HaveCount(2);
        byPath["/Name"].Should().Be(NfcTestData.Nfc);
        byPath["/Exemplars"].Should().BeOfType<string[]>()
            .Which.Should().Equal(NfcTestData.Nfc, NfcTestData.Nfc);
    }

    /// <summary>
    /// Asserts every non-null operation value in the patch is NFD. Delegates to
    /// <see cref="AssertAllNfd"/>, which already understands string,
    /// string[], MultiString, RichString, and RichMultiString — so failures report a property path.
    /// </summary>
    private static void AssertAllPatchValuesNfd<T>(UpdateObjectInput<T> update) where T : class
    {
        update.Patch.Operations.Should().NotBeEmpty();
        foreach (var op in update.Patch.Operations)
        {
            if (op.Value is not null) AssertAllNfd(op.Value);
        }
    }

    #endregion

    #region ComplexFormComponent Tests

    [Fact]
    public async Task CreateComplexFormComponent_NormalizesToNfd()
    {
        var cfc = NfcTestData.CreateNfcComplexFormComponent();

        ComplexFormComponent? captured = null;
        Mock.Get(_mockApi)
            .Setup(api => api.CreateComplexFormComponent(
                It.IsAny<ComplexFormComponent>(), It.IsAny<BetweenPosition<ComplexFormComponent>?>()))
            .Callback<ComplexFormComponent, BetweenPosition<ComplexFormComponent>?>((c, _) => captured = c)
            .ReturnsAsync(cfc);

        await _normalizingApi.CreateComplexFormComponent(cfc);

        captured.Should().NotBeNull();
        AssertAllNfd(captured);
    }

    #endregion

    #region Sense Tests

    [Fact]
    public async Task CreateSense_NormalizesToNfd()
    {
        var sense = NfcTestData.CreateNfcSense();

        Sense? captured = null;
        Mock.Get(_mockApi)
            .Setup(api => api.CreateSense(It.IsAny<Guid>(), It.IsAny<Sense>(), It.IsAny<BetweenPosition?>()))
            .Callback<Guid, Sense, BetweenPosition?>((_, s, _) => captured = s)
            .ReturnsAsync(sense);

        await _normalizingApi.CreateSense(Guid.NewGuid(), sense);

        captured.Should().NotBeNull();
        AssertAllNfd(captured);
    }

    [Fact]
    public async Task UpdateSense_BeforeAfter_NormalizesToNfd()
    {
        var entryId = Guid.NewGuid();
        var before = NfcTestData.CreateNfcSense();
        var after = NfcTestData.CreateNfcSense();

        Sense? captured = null;
        Mock.Get(_mockApi)
            .Setup(api => api.UpdateSense(It.IsAny<Guid>(), It.IsAny<Sense>(), It.IsAny<Sense>(), It.IsAny<IMiniLcmApi>()))
            .Callback<Guid, Sense, Sense, IMiniLcmApi?>((_, _, a, _) => captured = a)
            .ReturnsAsync(after);

        await _normalizingApi.UpdateSense(entryId, before, after);

        captured.Should().NotBeNull();
        AssertAllNfd(captured);
    }

    [Fact]
    public async Task CreateSense_WithNestedExampleSentences_NormalizesToNfd()
    {
        var sense = NfcTestData.CreateNfcSenseWithExamples();

        Sense? captured = null;
        Mock.Get(_mockApi)
            .Setup(api => api.CreateSense(It.IsAny<Guid>(), It.IsAny<Sense>(), It.IsAny<BetweenPosition?>()))
            .Callback<Guid, Sense, BetweenPosition?>((_, s, _) => captured = s)
            .ReturnsAsync(sense);

        await _normalizingApi.CreateSense(Guid.NewGuid(), sense);

        captured.Should().NotBeNull();
        AssertAllNfd(captured);
    }

    #endregion

    #region ExampleSentence Tests

    [Fact]
    public async Task CreateExampleSentence_NormalizesToNfd()
    {
        var example = NfcTestData.CreateNfcExampleSentence();

        ExampleSentence? captured = null;
        Mock.Get(_mockApi)
            .Setup(api => api.CreateExampleSentence(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<ExampleSentence>(), It.IsAny<BetweenPosition?>()))
            .Callback<Guid, Guid, ExampleSentence, BetweenPosition?>((_, _, e, _) => captured = e)
            .ReturnsAsync(example);

        await _normalizingApi.CreateExampleSentence(Guid.NewGuid(), Guid.NewGuid(), example);

        captured.Should().NotBeNull();
        AssertAllNfd(captured);
    }

    [Fact]
    public async Task UpdateExampleSentence_BeforeAfter_NormalizesToNfd()
    {
        var entryId = Guid.NewGuid();
        var senseId = Guid.NewGuid();
        var before = NfcTestData.CreateNfcExampleSentence();
        var after = NfcTestData.CreateNfcExampleSentence();

        ExampleSentence? captured = null;
        Mock.Get(_mockApi)
            .Setup(api => api.UpdateExampleSentence(
                It.IsAny<Guid>(), It.IsAny<Guid>(),
                It.IsAny<ExampleSentence>(), It.IsAny<ExampleSentence>(),
                It.IsAny<IMiniLcmApi>()))
            .Callback<Guid, Guid, ExampleSentence, ExampleSentence, IMiniLcmApi?>((_, _, _, a, _) => captured = a)
            .ReturnsAsync(after);

        await _normalizingApi.UpdateExampleSentence(entryId, senseId, before, after);

        captured.Should().NotBeNull();
        AssertAllNfd(captured);
    }

    [Fact]
    public async Task CreateExampleSentence_WithTranslations_NormalizesToNfd()
    {
        var example = NfcTestData.CreateNfcExampleSentenceWithTranslations();

        ExampleSentence? captured = null;
        Mock.Get(_mockApi)
            .Setup(api => api.CreateExampleSentence(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<ExampleSentence>(), It.IsAny<BetweenPosition?>()))
            .Callback<Guid, Guid, ExampleSentence, BetweenPosition?>((_, _, e, _) => captured = e)
            .ReturnsAsync(example);

        await _normalizingApi.CreateExampleSentence(Guid.NewGuid(), Guid.NewGuid(), example);

        captured.Should().NotBeNull();
        AssertAllNfd(captured);
    }

    #endregion

    #region Translation Tests

    [Fact]
    public async Task AddTranslation_NormalizesToNfd()
    {
        var translation = NfcTestData.CreateNfcTranslation();

        Translation? captured = null;
        Mock.Get(_mockApi)
            .Setup(api => api.AddTranslation(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Translation>()))
            .Callback<Guid, Guid, Guid, Translation>((_, _, _, t) => captured = t)
            .Returns(Task.CompletedTask);

        await _normalizingApi.AddTranslation(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), translation);

        captured.Should().NotBeNull();
        AssertAllNfd(captured);
    }

    #endregion
}


/// <summary>
/// Tests for NormalizationAssert and the NfcTestData factories that feed it.
/// </summary>
public class NormalizationAssertTests
{
    [Fact]
    public void AllNfcFactories_ProduceNfcData()
    {
        // Single belt-and-braces test that every factory returns NFC data, replacing the
        // per-test AssertNfc(input) preconditions that used to be sprinkled across WriteNormalizationTests.
        AssertAllNfc(NfcTestData.CreateNfcWritingSystem());
        AssertAllNfc(NfcTestData.CreateNfcPartOfSpeech());
        AssertAllNfc(NfcTestData.CreateNfcPublication());
        AssertAllNfc(NfcTestData.CreateNfcSemanticDomain());
        AssertAllNfc(NfcTestData.CreateNfcComplexFormType());
        AssertAllNfc(NfcTestData.CreateNfcMorphTypeData());
        AssertAllNfc(NfcTestData.CreateNfcTranslation());
        AssertAllNfc(NfcTestData.CreateNfcExampleSentence());
        AssertAllNfc(NfcTestData.CreateNfcExampleSentenceWithTranslations());
        AssertAllNfc(NfcTestData.CreateNfcSense());
        AssertAllNfc(NfcTestData.CreateNfcSenseWithExamples());
        AssertAllNfc(NfcTestData.CreateNfcComplexFormComponent());
        AssertAllNfc(NfcTestData.CreateNfcEntry());
        AssertAllNfc(NfcTestData.CreateNfcEntryWithSenses());
        AssertAllNfc(NfcTestData.CreateNfcEntryWithComponents());
    }

    [Fact]
    public void AssertAllNfc_WithNfcData_DoesNotThrow()
    {
        AssertAllNfc(NfcTestData.CreateNfcEntry());
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

        var act = () => AssertAllNfc(entry);

        act.Should().Throw<Xunit.Sdk.XunitException>().WithMessage("*NFC*");
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

        AssertAllNfd(entry);
    }

    [Fact]
    public void AssertAllNfd_WithNfcData_Throws()
    {
        var act = () => AssertAllNfd(NfcTestData.CreateNfcEntry());

        act.Should().Throw<Xunit.Sdk.XunitException>().WithMessage("*NFD*");
    }

    [Fact]
    public void AssertAllNfc_WithEmptyMultiString_Throws()
    {
        var entry = new Entry
        {
            Id = Guid.NewGuid(),
            LexemeForm = [], // Empty - should fail
            CitationForm = NfcTestData.CreateNfcMultiString()
        };

        var act = () => AssertAllNfc(entry);

        act.Should().Throw<Xunit.Sdk.XunitException>().WithMessage("*no values*");
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

        var act = () => AssertAllNfc(entry);

        act.Should().Throw<Xunit.Sdk.XunitException>().WithMessage("*Senses*Gloss*");
    }

    [Fact]
    public void IsAllNfd_WithNfdData_ReturnsTrue()
    {
        var multiString = new MultiString { Values = { { "en", NfcTestData.Nfd } } };

        IsAllNfd(multiString).Should().BeTrue();
    }

    [Fact]
    public void IsAllNfd_WithNfcData_ReturnsFalse()
    {
        IsAllNfd(NfcTestData.CreateNfcMultiString()).Should().BeFalse();
    }
}
