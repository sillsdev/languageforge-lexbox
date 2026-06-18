using MiniLcm;
using MiniLcm.Media;
using MiniLcm.Models;
using MiniLcm.SyncHelpers;
using MiniLcm.Wrappers;

namespace MiniLcm.Validators;

public class MiniLcmApiValidationWrapperFactory(MiniLcmValidators validators) : IMiniLcmWrapperFactory
{
    public IMiniLcmApi Create(IMiniLcmApi api, IProjectIdentifier _unused) => Create(api);

    public IMiniLcmApi Create(IMiniLcmApi api)
    {
        return new MiniLcmApiValidationWrapper(api, validators);
    }
}

/// <summary>
/// Validates models on write before forwarding to the inner API.
///
/// Reads are auto-forwarded via BeaKona.AutoInterface. Writes are hand-written so that adding
/// an IMiniLcmWriteApi method without choosing how it validates is a compile error rather than
/// a silent unvalidated forward - which is how CreateEntry drifted out of validation (#2362).
///
/// Not every write validates. CreateEntry, CreateMorphType and all JsonPatch-based updates pass
/// through because sync/import wraps the api here and writes empty FLEx MultiStrings the validators
/// would reject - validating would break import. Publication writes pass through across the board
/// (a deferred gap), so UpdatePublication is the one before/after update that doesn't validate (#2362).
/// </summary>
public partial class MiniLcmApiValidationWrapper(
    IMiniLcmApi api,
    MiniLcmValidators validators) : IMiniLcmApi
{
    private readonly IMiniLcmApi _api = api;

    // BeaKona.AutoInterface only forwards IMiniLcmReadApi methods. IMiniLcmWriteApi methods are
    // NOT auto-forwarded, ensuring every write method is hand-written below (see class summary).
    [BeaKona.AutoInterface]
    private IMiniLcmReadApi ReadApi => _api;

    #region WritingSystem

    public async Task<WritingSystem> CreateWritingSystem(WritingSystem writingSystem, BetweenPosition<WritingSystemId?>? between = null)
    {
        await validators.ValidateAndThrow(writingSystem);
        return await _api.CreateWritingSystem(writingSystem, between);
    }

    public async Task<WritingSystem> UpdateWritingSystem(WritingSystemId id, WritingSystemType type, UpdateObjectInput<WritingSystem> update)
    {
        await validators.ValidateAndThrow(update);
        return await _api.UpdateWritingSystem(id, type, update);
    }

    public async Task<WritingSystem> UpdateWritingSystem(WritingSystem before, WritingSystem after, IMiniLcmApi? api = null)
    {
        await validators.ValidateAndThrow(after);
        return await _api.UpdateWritingSystem(before, after, api ?? this);
    }

    public Task MoveWritingSystem(WritingSystemId id, WritingSystemType type, BetweenPosition<WritingSystemId?> between)
    {
        return _api.MoveWritingSystem(id, type, between);
    }

    #endregion

    #region PartOfSpeech

    public async Task<PartOfSpeech> CreatePartOfSpeech(PartOfSpeech partOfSpeech)
    {
        await validators.ValidateAndThrow(partOfSpeech);
        return await _api.CreatePartOfSpeech(partOfSpeech);
    }

    public Task<PartOfSpeech> UpdatePartOfSpeech(Guid id, UpdateObjectInput<PartOfSpeech> update)
    {
        return _api.UpdatePartOfSpeech(id, update);
    }

    public async Task<PartOfSpeech> UpdatePartOfSpeech(PartOfSpeech before, PartOfSpeech after, IMiniLcmApi? api = null)
    {
        await validators.ValidateAndThrow(after);
        return await _api.UpdatePartOfSpeech(before, after, api ?? this);
    }

    public Task DeletePartOfSpeech(Guid id)
    {
        return _api.DeletePartOfSpeech(id);
    }

    #endregion

    #region Publication

    public Task<Publication> CreatePublication(Publication pub)
    {
        return _api.CreatePublication(pub);
    }

    public Task<Publication> UpdatePublication(Guid id, UpdateObjectInput<Publication> update)
    {
        return _api.UpdatePublication(id, update);
    }

    public Task<Publication> UpdatePublication(Publication before, Publication after, IMiniLcmApi? api = null)
    {
        return _api.UpdatePublication(before, after, api ?? this);
    }

    public Task DeletePublication(Guid id)
    {
        return _api.DeletePublication(id);
    }

    #endregion

    #region SemanticDomain

    public async Task<SemanticDomain> CreateSemanticDomain(SemanticDomain semanticDomain)
    {
        await validators.ValidateAndThrow(semanticDomain);
        return await _api.CreateSemanticDomain(semanticDomain);
    }

    public Task<SemanticDomain> UpdateSemanticDomain(Guid id, UpdateObjectInput<SemanticDomain> update)
    {
        return _api.UpdateSemanticDomain(id, update);
    }

    public async Task<SemanticDomain> UpdateSemanticDomain(SemanticDomain before, SemanticDomain after, IMiniLcmApi? api = null)
    {
        await validators.ValidateAndThrow(after);
        return await _api.UpdateSemanticDomain(before, after, api ?? this);
    }

    public Task DeleteSemanticDomain(Guid id)
    {
        return _api.DeleteSemanticDomain(id);
    }

    #endregion

    #region ComplexFormType

    public async Task<ComplexFormType> CreateComplexFormType(ComplexFormType complexFormType)
    {
        await validators.ValidateAndThrow(complexFormType);
        return await _api.CreateComplexFormType(complexFormType);
    }

    public Task<ComplexFormType> UpdateComplexFormType(Guid id, UpdateObjectInput<ComplexFormType> update)
    {
        return _api.UpdateComplexFormType(id, update);
    }

    public async Task<ComplexFormType> UpdateComplexFormType(ComplexFormType before, ComplexFormType after, IMiniLcmApi? api = null)
    {
        await validators.ValidateAndThrow(after);
        return await _api.UpdateComplexFormType(before, after, api ?? this);
    }

    public Task DeleteComplexFormType(Guid id)
    {
        return _api.DeleteComplexFormType(id);
    }

    #endregion

    #region MorphType

    public Task<MorphType> CreateMorphType(MorphType morphType)
    {
        return _api.CreateMorphType(morphType);
    }

    public async Task<MorphType> UpdateMorphType(Guid id, UpdateObjectInput<MorphType> update)
    {
        await validators.ValidateAndThrow(update);
        return await _api.UpdateMorphType(id, update);
    }

    public async Task<MorphType> UpdateMorphType(MorphType before, MorphType after, IMiniLcmApi? api = null)
    {
        await validators.ValidateAndThrow(after);
        return await _api.UpdateMorphType(before, after, api ?? this);
    }

    #endregion

    #region Entry

    public Task<Entry> CreateEntry(Entry entry, CreateEntryOptions? options = null)
    {
        return _api.CreateEntry(entry, options);
    }

    public Task<Entry> UpdateEntry(Guid id, UpdateObjectInput<Entry> update)
    {
        return _api.UpdateEntry(id, update);
    }

    public async Task<Entry> UpdateEntry(Entry before, Entry after, IMiniLcmApi? api = null)
    {
        await validators.ValidateAndThrow(after);
        return await _api.UpdateEntry(before, after, api ?? this);
    }

    public Task DeleteEntry(Guid id)
    {
        return _api.DeleteEntry(id);
    }

    public Task<ComplexFormComponent> CreateComplexFormComponent(ComplexFormComponent complexFormComponent, BetweenPosition<ComplexFormComponent>? position = null)
    {
        return _api.CreateComplexFormComponent(complexFormComponent, position);
    }

    public Task MoveComplexFormComponent(ComplexFormComponent complexFormComponent, BetweenPosition<ComplexFormComponent> between)
    {
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

    #endregion

    #region Sense

    public async Task<Sense> CreateSense(Guid entryId, Sense sense, BetweenPosition? between = null)
    {
        await validators.ValidateAndThrow(sense);
        return await _api.CreateSense(entryId, sense, between);
    }

    public Task<Sense> UpdateSense(Guid entryId, Guid senseId, UpdateObjectInput<Sense> update)
    {
        return _api.UpdateSense(entryId, senseId, update);
    }

    public async Task<Sense> UpdateSense(Guid entryId, Sense before, Sense after, IMiniLcmApi? api = null)
    {
        await validators.ValidateAndThrow(after);
        return await _api.UpdateSense(entryId, before, after, api ?? this);
    }

    public Task MoveSense(Guid entryId, Guid senseId, BetweenPosition between)
    {
        return _api.MoveSense(entryId, senseId, between);
    }

    public Task DeleteSense(Guid entryId, Guid senseId)
    {
        return _api.DeleteSense(entryId, senseId);
    }

    public Task AddSemanticDomainToSense(Guid senseId, SemanticDomain semanticDomain)
    {
        return _api.AddSemanticDomainToSense(senseId, semanticDomain);
    }

    public Task RemoveSemanticDomainFromSense(Guid senseId, Guid semanticDomainId)
    {
        return _api.RemoveSemanticDomainFromSense(senseId, semanticDomainId);
    }

    public Task SetSensePartOfSpeech(Guid senseId, Guid? partOfSpeechId)
    {
        return _api.SetSensePartOfSpeech(senseId, partOfSpeechId);
    }

    #endregion

    #region ExampleSentence

    public async Task<ExampleSentence> CreateExampleSentence(Guid entryId,
        Guid senseId,
        ExampleSentence exampleSentence,
        BetweenPosition? between = null)
    {
        await validators.ValidateAndThrow(exampleSentence);
        return await _api.CreateExampleSentence(entryId, senseId, exampleSentence, between);
    }

    public Task<ExampleSentence> UpdateExampleSentence(Guid entryId,
        Guid senseId,
        Guid exampleSentenceId,
        UpdateObjectInput<ExampleSentence> update)
    {
        return _api.UpdateExampleSentence(entryId, senseId, exampleSentenceId, update);
    }

    public async Task<ExampleSentence> UpdateExampleSentence(Guid entryId,
        Guid senseId,
        ExampleSentence before,
        ExampleSentence after,
        IMiniLcmApi? api = null)
    {
        await validators.ValidateAndThrow(after);
        return await _api.UpdateExampleSentence(entryId, senseId, before, after, api ?? this);
    }

    public Task MoveExampleSentence(Guid entryId, Guid senseId, Guid exampleSentenceId, BetweenPosition between)
    {
        return _api.MoveExampleSentence(entryId, senseId, exampleSentenceId, between);
    }

    public Task DeleteExampleSentence(Guid entryId, Guid senseId, Guid exampleSentenceId)
    {
        return _api.DeleteExampleSentence(entryId, senseId, exampleSentenceId);
    }

    public Task AddTranslation(Guid entryId, Guid senseId, Guid exampleSentenceId, Translation translation)
    {
        return _api.AddTranslation(entryId, senseId, exampleSentenceId, translation);
    }

    public Task RemoveTranslation(Guid entryId, Guid senseId, Guid exampleSentenceId, Guid translationId)
    {
        return _api.RemoveTranslation(entryId, senseId, exampleSentenceId, translationId);
    }

    public Task UpdateTranslation(Guid entryId, Guid senseId, Guid exampleSentenceId, Guid translationId, UpdateObjectInput<Translation> update)
    {
        return _api.UpdateTranslation(entryId, senseId, exampleSentenceId, translationId, update);
    }

    #endregion

    #region CustomView

    public Task<CustomView> CreateCustomView(CustomView customView)
    {
        return _api.CreateCustomView(customView);
    }

    public Task<CustomView> UpdateCustomView(CustomView customView)
    {
        return _api.UpdateCustomView(customView);
    }

    public Task DeleteCustomView(Guid id)
    {
        return _api.DeleteCustomView(id);
    }

    #endregion

    #region Bulk and Files

    public Task BulkImportSemanticDomains(IAsyncEnumerable<SemanticDomain> semanticDomains)
    {
        return _api.BulkImportSemanticDomains(semanticDomains);
    }

    public Task BulkCreateEntries(IAsyncEnumerable<Entry> entries)
    {
        return _api.BulkCreateEntries(entries);
    }

    public Task<UploadFileResponse> SaveFile(Stream stream, LcmFileMetadata metadata)
    {
        return _api.SaveFile(stream, metadata);
    }

    #endregion

    void IDisposable.Dispose()
    {
    }
}
