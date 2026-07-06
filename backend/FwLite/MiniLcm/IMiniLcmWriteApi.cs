using System.Linq.Expressions;
using MiniLcm.Media;
using MiniLcm.Models;
using MiniLcm.SyncHelpers;
using SystemTextJsonPatch;

namespace MiniLcm;

public interface IMiniLcmWriteApi
{
    Task<WritingSystem> CreateWritingSystem(WritingSystem writingSystem, BetweenPosition<WritingSystemId?>? between = null);

    Task<WritingSystem> UpdateWritingSystem(WritingSystemId id,
        WritingSystemType type,
        UpdateObjectInput<WritingSystem> update);
    Task<WritingSystem> UpdateWritingSystem(WritingSystem before, WritingSystem after, IMiniLcmApi? api = null);
    // Note there's no Task DeleteWritingSystem(Guid id) because deleting writing systems needs careful consideration, as it can cause a massive cascade of data deletion
    Task MoveWritingSystem(WritingSystemId id, WritingSystemType type, BetweenPosition<WritingSystemId?> between);

    #region PartOfSpeech
    Task<PartOfSpeech> CreatePartOfSpeech(PartOfSpeech partOfSpeech);
    Task<PartOfSpeech> UpdatePartOfSpeech(Guid id, UpdateObjectInput<PartOfSpeech> update);
    Task<PartOfSpeech> UpdatePartOfSpeech(PartOfSpeech before, PartOfSpeech after, IMiniLcmApi? api = null);
    Task DeletePartOfSpeech(Guid id);
    #endregion
    #region Publication
    Task<Publication> CreatePublication(Publication pub);
    Task<Publication> UpdatePublication(Guid id, UpdateObjectInput<Publication> update);
    Task<Publication> UpdatePublication(Publication before, Publication after, IMiniLcmApi? api = null);
    Task DeletePublication(Guid id);
    #endregion

    #region SemanticDomain
    Task<SemanticDomain> CreateSemanticDomain(SemanticDomain semanticDomain);
    Task<SemanticDomain> UpdateSemanticDomain(Guid id, UpdateObjectInput<SemanticDomain> update);
    Task<SemanticDomain> UpdateSemanticDomain(SemanticDomain before, SemanticDomain after, IMiniLcmApi? api = null);
    Task DeleteSemanticDomain(Guid id);
    #endregion

    #region ComplexFormType
    Task<ComplexFormType> CreateComplexFormType(ComplexFormType complexFormType);
    Task<ComplexFormType> UpdateComplexFormType(Guid id, UpdateObjectInput<ComplexFormType> update);
    Task<ComplexFormType> UpdateComplexFormType(ComplexFormType before, ComplexFormType after, IMiniLcmApi? api = null);
    Task DeleteComplexFormType(Guid id);
    #endregion

    #region VariantType
    Task<VariantType> CreateVariantType(VariantType variantType);
    Task<VariantType> UpdateVariantType(Guid id, UpdateObjectInput<VariantType> update);
    Task<VariantType> UpdateVariantType(VariantType before, VariantType after, IMiniLcmApi? api = null);
    Task DeleteVariantType(Guid id);
    #endregion

    #region MorphType
    Task<MorphType> CreateMorphType(MorphType morphType);
    Task<MorphType> UpdateMorphType(Guid id, UpdateObjectInput<MorphType> update);
    Task<MorphType> UpdateMorphType(MorphType before, MorphType after, IMiniLcmApi? api = null);
    #endregion

    #region Entry
    /// <summary>
    /// Creates an entry. With null <paramref name="options"/> the main publication is auto-added
    /// (<see cref="CreateEntryOptions.WithMainPublication"/>); pass <see cref="CreateEntryOptions.AsIs"/> to keep publications as given (sync/import do this).
    /// </summary>
    Task<Entry> CreateEntry(Entry entry, CreateEntryOptions? options = null);
    Task<Entry> UpdateEntry(Guid id, UpdateObjectInput<Entry> update);

    Task<Entry> UpdateEntry(Entry before, Entry after, IMiniLcmApi? api = null);
    Task DeleteEntry(Guid id);
    Task<ComplexFormComponent> CreateComplexFormComponent(ComplexFormComponent complexFormComponent, BetweenPosition<ComplexFormComponent>? position = null);
    Task MoveComplexFormComponent(ComplexFormComponent complexFormComponent, BetweenPosition<ComplexFormComponent> between);
    Task DeleteComplexFormComponent(ComplexFormComponent complexFormComponent);
    Task AddComplexFormType(Guid entryId, Guid complexFormTypeId);
    Task RemoveComplexFormType(Guid entryId, Guid complexFormTypeId);
    // Variant links are unordered (no position/move) and resolved by their composite key
    // (VariantEntryId, MainEntryId, MainSenseId) — see VARIANTS.md. A link's Types sequence
    // IS ordered (FLEx lets users reorder it), hence the picture-style position/move pair.
    Task<Variant> CreateVariant(Variant variant);
    Task<Variant> UpdateVariant(Variant before, Variant after, IMiniLcmApi? api = null);
    Task DeleteVariant(Variant variant);
    Task AddVariantType(Variant variant, Guid variantTypeId, BetweenPosition? position = null);
    Task RemoveVariantType(Variant variant, Guid variantTypeId);
    Task MoveVariantType(Variant variant, Guid variantTypeId, BetweenPosition position);
    Task AddPublication(Guid entryId, Guid publicationId);
    Task RemovePublication(Guid entryId, Guid publicationId);
    #endregion

    #region Sense
    /// <summary>
    /// Creates the provided sense and adds it to the specified entry
    /// </summary>
    /// <param name="entryId">The ID of the sense's parent entry</param>
    /// <param name="sesnse">The sense to create</param>
    /// <param name="position">Where the sense should be inserted in the entry's list of senses. If null it will be appended to the end of the list.</param>
    /// <returns></returns>
    Task<Sense> CreateSense(Guid entryId, Sense sense, BetweenPosition? position = null);
    Task<Sense> UpdateSense(Guid entryId, Guid senseId, UpdateObjectInput<Sense> update);
    Task<Sense> UpdateSense(Guid entryId, Sense before, Sense after, IMiniLcmApi? api = null);
    Task MoveSense(Guid entryId, Guid senseId, BetweenPosition position);
    Task DeleteSense(Guid entryId, Guid senseId);
    Task AddSemanticDomainToSense(Guid senseId, SemanticDomain semanticDomain);
    Task RemoveSemanticDomainFromSense(Guid senseId, Guid semanticDomainId);
    Task SetSensePartOfSpeech(Guid senseId, Guid? partOfSpeechId);
    #endregion

    #region ExampleSentence
    /// <summary>
    /// Creates the provided example sentence and adds it to the specified sense
    /// </summary>
    /// <param name="entryId">The ID of the sense's parent entry</param>
    /// <param name="senseId">The ID of example sentence's parent sense</param>
    /// <param name="exampleSentence">The example sentence to create</param>
    /// <param name="position">Where the example sentence should be inserted in the sense's list of example sentences. If null it will be appended to the end of the list.</param>
    /// <returns></returns>
    Task<ExampleSentence> CreateExampleSentence(Guid entryId, Guid senseId, ExampleSentence exampleSentence, BetweenPosition? position = null);
    Task<ExampleSentence> UpdateExampleSentence(Guid entryId,
        Guid senseId,
        Guid exampleSentenceId,
        UpdateObjectInput<ExampleSentence> update);
    Task<ExampleSentence> UpdateExampleSentence(Guid entryId,
        Guid senseId,
        ExampleSentence before,
        ExampleSentence after,
        IMiniLcmApi? api = null);
    Task MoveExampleSentence(Guid entryId, Guid senseId, Guid exampleSentenceId, BetweenPosition position);

    Task DeleteExampleSentence(Guid entryId, Guid senseId, Guid exampleSentenceId);

    Task AddTranslation(Guid entryId, Guid senseId, Guid exampleSentenceId, Translation translation);
    Task RemoveTranslation(Guid entryId, Guid senseId, Guid exampleSentenceId, Guid translationId);
    Task UpdateTranslation(Guid entryId, Guid senseId, Guid exampleSentenceId, Guid translationId, UpdateObjectInput<Translation> update);
    #endregion

    #region Picture
    /// <summary>
    /// Creates the provided picture and adds it to the specified sense
    /// </summary>
    /// <param name="entryId">The ID of the sense's parent entry</param>
    /// <param name="senseId">The ID of picture's parent sense</param>
    /// <param name="picture">The picture to create</param>
    /// <param name="position">Where the picture should be inserted in the sense's list of pictures. If null it will be appended to the end of the list.</param>
    /// <returns></returns>
    Task<Picture> CreatePicture(Guid entryId, Guid senseId, Picture picture, BetweenPosition? position = null);
    Task<Picture> UpdatePicture(Guid entryId,
        Guid senseId,
        Guid pictureId,
        UpdateObjectInput<Picture> update);
    Task<Picture> UpdatePicture(Guid entryId,
        Guid senseId,
        Picture before,
        Picture after,
        IMiniLcmApi? api = null);
    Task MovePicture(Guid entryId, Guid senseId, Guid pictureId, BetweenPosition position);
    Task DeletePicture(Guid entryId, Guid senseId, Guid pictureId);
    #endregion

    #region Submit (fire-and-forget write variants for sync)
    // Result-less write variants the sync uses instead of the returning Update/Create methods above. The CRDT
    // overrides them to submit the change without fetching the result, so applying to an object the other side
    // deleted leaves it deleted (delete wins) rather than throwing. The defaults forward to the returning
    // method (correct for FwData, which still surfaces a genuinely-missing object).
    Task SubmitUpdateEntry(Guid id, UpdateObjectInput<Entry> update) => UpdateEntry(id, update);
    Task SubmitCreateComplexFormComponent(ComplexFormComponent complexFormComponent, BetweenPosition<ComplexFormComponent>? position = null) => CreateComplexFormComponent(complexFormComponent, position);
    Task SubmitMoveComplexFormComponent(ComplexFormComponent complexFormComponent, BetweenPosition<ComplexFormComponent> between) => MoveComplexFormComponent(complexFormComponent, between);
    Task SubmitCreateVariant(Variant variant) => CreateVariant(variant);
    /// <summary>
    /// Patches a variant link's own fields (HideMinorEntry, Comment) — the link is located by
    /// its composite key, not its Id, since FwData links have no stable Id. Abstract (unlike
    /// its Submit* siblings) because there is no id-based UpdateVariant(id, patch) twin to
    /// forward to; the patch path only exists for sync.
    /// </summary>
    Task SubmitUpdateVariant(Variant variant, UpdateObjectInput<Variant> update);
    Task SubmitCreateSense(Guid entryId, Sense sense, BetweenPosition? position = null) => CreateSense(entryId, sense, position);
    Task SubmitUpdateSense(Guid entryId, Guid senseId, UpdateObjectInput<Sense> update) => UpdateSense(entryId, senseId, update);
    Task SubmitCreateExampleSentence(Guid entryId, Guid senseId, ExampleSentence exampleSentence, BetweenPosition? position = null) => CreateExampleSentence(entryId, senseId, exampleSentence, position);
    Task SubmitUpdateExampleSentence(Guid entryId, Guid senseId, Guid exampleSentenceId, UpdateObjectInput<ExampleSentence> update) => UpdateExampleSentence(entryId, senseId, exampleSentenceId, update);
    // Dependency types too (they sync before entries, outside EntrySync's try/catch). WritingSystem is omitted
    // (its update resolves the entity id, so it can't be a blind submit); MorphType is omitted (not deletable).
    Task SubmitUpdatePartOfSpeech(Guid id, UpdateObjectInput<PartOfSpeech> update) => UpdatePartOfSpeech(id, update);
    Task SubmitUpdatePicture(Guid entryId, Guid senseId, Guid pictureId, UpdateObjectInput<Picture> update) => UpdatePicture(entryId, senseId, pictureId, update);
    Task SubmitUpdatePublication(Guid id, UpdateObjectInput<Publication> update) => UpdatePublication(id, update);
    Task SubmitUpdateSemanticDomain(Guid id, UpdateObjectInput<SemanticDomain> update) => UpdateSemanticDomain(id, update);
    Task SubmitUpdateComplexFormType(Guid id, UpdateObjectInput<ComplexFormType> update) => UpdateComplexFormType(id, update);
    Task SubmitUpdateVariantType(Guid id, UpdateObjectInput<VariantType> update) => UpdateVariantType(id, update);
    #endregion

    #region CustomView
    Task<CustomView> CreateCustomView(CustomView customView)
    {
        throw new NotSupportedException("Custom views are only supported by CRDT projects");
    }
    Task<CustomView> UpdateCustomView(CustomView customView)
    {
        throw new NotSupportedException("Custom views are only supported by CRDT projects");
    }
    Task DeleteCustomView(Guid id)
    {
        throw new NotSupportedException("Custom views are only supported by CRDT projects");
    }
    #endregion

    #region Comments
    Task<CommentThread> CreateCommentThread(CommentThread thread, UserComment firstComment)
    {
        throw new NotSupportedException("Comments are only supported by CRDT projects");
    }
    Task<UserComment> AddUserComment(Guid threadId, UserComment comment)
    {
        throw new NotSupportedException("Comments are only supported by CRDT projects");
    }
    Task<UserComment> EditUserComment(Guid commentId, string text)
    {
        throw new NotSupportedException("Comments are only supported by CRDT projects");
    }
    Task<CommentThread> SetCommentThreadStatus(Guid threadId, ThreadStatus status)
    {
        throw new NotSupportedException("Comments are only supported by CRDT projects");
    }
    Task DeleteUserComment(Guid commentId)
    {
        throw new NotSupportedException("Comments are only supported by CRDT projects");
    }
    Task DeleteCommentThread(Guid threadId)
    {
        throw new NotSupportedException("Comments are only supported by CRDT projects");
    }
    Task MarkCommentRead(Guid commentId)
    {
        throw new NotSupportedException("Comments are only supported by CRDT projects");
    }
    Task MarkCommentThreadRead(Guid threadId)
    {
        throw new NotSupportedException("Comments are only supported by CRDT projects");
    }
    Task MarkAllCommentsRead()
    {
        throw new NotSupportedException("Comments are only supported by CRDT projects");
    }
    #endregion


    /// <summary>
    /// Imports the provided semantic domains in bulk.
    /// </summary>
    /// <param name="semanticDomains">The semantic domains to import.</param>
    async Task BulkImportSemanticDomains(IAsyncEnumerable<SemanticDomain> semanticDomains)
    {
        await foreach (var semanticDomain in semanticDomains)
        {
            await this.CreateSemanticDomain(semanticDomain);
        }
    }

    /// <summary>
    /// Imports the provided entries in bulk.
    /// </summary>
    /// <param name="entries">The entries to import.</param>
    async Task BulkCreateEntries(IAsyncEnumerable<Entry> entries)
    {
        await foreach (var entry in entries)
        {
            // Import preserves the source's publications; never inject the main publication into imported entries.
            await this.CreateEntry(entry, CreateEntryOptions.AsIs);
        }
    }

    /// <summary>
    /// Saves a media file using the provided data stream and metadata.
    /// </summary>
    /// <param name="stream">The stream containing the media file data to be saved.</param>
    /// <param name="metadata">Metadata associated with the media file, including details like filename and upload information.</param>
    /// <returns>An <see cref="UploadFileResponse"/> indicating the outcome of the save operation.</returns>
    Task<UploadFileResponse> SaveFile(Stream stream, LcmFileMetadata metadata)
    {
        return Task.FromResult(new UploadFileResponse(UploadFileResult.NotSupported));
    }
}

/// <summary>
/// API for saving the project, really only used by FwData
/// </summary>
public interface IMiniLcmSaveApi
{
    void Save();
}

/// <summary>
/// wrapper around JsonPatchDocument that allows for fluent updates
/// </summary>
/// <param name="patchDocument"></param>
/// <typeparam name="T"></typeparam>
public class UpdateObjectInput<T>(JsonPatchDocument<T> patchDocument) where T : class
{
    public UpdateObjectInput() : this(new JsonPatchDocument<T>()) { }
    public JsonPatchDocument<T> Patch { get; } = patchDocument;

    public void Apply(T obj)
    {
        Patch.ApplyTo(obj);
    }

    public UpdateObjectInput<T> Set<T_Val>(Expression<Func<T, T_Val>> field, T_Val value)
    {
        Patch.Replace(field, value);
        return this;
    }

    public UpdateObjectInput<T> Add<T_Val>(Expression<Func<T, IList<T_Val>>> field, T_Val value)
    {
        Patch.Add(field, value);
        return this;
    }

    /// <summary>
    /// Removes an item by index, should not be used with CRDTs.
    /// </summary>
    public UpdateObjectInput<T> Remove<T_Val>(Expression<Func<T, IList<T_Val>>> field, int index)
    {
        Patch.Remove(field, index);
        return this;
    }
}
