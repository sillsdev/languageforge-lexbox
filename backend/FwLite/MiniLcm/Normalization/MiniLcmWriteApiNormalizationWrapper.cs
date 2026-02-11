using MiniLcm.Media;
using MiniLcm.Models;
using MiniLcm.SyncHelpers;
using MiniLcm.Wrappers;

namespace MiniLcm.Normalization;

public class MiniLcmWriteApiNormalizationWrapperFactory : IMiniLcmWrapperFactory
{
    public IMiniLcmApi Create(IMiniLcmApi api, IProjectIdentifier _unused) => Create(api);

    public IMiniLcmApi Create(IMiniLcmApi api)
    {
        return new MiniLcmWriteApiNormalizationWrapper(api);
    }
}

/// <summary>
/// Normalizes all user-entered text to NFD on write operations.
/// Read operations are forwarded automatically via BeaKona.AutoInterface.
/// Write operations MUST be manually implemented - the compiler will fail if any are missing.
/// </summary>
public partial class MiniLcmWriteApiNormalizationWrapper(IMiniLcmApi api) : IMiniLcmApi
{
    private readonly IMiniLcmApi _api = api;

    // BeaKona.AutoInterface only forwards IMiniLcmReadApi methods.
    // IMiniLcmWriteApi methods are NOT auto-forwarded, ensuring compile-time
    // enforcement that all write methods are manually implemented below.
    [BeaKona.AutoInterface]
    private IMiniLcmReadApi ReadApi => _api;

    // ********** IMiniLcmWriteApi Manual Implementations **********
    // All write methods are implemented manually to ensure NFD normalization
    // and guarantee compile-time coverage of all write operations.

    #region WritingSystem

    public async Task<WritingSystem> CreateWritingSystem(WritingSystem writingSystem, BetweenPosition<WritingSystemId?>? between = null)
    {
        var normalized = NormalizeWritingSystem(writingSystem);
        return await _api.CreateWritingSystem(normalized, between);
    }

    // JsonPatch: passed through without normalization (not user-facing, frontend uses object-based Update)
    public Task<WritingSystem> UpdateWritingSystem(WritingSystemId id, WritingSystemType type, UpdateObjectInput<WritingSystem> update)
        => _api.UpdateWritingSystem(id, type, update);

    public async Task<WritingSystem> UpdateWritingSystem(WritingSystem before, WritingSystem after, IMiniLcmApi? api = null)
    {
        var normalized = NormalizeWritingSystem(after);
        return await _api.UpdateWritingSystem(before, normalized, api ?? this);
    }

    public Task MoveWritingSystem(WritingSystemId id, WritingSystemType type, BetweenPosition<WritingSystemId?> between)
    {
        return _api.MoveWritingSystem(id, type, between);
    }

    private static WritingSystem NormalizeWritingSystem(WritingSystem ws)
    {
        return ws with
        {
            Name = StringNormalizer.Normalize(ws.Name) ?? ws.Name,
            Abbreviation = StringNormalizer.Normalize(ws.Abbreviation) ?? ws.Abbreviation,
            Font = StringNormalizer.Normalize(ws.Font) ?? ws.Font,
            Exemplars = StringNormalizer.Normalize(ws.Exemplars)
        };
    }

    #endregion

    #region PartOfSpeech

    public async Task<PartOfSpeech> CreatePartOfSpeech(PartOfSpeech partOfSpeech)
    {
        var normalized = NormalizePartOfSpeech(partOfSpeech);
        return await _api.CreatePartOfSpeech(normalized);
    }

    // JsonPatch: passed through without normalization (not user-facing)
    public Task<PartOfSpeech> UpdatePartOfSpeech(Guid id, UpdateObjectInput<PartOfSpeech> update)
        => _api.UpdatePartOfSpeech(id, update);

    public async Task<PartOfSpeech> UpdatePartOfSpeech(PartOfSpeech before, PartOfSpeech after, IMiniLcmApi? api = null)
    {
        var normalized = NormalizePartOfSpeech(after);
        return await _api.UpdatePartOfSpeech(before, normalized, api ?? this);
    }

    public Task DeletePartOfSpeech(Guid id)
    {
        return _api.DeletePartOfSpeech(id);
    }

    private static PartOfSpeech NormalizePartOfSpeech(PartOfSpeech pos)
    {
        return new PartOfSpeech
        {
            Id = pos.Id,
            Name = StringNormalizer.Normalize(pos.Name),
            DeletedAt = pos.DeletedAt,
            Predefined = pos.Predefined
        };
    }

    #endregion

    #region Publication

    public async Task<Publication> CreatePublication(Publication pub)
    {
        var normalized = NormalizePublication(pub);
        return await _api.CreatePublication(normalized);
    }

    // JsonPatch: passed through without normalization (not user-facing)
    public Task<Publication> UpdatePublication(Guid id, UpdateObjectInput<Publication> update)
        => _api.UpdatePublication(id, update);

    public async Task<Publication> UpdatePublication(Publication before, Publication after, IMiniLcmApi? api = null)
    {
        var normalized = NormalizePublication(after);
        return await _api.UpdatePublication(before, normalized, api ?? this);
    }

    public Task DeletePublication(Guid id)
    {
        return _api.DeletePublication(id);
    }

    private static Publication NormalizePublication(Publication pub)
    {
        return new Publication
        {
            Id = pub.Id,
            Name = StringNormalizer.Normalize(pub.Name),
            DeletedAt = pub.DeletedAt
        };
    }

    #endregion

    #region SemanticDomain

    public async Task<SemanticDomain> CreateSemanticDomain(SemanticDomain semanticDomain)
    {
        var normalized = NormalizeSemanticDomain(semanticDomain);
        return await _api.CreateSemanticDomain(normalized);
    }

    // JsonPatch: passed through without normalization (not user-facing)
    public Task<SemanticDomain> UpdateSemanticDomain(Guid id, UpdateObjectInput<SemanticDomain> update)
        => _api.UpdateSemanticDomain(id, update);

    public async Task<SemanticDomain> UpdateSemanticDomain(SemanticDomain before, SemanticDomain after, IMiniLcmApi? api = null)
    {
        var normalized = NormalizeSemanticDomain(after);
        return await _api.UpdateSemanticDomain(before, normalized, api ?? this);
    }

    public Task DeleteSemanticDomain(Guid id)
    {
        return _api.DeleteSemanticDomain(id);
    }

    private static SemanticDomain NormalizeSemanticDomain(SemanticDomain sd)
    {
        return new SemanticDomain
        {
            Id = sd.Id,
            Name = StringNormalizer.Normalize(sd.Name),
            Code = sd.Code, // Code is metadata, not user text
            DeletedAt = sd.DeletedAt,
            Predefined = sd.Predefined
        };
    }

    #endregion

    #region ComplexFormType

    public async Task<ComplexFormType> CreateComplexFormType(ComplexFormType complexFormType)
    {
        var normalized = NormalizeComplexFormType(complexFormType);
        return await _api.CreateComplexFormType(normalized);
    }

    // JsonPatch: passed through without normalization (not user-facing)
    public Task<ComplexFormType> UpdateComplexFormType(Guid id, UpdateObjectInput<ComplexFormType> update)
        => _api.UpdateComplexFormType(id, update);

    public async Task<ComplexFormType> UpdateComplexFormType(ComplexFormType before, ComplexFormType after, IMiniLcmApi? api = null)
    {
        var normalized = NormalizeComplexFormType(after);
        return await _api.UpdateComplexFormType(before, normalized, api ?? this);
    }

    public Task DeleteComplexFormType(Guid id)
    {
        return _api.DeleteComplexFormType(id);
    }

    private static ComplexFormType NormalizeComplexFormType(ComplexFormType cft)
    {
        return cft with
        {
            Name = StringNormalizer.Normalize(cft.Name)
        };
    }

    #endregion

    #region MorphType

    public async Task<MorphTypeData> CreateMorphTypeData(MorphTypeData morphType)
    {
        var normalized = NormalizeMorphTypeData(morphType);
        return await _api.CreateMorphTypeData(normalized);
    }

    // JsonPatch: passed through without normalization (not user-facing)
    public Task<MorphTypeData> UpdateMorphTypeData(Guid id, UpdateObjectInput<MorphTypeData> update)
        => _api.UpdateMorphTypeData(id, update);

    public async Task<MorphTypeData> UpdateMorphTypeData(MorphTypeData before, MorphTypeData after, IMiniLcmApi? api = null)
    {
        var normalized = NormalizeMorphTypeData(after);
        return await _api.UpdateMorphTypeData(before, normalized, api ?? this);
    }

    public Task DeleteMorphTypeData(Guid id)
    {
        return _api.DeleteMorphTypeData(id);
    }

    private static MorphTypeData NormalizeMorphTypeData(MorphTypeData mtd)
    {
        return new MorphTypeData
        {
            Id = mtd.Id,
            MorphType = mtd.MorphType,
            Name = StringNormalizer.Normalize(mtd.Name),
            Abbreviation = StringNormalizer.Normalize(mtd.Abbreviation),
            Description = StringNormalizer.Normalize(mtd.Description),
            LeadingToken = StringNormalizer.Normalize(mtd.LeadingToken) ?? mtd.LeadingToken,
            TrailingToken = StringNormalizer.Normalize(mtd.TrailingToken) ?? mtd.TrailingToken,
            SecondaryOrder = mtd.SecondaryOrder,
            DeletedAt = mtd.DeletedAt
        };
    }

    #endregion

    #region Entry

    public async Task<Entry> CreateEntry(Entry entry, CreateEntryOptions? options = null)
    {
        var normalized = NormalizeEntry(entry);
        return await _api.CreateEntry(normalized, options);
    }

    // JsonPatch: passed through without normalization (not user-facing)
    public Task<Entry> UpdateEntry(Guid id, UpdateObjectInput<Entry> update)
        => _api.UpdateEntry(id, update);

    public async Task<Entry> UpdateEntry(Entry before, Entry after, IMiniLcmApi? api = null)
    {
        var normalized = NormalizeEntry(after);
        return await _api.UpdateEntry(before, normalized, api ?? this);
    }

    public Task DeleteEntry(Guid id)
    {
        return _api.DeleteEntry(id);
    }

    public async Task<ComplexFormComponent> CreateComplexFormComponent(ComplexFormComponent complexFormComponent, BetweenPosition<ComplexFormComponent>? position = null)
    {
        var normalized = NormalizeComplexFormComponent(complexFormComponent);
        return await _api.CreateComplexFormComponent(normalized, position);
    }

    public Task MoveComplexFormComponent(ComplexFormComponent complexFormComponent, BetweenPosition<ComplexFormComponent> between)
    {
        // No text to normalize in move operation
        return _api.MoveComplexFormComponent(complexFormComponent, between);
    }

    public Task DeleteComplexFormComponent(ComplexFormComponent complexFormComponent)
    {
        return _api.DeleteComplexFormComponent(complexFormComponent);
    }

    public Task AddComplexFormType(Guid entryId, Guid complexFormTypeId)
    {
        return _api.AddComplexFormType(entryId, complexFormTypeId);
    }

    public Task RemoveComplexFormType(Guid entryId, Guid complexFormTypeId)
    {
        return _api.RemoveComplexFormType(entryId, complexFormTypeId);
    }

    public Task AddPublication(Guid entryId, Guid publicationId)
    {
        return _api.AddPublication(entryId, publicationId);
    }

    public Task RemovePublication(Guid entryId, Guid publicationId)
    {
        return _api.RemovePublication(entryId, publicationId);
    }

    private static Entry NormalizeEntry(Entry entry)
    {
        return new Entry
        {
            Id = entry.Id,
            DeletedAt = entry.DeletedAt,
            LexemeForm = StringNormalizer.Normalize(entry.LexemeForm),
            CitationForm = StringNormalizer.Normalize(entry.CitationForm),
            LiteralMeaning = StringNormalizer.Normalize(entry.LiteralMeaning),
            Note = StringNormalizer.Normalize(entry.Note),
            MorphType = entry.MorphType,
            Senses = entry.Senses.Select(NormalizeSense).ToList(),
            Components = entry.Components.Select(NormalizeComplexFormComponent).ToList(),
            ComplexForms = entry.ComplexForms.Select(NormalizeComplexFormComponent).ToList(),
            ComplexFormTypes = entry.ComplexFormTypes,
            PublishIn = entry.PublishIn
        };
    }

    private static ComplexFormComponent NormalizeComplexFormComponent(ComplexFormComponent cfc)
    {
        return cfc with
        {
            ComplexFormHeadword = StringNormalizer.Normalize(cfc.ComplexFormHeadword),
            ComponentHeadword = StringNormalizer.Normalize(cfc.ComponentHeadword)
        };
    }

    #endregion

    #region Sense

    public async Task<Sense> CreateSense(Guid entryId, Sense sense, BetweenPosition? position = null)
    {
        var normalized = NormalizeSense(sense);
        return await _api.CreateSense(entryId, normalized, position);
    }

    // JsonPatch: passed through without normalization (not user-facing)
    public Task<Sense> UpdateSense(Guid entryId, Guid senseId, UpdateObjectInput<Sense> update)
        => _api.UpdateSense(entryId, senseId, update);

    public async Task<Sense> UpdateSense(Guid entryId, Sense before, Sense after, IMiniLcmApi? api = null)
    {
        var normalized = NormalizeSense(after);
        return await _api.UpdateSense(entryId, before, normalized, api ?? this);
    }

    public Task MoveSense(Guid entryId, Guid senseId, BetweenPosition position)
    {
        return _api.MoveSense(entryId, senseId, position);
    }

    public Task DeleteSense(Guid entryId, Guid senseId)
    {
        return _api.DeleteSense(entryId, senseId);
    }

    public Task AddSemanticDomainToSense(Guid senseId, SemanticDomain semanticDomain)
    {
        var normalized = NormalizeSemanticDomain(semanticDomain);
        return _api.AddSemanticDomainToSense(senseId, normalized);
    }

    public Task RemoveSemanticDomainFromSense(Guid senseId, Guid semanticDomainId)
    {
        return _api.RemoveSemanticDomainFromSense(senseId, semanticDomainId);
    }

    public Task SetSensePartOfSpeech(Guid senseId, Guid? partOfSpeechId)
    {
        return _api.SetSensePartOfSpeech(senseId, partOfSpeechId);
    }

    private static Sense NormalizeSense(Sense sense)
    {
        return new Sense
        {
            Id = sense.Id,
            Order = sense.Order,
            DeletedAt = sense.DeletedAt,
            EntryId = sense.EntryId,
            Definition = StringNormalizer.Normalize(sense.Definition),
            Gloss = StringNormalizer.Normalize(sense.Gloss),
            PartOfSpeech = sense.PartOfSpeech is not null ? NormalizePartOfSpeech(sense.PartOfSpeech) : null,
            PartOfSpeechId = sense.PartOfSpeechId,
            SemanticDomains = sense.SemanticDomains.Select(NormalizeSemanticDomain).ToList(),
            ExampleSentences = sense.ExampleSentences.Select(NormalizeExampleSentence).ToList()
        };
    }

    #endregion

    #region ExampleSentence

    public async Task<ExampleSentence> CreateExampleSentence(Guid entryId, Guid senseId, ExampleSentence exampleSentence, BetweenPosition? position = null)
    {
        var normalized = NormalizeExampleSentence(exampleSentence);
        return await _api.CreateExampleSentence(entryId, senseId, normalized, position);
    }

    // JsonPatch: passed through without normalization (not user-facing)
    public Task<ExampleSentence> UpdateExampleSentence(Guid entryId, Guid senseId, Guid exampleSentenceId, UpdateObjectInput<ExampleSentence> update)
        => _api.UpdateExampleSentence(entryId, senseId, exampleSentenceId, update);

    public async Task<ExampleSentence> UpdateExampleSentence(Guid entryId, Guid senseId, ExampleSentence before, ExampleSentence after, IMiniLcmApi? api = null)
    {
        var normalized = NormalizeExampleSentence(after);
        return await _api.UpdateExampleSentence(entryId, senseId, before, normalized, api ?? this);
    }

    public Task MoveExampleSentence(Guid entryId, Guid senseId, Guid exampleSentenceId, BetweenPosition position)
    {
        return _api.MoveExampleSentence(entryId, senseId, exampleSentenceId, position);
    }

    public Task DeleteExampleSentence(Guid entryId, Guid senseId, Guid exampleSentenceId)
    {
        return _api.DeleteExampleSentence(entryId, senseId, exampleSentenceId);
    }

    public Task AddTranslation(Guid entryId, Guid senseId, Guid exampleSentenceId, Translation translation)
    {
        var normalized = NormalizeTranslation(translation);
        return _api.AddTranslation(entryId, senseId, exampleSentenceId, normalized);
    }

    public Task RemoveTranslation(Guid entryId, Guid senseId, Guid exampleSentenceId, Guid translationId)
    {
        return _api.RemoveTranslation(entryId, senseId, exampleSentenceId, translationId);
    }

    // JsonPatch: passed through without normalization (not user-facing)
    public Task UpdateTranslation(Guid entryId, Guid senseId, Guid exampleSentenceId, Guid translationId, UpdateObjectInput<Translation> update)
        => _api.UpdateTranslation(entryId, senseId, exampleSentenceId, translationId, update);

    private static ExampleSentence NormalizeExampleSentence(ExampleSentence example)
    {
        return new ExampleSentence
        {
            Id = example.Id,
            Order = example.Order,
            DeletedAt = example.DeletedAt,
            SenseId = example.SenseId,
            Sentence = StringNormalizer.Normalize(example.Sentence),
            Translations = example.Translations.Select(NormalizeTranslation).ToList(),
            Reference = StringNormalizer.Normalize(example.Reference)
        };
    }

    private static Translation NormalizeTranslation(Translation translation)
    {
        return new Translation
        {
            Id = translation.Id,
            Text = StringNormalizer.Normalize(translation.Text)
        };
    }

    #endregion

    #region Bulk Import

    public async Task BulkImportSemanticDomains(IAsyncEnumerable<SemanticDomain> semanticDomains)
    {
        // Create a normalized async enumerable that normalizes items as they're consumed
        async IAsyncEnumerable<SemanticDomain> NormalizeStream()
        {
            await foreach (var semanticDomain in semanticDomains)
            {
                yield return NormalizeSemanticDomain(semanticDomain);
            }
        }
        
        await _api.BulkImportSemanticDomains(NormalizeStream());
    }

    public async Task BulkCreateEntries(IAsyncEnumerable<Entry> entries)
    {
        // Create a normalized async enumerable that normalizes items as they're consumed
        async IAsyncEnumerable<Entry> NormalizeStream()
        {
            await foreach (var entry in entries)
            {
                yield return NormalizeEntry(entry);
            }
        }
        
        await _api.BulkCreateEntries(NormalizeStream());
    }

    #endregion

    #region File Operations

    public Task<UploadFileResponse> SaveFile(Stream stream, LcmFileMetadata metadata)
    {
        // File metadata is not user-entered text, so don't normalize
        return _api.SaveFile(stream, metadata);
    }

    #endregion

    void IDisposable.Dispose()
    {
        // No resources to dispose
    }
}
