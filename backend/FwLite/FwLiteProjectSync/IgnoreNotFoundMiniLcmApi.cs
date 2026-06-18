using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using MiniLcm;
using MiniLcm.Exceptions;
using MiniLcm.Media;
using MiniLcm.Models;
using MiniLcm.SyncHelpers;

namespace FwLiteProjectSync;

public class IgnoreNotFoundMiniLcmApiFactory(ILogger<IgnoreNotFoundMiniLcmApi> logger)
{
    public IMiniLcmApi Create(IMiniLcmApi api) => new IgnoreNotFoundMiniLcmApi(api, logger);
}

/// <summary>
/// During sync an object can be deleted on this side while it's still referenced by edits coming from the
/// other side. The diff then tries to update an object that's gone, create a child on a deleted parent, or
/// attach a relationship to a deleted object, and the API throws <see cref="NotFoundException"/>. Every write
/// here swallows that (and logs it): the object is gone, so there's nothing to write, and the reverse-direction
/// diff propagates the deletion to the other side.
///
/// Because a NotFound can come from almost any write (any one that touches a parent/referenced object), every
/// write is wrapped rather than a hand-picked subset — that way a newly added write can't silently skip the
/// handling. The one exception is CreateEntry (see below). Read operations are forwarded automatically via
/// BeaKona.AutoInterface.
///
/// The swallowed result (null) is never observed: the sync diff discards the return of every wrapped write
/// (child creates, relationship adds, updates). CreateEntry is the only write whose return the diff consumes,
/// so it's left unwrapped — and it creates a root object that can't 404, so it needs no tolerance anyway.
///
/// Note: a couple of "referenced object was deleted" cases throw InvalidOperationException instead of
/// NotFoundException (e.g. CreateSense with a deleted PartOfSpeech, AddComplexFormType with a deleted type)
/// and are deliberately NOT handled here — catching InvalidOperationException broadly would mask real bugs.
///
/// Only meant to wrap the CRDT API during sync (see <see cref="CrdtFwdataProjectSyncService"/>).
/// </summary>
public partial class IgnoreNotFoundMiniLcmApi(IMiniLcmApi api, ILogger logger) : IMiniLcmApi
{
    private readonly IMiniLcmApi _api = api;

    [BeaKona.AutoInterface]
    private IMiniLcmReadApi ReadApi => _api;

    private async Task<T> IgnoreNotFound<T>(Func<Task<T>> operation, [CallerMemberName] string method = "")
    {
        try
        {
            return await operation();
        }
        catch (NotFoundException e)
        {
            logger.LogInformation(e, "Ignoring {Method} during sync because the object was deleted on this side", method);
            return default!;
        }
    }

    private async Task IgnoreNotFound(Func<Task> operation, [CallerMemberName] string method = "")
    {
        try
        {
            await operation();
        }
        catch (NotFoundException e)
        {
            logger.LogInformation(e, "Ignoring {Method} during sync because the object was deleted on this side", method);
        }
    }

    #region WritingSystem
    public Task<WritingSystem> CreateWritingSystem(WritingSystem writingSystem, BetweenPosition<WritingSystemId?>? between = null) => IgnoreNotFound(() => _api.CreateWritingSystem(writingSystem, between));
    public Task<WritingSystem> UpdateWritingSystem(WritingSystemId id, WritingSystemType type, UpdateObjectInput<WritingSystem> update) => IgnoreNotFound(() => _api.UpdateWritingSystem(id, type, update));
    public Task<WritingSystem> UpdateWritingSystem(WritingSystem before, WritingSystem after, IMiniLcmApi? api = null) => IgnoreNotFound(() => _api.UpdateWritingSystem(before, after, api ?? this));
    public Task MoveWritingSystem(WritingSystemId id, WritingSystemType type, BetweenPosition<WritingSystemId?> between) => IgnoreNotFound(() => _api.MoveWritingSystem(id, type, between));
    #endregion

    #region PartOfSpeech
    public Task<PartOfSpeech> CreatePartOfSpeech(PartOfSpeech partOfSpeech) => IgnoreNotFound(() => _api.CreatePartOfSpeech(partOfSpeech));
    public Task<PartOfSpeech> UpdatePartOfSpeech(Guid id, UpdateObjectInput<PartOfSpeech> update) => IgnoreNotFound(() => _api.UpdatePartOfSpeech(id, update));
    public Task<PartOfSpeech> UpdatePartOfSpeech(PartOfSpeech before, PartOfSpeech after, IMiniLcmApi? api = null) => IgnoreNotFound(() => _api.UpdatePartOfSpeech(before, after, api ?? this));
    public Task DeletePartOfSpeech(Guid id) => IgnoreNotFound(() => _api.DeletePartOfSpeech(id));
    #endregion

    #region Publication
    public Task<Publication> CreatePublication(Publication pub) => IgnoreNotFound(() => _api.CreatePublication(pub));
    public Task<Publication> UpdatePublication(Guid id, UpdateObjectInput<Publication> update) => IgnoreNotFound(() => _api.UpdatePublication(id, update));
    public Task<Publication> UpdatePublication(Publication before, Publication after, IMiniLcmApi? api = null) => IgnoreNotFound(() => _api.UpdatePublication(before, after, api ?? this));
    public Task DeletePublication(Guid id) => IgnoreNotFound(() => _api.DeletePublication(id));
    #endregion

    #region SemanticDomain
    public Task<SemanticDomain> CreateSemanticDomain(SemanticDomain semanticDomain) => IgnoreNotFound(() => _api.CreateSemanticDomain(semanticDomain));
    public Task<SemanticDomain> UpdateSemanticDomain(Guid id, UpdateObjectInput<SemanticDomain> update) => IgnoreNotFound(() => _api.UpdateSemanticDomain(id, update));
    public Task<SemanticDomain> UpdateSemanticDomain(SemanticDomain before, SemanticDomain after, IMiniLcmApi? api = null) => IgnoreNotFound(() => _api.UpdateSemanticDomain(before, after, api ?? this));
    public Task DeleteSemanticDomain(Guid id) => IgnoreNotFound(() => _api.DeleteSemanticDomain(id));
    #endregion

    #region ComplexFormType
    public Task<ComplexFormType> CreateComplexFormType(ComplexFormType complexFormType) => IgnoreNotFound(() => _api.CreateComplexFormType(complexFormType));
    public Task<ComplexFormType> UpdateComplexFormType(Guid id, UpdateObjectInput<ComplexFormType> update) => IgnoreNotFound(() => _api.UpdateComplexFormType(id, update));
    public Task<ComplexFormType> UpdateComplexFormType(ComplexFormType before, ComplexFormType after, IMiniLcmApi? api = null) => IgnoreNotFound(() => _api.UpdateComplexFormType(before, after, api ?? this));
    public Task DeleteComplexFormType(Guid id) => IgnoreNotFound(() => _api.DeleteComplexFormType(id));
    #endregion

    #region MorphType
    public Task<MorphType> CreateMorphType(MorphType morphType) => IgnoreNotFound(() => _api.CreateMorphType(morphType));
    public Task<MorphType> UpdateMorphType(Guid id, UpdateObjectInput<MorphType> update) => IgnoreNotFound(() => _api.UpdateMorphType(id, update));
    public Task<MorphType> UpdateMorphType(MorphType before, MorphType after, IMiniLcmApi? api = null) => IgnoreNotFound(() => _api.UpdateMorphType(before, after, api ?? this));
    #endregion

    #region Entry
    // Not wrapped: the diff consumes the returned entry, and a root-object create can't 404, so wrapping would
    // add no safety while risking a null fed into the diff.
    public Task<Entry> CreateEntry(Entry entry, CreateEntryOptions? options = null) => _api.CreateEntry(entry, options);
    public Task<Entry> UpdateEntry(Guid id, UpdateObjectInput<Entry> update) => IgnoreNotFound(() => _api.UpdateEntry(id, update));
    public Task<Entry> UpdateEntry(Entry before, Entry after, IMiniLcmApi? api = null) => IgnoreNotFound(() => _api.UpdateEntry(before, after, api ?? this));
    public Task DeleteEntry(Guid id) => IgnoreNotFound(() => _api.DeleteEntry(id));
    public Task<ComplexFormComponent> CreateComplexFormComponent(ComplexFormComponent complexFormComponent, BetweenPosition<ComplexFormComponent>? position = null) => IgnoreNotFound(() => _api.CreateComplexFormComponent(complexFormComponent, position));
    public Task MoveComplexFormComponent(ComplexFormComponent complexFormComponent, BetweenPosition<ComplexFormComponent> between) => IgnoreNotFound(() => _api.MoveComplexFormComponent(complexFormComponent, between));
    public Task DeleteComplexFormComponent(ComplexFormComponent complexFormComponent) => IgnoreNotFound(() => _api.DeleteComplexFormComponent(complexFormComponent));
    public Task AddComplexFormType(Guid entryId, Guid complexFormTypeId) => IgnoreNotFound(() => _api.AddComplexFormType(entryId, complexFormTypeId));
    public Task RemoveComplexFormType(Guid entryId, Guid complexFormTypeId) => IgnoreNotFound(() => _api.RemoveComplexFormType(entryId, complexFormTypeId));
    public Task AddPublication(Guid entryId, Guid publicationId) => IgnoreNotFound(() => _api.AddPublication(entryId, publicationId));
    public Task RemovePublication(Guid entryId, Guid publicationId) => IgnoreNotFound(() => _api.RemovePublication(entryId, publicationId));
    #endregion

    #region Sense
    public Task<Sense> CreateSense(Guid entryId, Sense sense, BetweenPosition? position = null) => IgnoreNotFound(() => _api.CreateSense(entryId, sense, position));
    public Task<Sense> UpdateSense(Guid entryId, Guid senseId, UpdateObjectInput<Sense> update) => IgnoreNotFound(() => _api.UpdateSense(entryId, senseId, update));
    public Task<Sense> UpdateSense(Guid entryId, Sense before, Sense after, IMiniLcmApi? api = null) => IgnoreNotFound(() => _api.UpdateSense(entryId, before, after, api ?? this));
    public Task MoveSense(Guid entryId, Guid senseId, BetweenPosition position) => IgnoreNotFound(() => _api.MoveSense(entryId, senseId, position));
    public Task DeleteSense(Guid entryId, Guid senseId) => IgnoreNotFound(() => _api.DeleteSense(entryId, senseId));
    public Task AddSemanticDomainToSense(Guid senseId, SemanticDomain semanticDomain) => IgnoreNotFound(() => _api.AddSemanticDomainToSense(senseId, semanticDomain));
    public Task RemoveSemanticDomainFromSense(Guid senseId, Guid semanticDomainId) => IgnoreNotFound(() => _api.RemoveSemanticDomainFromSense(senseId, semanticDomainId));
    public Task SetSensePartOfSpeech(Guid senseId, Guid? partOfSpeechId) => IgnoreNotFound(() => _api.SetSensePartOfSpeech(senseId, partOfSpeechId));
    #endregion

    #region ExampleSentence
    public Task<ExampleSentence> CreateExampleSentence(Guid entryId, Guid senseId, ExampleSentence exampleSentence, BetweenPosition? position = null) => IgnoreNotFound(() => _api.CreateExampleSentence(entryId, senseId, exampleSentence, position));
    public Task<ExampleSentence> UpdateExampleSentence(Guid entryId, Guid senseId, Guid exampleSentenceId, UpdateObjectInput<ExampleSentence> update) => IgnoreNotFound(() => _api.UpdateExampleSentence(entryId, senseId, exampleSentenceId, update));
    public Task<ExampleSentence> UpdateExampleSentence(Guid entryId, Guid senseId, ExampleSentence before, ExampleSentence after, IMiniLcmApi? api = null) => IgnoreNotFound(() => _api.UpdateExampleSentence(entryId, senseId, before, after, api ?? this));
    public Task MoveExampleSentence(Guid entryId, Guid senseId, Guid exampleSentenceId, BetweenPosition position) => IgnoreNotFound(() => _api.MoveExampleSentence(entryId, senseId, exampleSentenceId, position));
    public Task DeleteExampleSentence(Guid entryId, Guid senseId, Guid exampleSentenceId) => IgnoreNotFound(() => _api.DeleteExampleSentence(entryId, senseId, exampleSentenceId));
    public Task AddTranslation(Guid entryId, Guid senseId, Guid exampleSentenceId, Translation translation) => IgnoreNotFound(() => _api.AddTranslation(entryId, senseId, exampleSentenceId, translation));
    public Task RemoveTranslation(Guid entryId, Guid senseId, Guid exampleSentenceId, Guid translationId) => IgnoreNotFound(() => _api.RemoveTranslation(entryId, senseId, exampleSentenceId, translationId));
    public Task UpdateTranslation(Guid entryId, Guid senseId, Guid exampleSentenceId, Guid translationId, UpdateObjectInput<Translation> update) => IgnoreNotFound(() => _api.UpdateTranslation(entryId, senseId, exampleSentenceId, translationId, update));
    #endregion

    #region CustomView
    public Task<CustomView> CreateCustomView(CustomView customView) => IgnoreNotFound(() => _api.CreateCustomView(customView));
    public Task<CustomView> UpdateCustomView(CustomView customView) => IgnoreNotFound(() => _api.UpdateCustomView(customView));
    public Task DeleteCustomView(Guid id) => IgnoreNotFound(() => _api.DeleteCustomView(id));
    #endregion

    #region Bulk / Files
    public Task BulkImportSemanticDomains(IAsyncEnumerable<SemanticDomain> semanticDomains) => IgnoreNotFound(() => _api.BulkImportSemanticDomains(semanticDomains));
    public Task BulkCreateEntries(IAsyncEnumerable<Entry> entries) => IgnoreNotFound(() => _api.BulkCreateEntries(entries));
    public Task<UploadFileResponse> SaveFile(Stream stream, LcmFileMetadata metadata) => IgnoreNotFound(() => _api.SaveFile(stream, metadata));
    #endregion

    void IDisposable.Dispose()
    {
    }
}
