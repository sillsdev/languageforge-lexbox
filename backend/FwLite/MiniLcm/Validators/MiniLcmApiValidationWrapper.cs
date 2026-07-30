using FluentValidation;
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
/// Validates user-supplied models before writing them.
///
/// Design notes:
/// - Read operations are forwarded automatically via BeaKona.AutoInterface.
/// - Write operations are hand-written so validation can't silently go missing again: a drifted or
///   missing write override becomes a compile error rather than a BeaKona-generated pass-through with
///   no validation (the exact bug in #2359 / #2362, where CreateEntry's signature drifted and every
///   caller bound to the generated unvalidated forwarder).
/// - Only writes that carry a model we have a validator for actually validate; the rest forward as-is.
///   Where a method deliberately does NOT validate, that's now an explicit, visible choice.
/// - Submit* variants forward to the inner Submit* (not the returning Update/Create) so the CRDT
///   delete-wins semantics are preserved.
/// - Imperative validation that needs an async lookup (the single-main-publication rule) throws
///   FluentValidation.ValidationException, matching the type the validators throw.
/// </summary>
public partial class MiniLcmApiValidationWrapper(
    IMiniLcmApi api,
    MiniLcmValidators validators) : IMiniLcmApi
{
    private readonly IMiniLcmApi _api = api;

    // BeaKona.AutoInterface only forwards IMiniLcmReadApi methods.
    // IMiniLcmWriteApi methods are NOT auto-forwarded, ensuring compile-time
    // enforcement that all write methods are manually implemented below.
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

    public Task SubmitUpdatePartOfSpeech(Guid id, UpdateObjectInput<PartOfSpeech> update)
    {
        return _api.SubmitUpdatePartOfSpeech(id, update);
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

    public async Task<Publication> CreatePublication(Publication pub)
    {
        await validators.ValidateAndThrow(pub);
        if (pub.IsMain && await GetExistingMain() is not null)
            throw new ValidationException("Cannot create a second main publication. A main publication already exists.");
        return await _api.CreatePublication(pub);
    }

    public async Task<Publication> UpdatePublication(Guid id, UpdateObjectInput<Publication> update)
    {
        await validators.ValidateAndThrow(update);
        if (update.TryGetPropertyChange<Publication, bool>(nameof(Publication.IsMain), out var isMain) && isMain)
            await ThrowIfAnotherMainExists(id);
        return await _api.UpdatePublication(id, update);
    }

    public async Task SubmitUpdatePublication(Guid id, UpdateObjectInput<Publication> update)
    {
        await validators.ValidateAndThrow(update);
        if (update.TryGetPropertyChange<Publication, bool>(nameof(Publication.IsMain), out var isMain) && isMain)
            await ThrowIfAnotherMainExists(id);
        await _api.SubmitUpdatePublication(id, update);
    }

    public async Task<Publication> UpdatePublication(Publication before, Publication after, IMiniLcmApi? api = null)
    {
        await validators.ValidateAndThrow(after);
        // This overload bypasses PublicationUpdateValidator, so enforce the single-main invariant here too.
        if (after.IsMain && !before.IsMain)
            await ThrowIfAnotherMainExists(after.Id);
        if (before.IsMain && !after.IsMain)
            throw new ValidationException("Cannot turn off the IsMain flag on a publication; the main publication is fixed.");
        return await _api.UpdatePublication(before, after, api ?? this);
    }

    public Task DeletePublication(Guid id)
    {
        return _api.DeletePublication(id);
    }

    private async Task ThrowIfAnotherMainExists(Guid id)
    {
        if (await GetExistingMain() is { } main && main.Id != id)
            throw new ValidationException("Cannot set IsMain on this publication. Another publication is already the main publication.");
    }

    private async Task<Publication?> GetExistingMain()
    {
        await foreach (var publication in _api.GetPublications())
        {
            if (publication.IsMain) return publication;
        }
        return null;
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

    public Task SubmitUpdateSemanticDomain(Guid id, UpdateObjectInput<SemanticDomain> update)
    {
        return _api.SubmitUpdateSemanticDomain(id, update);
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

    public Task SubmitUpdateComplexFormType(Guid id, UpdateObjectInput<ComplexFormType> update)
    {
        return _api.SubmitUpdateComplexFormType(id, update);
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

    // CreateMorphType deliberately does not validate: sync/import writes FLEx morph types whose names
    // can be empty, which MorphTypeValidator would reject. Preserving prior behavior; see #2359.
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

    // CreateEntry now validates (#2359). Note: sync drives this with FwData reads that can carry empty
    // MultiString values (EntryValidator.NoEmptyValues rejects them) — see the PR description's risk note.
    public async Task<Entry> CreateEntry(Entry entry, CreateEntryOptions? options = null)
    {
        await validators.ValidateAndThrow(entry);
        return await _api.CreateEntry(entry, options);
    }

    public Task<Entry> UpdateEntry(Guid id, UpdateObjectInput<Entry> update)
    {
        return _api.UpdateEntry(id, update);
    }

    public Task SubmitUpdateEntry(Guid id, UpdateObjectInput<Entry> update)
    {
        return _api.SubmitUpdateEntry(id, update);
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

    public Task SubmitCreateComplexFormComponent(ComplexFormComponent complexFormComponent, BetweenPosition<ComplexFormComponent>? position = null)
    {
        return _api.SubmitCreateComplexFormComponent(complexFormComponent, position);
    }

    public Task MoveComplexFormComponent(ComplexFormComponent complexFormComponent, BetweenPosition<ComplexFormComponent> between)
    {
        return _api.MoveComplexFormComponent(complexFormComponent, between);
    }

    public Task SubmitMoveComplexFormComponent(ComplexFormComponent complexFormComponent, BetweenPosition<ComplexFormComponent> between)
    {
        return _api.SubmitMoveComplexFormComponent(complexFormComponent, between);
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

    public Task SubmitCreateSense(Guid entryId, Sense sense, BetweenPosition? position = null)
    {
        return _api.SubmitCreateSense(entryId, sense, position);
    }

    public Task<Sense> UpdateSense(Guid entryId, Guid senseId, UpdateObjectInput<Sense> update)
    {
        return _api.UpdateSense(entryId, senseId, update);
    }

    public Task SubmitUpdateSense(Guid entryId, Guid senseId, UpdateObjectInput<Sense> update)
    {
        return _api.SubmitUpdateSense(entryId, senseId, update);
    }

    public async Task<Sense> UpdateSense(Guid entryId, Sense before, Sense after, IMiniLcmApi? api = null)
    {
        await validators.ValidateAndThrow(after);
        return await _api.UpdateSense(entryId, before, after, api ?? this);
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

    public Task SubmitCreateExampleSentence(Guid entryId, Guid senseId, ExampleSentence exampleSentence, BetweenPosition? position = null)
    {
        return _api.SubmitCreateExampleSentence(entryId, senseId, exampleSentence, position);
    }

    public Task<ExampleSentence> UpdateExampleSentence(Guid entryId, Guid senseId, Guid exampleSentenceId, UpdateObjectInput<ExampleSentence> update)
    {
        return _api.UpdateExampleSentence(entryId, senseId, exampleSentenceId, update);
    }

    public Task SubmitUpdateExampleSentence(Guid entryId, Guid senseId, Guid exampleSentenceId, UpdateObjectInput<ExampleSentence> update)
    {
        return _api.SubmitUpdateExampleSentence(entryId, senseId, exampleSentenceId, update);
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

    #region Picture

    public Task<Picture> CreatePicture(Guid entryId, Guid senseId, Picture picture, BetweenPosition? position = null)
    {
        return _api.CreatePicture(entryId, senseId, picture, position);
    }

    public Task<Picture> UpdatePicture(Guid entryId, Guid senseId, Guid pictureId, UpdateObjectInput<Picture> update)
    {
        return _api.UpdatePicture(entryId, senseId, pictureId, update);
    }

    public Task SubmitUpdatePicture(Guid entryId, Guid senseId, Guid pictureId, UpdateObjectInput<Picture> update)
    {
        return _api.SubmitUpdatePicture(entryId, senseId, pictureId, update);
    }

    public Task<Picture> UpdatePicture(Guid entryId, Guid senseId, Picture before, Picture after, IMiniLcmApi? api = null)
    {
        return _api.UpdatePicture(entryId, senseId, before, after, api ?? this);
    }

    public Task MovePicture(Guid entryId, Guid senseId, Guid pictureId, BetweenPosition position)
    {
        return _api.MovePicture(entryId, senseId, pictureId, position);
    }

    public Task DeletePicture(Guid entryId, Guid senseId, Guid pictureId)
    {
        return _api.DeletePicture(entryId, senseId, pictureId);
    }

    #endregion

    #region Bulk Import

    // Bulk import forwards to the inner api, which preserves source data (including empty FLEx values)
    // by not routing through the validating single-entry CreateEntry.
    public Task BulkImportSemanticDomains(IAsyncEnumerable<SemanticDomain> semanticDomains)
    {
        return _api.BulkImportSemanticDomains(semanticDomains);
    }

    public Task BulkCreateEntries(IAsyncEnumerable<Entry> entries)
    {
        return _api.BulkCreateEntries(entries);
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

    #region Comments

    public Task<CommentThread> CreateCommentThread(CommentThread thread, UserComment firstComment)
    {
        return _api.CreateCommentThread(thread, firstComment);
    }

    public Task<UserComment> AddUserComment(Guid threadId, UserComment comment)
    {
        return _api.AddUserComment(threadId, comment);
    }

    public Task<UserComment> EditUserComment(Guid commentId, string text)
    {
        return _api.EditUserComment(commentId, text);
    }

    public Task<CommentThread> SetCommentThreadStatus(Guid threadId, ThreadStatus status)
    {
        return _api.SetCommentThreadStatus(threadId, status);
    }

    public Task DeleteUserComment(Guid commentId)
    {
        return _api.DeleteUserComment(commentId);
    }

    public Task DeleteCommentThread(Guid threadId)
    {
        return _api.DeleteCommentThread(threadId);
    }

    public Task MarkCommentRead(Guid commentId)
    {
        return _api.MarkCommentRead(commentId);
    }

    public Task MarkCommentThreadRead(Guid threadId)
    {
        return _api.MarkCommentThreadRead(threadId);
    }

    public Task MarkAllCommentsRead()
    {
        return _api.MarkAllCommentsRead();
    }

    #endregion

    #region File Operations

    public Task<UploadFileResponse> SaveFile(Stream stream, LcmFileMetadata metadata)
    {
        return _api.SaveFile(stream, metadata);
    }

    #endregion

    void IDisposable.Dispose()
    {
    }
}
