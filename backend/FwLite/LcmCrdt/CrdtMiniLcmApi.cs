using LcmCrdt.MiniLcmImp;
using MiniLcm.SyncHelpers;
using MiniLcm.Media;

namespace LcmCrdt;

public class CrdtMiniLcmApi(
    CurrentProjectService projectService,
    CrdtWritingSystemApi writingSystemApi,
    CrdtSemanticDomainsApi semanticDomainsApi,
    CrdtPublicationApi publicationApi,
    CrdtComplexFormComponentApi complexFormComponentApi,
    CrdtMorphTypeApi morphTypeApi,
    CrdtPartsOfSpeechApi partsOfSpeechApi,
    CrdtComplexFormTypesApi complexFormTypesApi,
    CrdtEntryApi entryApi,
    CrdtSenseApi senseApi,
    CrdtExampleSentenceApi exampleSentenceApi,
    CrdtPictureApi pictureApi,
    CrdtMediaApi mediaApi,
    CrdtCustomViewApi customViewApi,
    CrdtCommentApi commentApi) : IMiniLcmApi
{
    public ProjectData ProjectData => projectService.ProjectData;
    public CrdtProject Project => projectService.Project;

    #region WritingSystemApi
    public Task<WritingSystems> GetWritingSystems()
    {
        return writingSystemApi.GetWritingSystems();
    }

    public Task<WritingSystem> CreateWritingSystem(WritingSystem writingSystem,
        BetweenPosition<WritingSystemId?>? between = null)
    {
        return writingSystemApi.CreateWritingSystem(writingSystem, between);
    }

    public Task<WritingSystem> UpdateWritingSystem(WritingSystemId id,
        WritingSystemType type,
        UpdateObjectInput<WritingSystem> update)
    {
        return writingSystemApi.UpdateWritingSystem(id, type, update);
    }

    public Task<WritingSystem> UpdateWritingSystem(WritingSystem before,
        WritingSystem after,
        IMiniLcmApi? api = null)
    {
        return writingSystemApi.UpdateWritingSystem(before, after, api ?? this);
    }

    public Task MoveWritingSystem(WritingSystemId id, WritingSystemType type, BetweenPosition<WritingSystemId?> between)
    {
        return writingSystemApi.MoveWritingSystem(id, type, between);
    }

    public Task<WritingSystem?> GetWritingSystem(WritingSystemId id, WritingSystemType type)
    {
        return writingSystemApi.GetWritingSystem(id, type);
    }
    #endregion

    #region PartsOfSpeechApi
    public IAsyncEnumerable<PartOfSpeech> GetPartsOfSpeech()
    {
        return partsOfSpeechApi.GetPartsOfSpeech();
    }

    public async Task<PartOfSpeech?> GetPartOfSpeech(Guid id)
    {
        return await partsOfSpeechApi.GetPartOfSpeech(id);
    }

    public async Task<PartOfSpeech> CreatePartOfSpeech(PartOfSpeech partOfSpeech)
    {
        return await partsOfSpeechApi.CreatePartOfSpeech(partOfSpeech);
    }

    public async Task SubmitUpdatePartOfSpeech(Guid id, UpdateObjectInput<PartOfSpeech> update)
    {
        await partsOfSpeechApi.SubmitUpdatePartOfSpeech(id, update);
    }

    public async Task<PartOfSpeech> UpdatePartOfSpeech(Guid id, UpdateObjectInput<PartOfSpeech> update)
    {
        return await partsOfSpeechApi.UpdatePartOfSpeech(id, update);
    }

    public async Task<PartOfSpeech> UpdatePartOfSpeech(PartOfSpeech before, PartOfSpeech after, IMiniLcmApi? api)
    {
        return await partsOfSpeechApi.UpdatePartOfSpeech(before, after, api ?? this);
    }

    public async Task DeletePartOfSpeech(Guid id)
    {
        await partsOfSpeechApi.DeletePartOfSpeech(id);
    }

    public async Task SetSensePartOfSpeech(Guid senseId, Guid? partOfSpeechId)
    {
        await partsOfSpeechApi.SetSensePartOfSpeech(senseId, partOfSpeechId);
    }
    #endregion

    #region PublicationApi
    public IAsyncEnumerable<Publication> GetPublications()
    {
        return publicationApi.GetPublications();
    }

    public async Task<Publication?> GetPublication(Guid id)
    {
        return await publicationApi.GetPublication(id);
    }

    public async Task<Publication> CreatePublication(Publication pub)
    {
        return await publicationApi.CreatePublication(pub);
    }

    public async Task SubmitUpdatePublication(Guid id, UpdateObjectInput<Publication> update)
    {
        await publicationApi.SubmitUpdatePublication(id, update);
    }

    public async Task<Publication> UpdatePublication(Guid id, UpdateObjectInput<Publication> update)
    {
        return await publicationApi.UpdatePublication(id, update);
    }

    public async Task<Publication> UpdatePublication(Publication before, Publication after, IMiniLcmApi? api = null)
    {
        return await publicationApi.UpdatePublication(before, after, api ?? this);
    }

    public async Task DeletePublication(Guid id)
    {
        await publicationApi.DeletePublication(id);
    }

    public async Task AddPublication(Guid entryId, Guid publicationId)
    {
        await publicationApi.AddPublication(entryId, publicationId);
    }

    public async Task RemovePublication(Guid entryId, Guid publicationId)
    {
        await publicationApi.RemovePublication(entryId, publicationId);
    }
    #endregion

    #region SemanticDomainApi
    public IAsyncEnumerable<SemanticDomain> GetSemanticDomains()
    {
        return semanticDomainsApi.GetSemanticDomains();
    }

    public async Task<SemanticDomain?> GetSemanticDomain(Guid id)
    {
        return await semanticDomainsApi.GetSemanticDomain(id);
    }

    public async Task<SemanticDomain> CreateSemanticDomain(SemanticDomain semanticDomain)
    {
        return await semanticDomainsApi.CreateSemanticDomain(semanticDomain);
    }

    public async Task SubmitUpdateSemanticDomain(Guid id, UpdateObjectInput<SemanticDomain> update)
    {
        await semanticDomainsApi.SubmitUpdateSemanticDomain(id, update);
    }

    public async Task<SemanticDomain> UpdateSemanticDomain(Guid id, UpdateObjectInput<SemanticDomain> update)
    {
        return await semanticDomainsApi.UpdateSemanticDomain(id, update);
    }

    public async Task<SemanticDomain> UpdateSemanticDomain(SemanticDomain before, SemanticDomain after, IMiniLcmApi? api = null)
    {
        return await semanticDomainsApi.UpdateSemanticDomain(before, after, api ?? this);
    }

    public async Task DeleteSemanticDomain(Guid id)
    {
        await semanticDomainsApi.DeleteSemanticDomain(id);
    }

    public async Task BulkImportSemanticDomains(IAsyncEnumerable<SemanticDomain> semanticDomains)
    {
        await semanticDomainsApi.BulkImportSemanticDomains(semanticDomains);
    }

    public async Task AddSemanticDomainToSense(Guid senseId, SemanticDomain semanticDomain)
    {
        await semanticDomainsApi.AddSemanticDomainToSense(senseId, semanticDomain);
    }

    public async Task RemoveSemanticDomainFromSense(Guid senseId, Guid semanticDomainId)
    {
        await semanticDomainsApi.RemoveSemanticDomainFromSense(senseId, semanticDomainId);
    }
    #endregion

    #region ComplexFormTypeApi
    public IAsyncEnumerable<ComplexFormType> GetComplexFormTypes()
    {
        return complexFormTypesApi.GetComplexFormTypes();
    }

    public async Task<ComplexFormType?> GetComplexFormType(Guid id)
    {
        return await complexFormTypesApi.GetComplexFormType(id);
    }

    public async Task<ComplexFormType> CreateComplexFormType(ComplexFormType complexFormType)
    {
        return await complexFormTypesApi.CreateComplexFormType(complexFormType);
    }

    public async Task SubmitUpdateComplexFormType(Guid id, UpdateObjectInput<ComplexFormType> update)
    {
        await complexFormTypesApi.SubmitUpdateComplexFormType(id, update);
    }

    public async Task<ComplexFormType> UpdateComplexFormType(Guid id, UpdateObjectInput<ComplexFormType> update)
    {
        return await complexFormTypesApi.UpdateComplexFormType(id, update);
    }

    public async Task<ComplexFormType> UpdateComplexFormType(ComplexFormType before, ComplexFormType after, IMiniLcmApi? api = null)
    {
        return await complexFormTypesApi.UpdateComplexFormType(before, after, api ?? this);
    }

    public async Task DeleteComplexFormType(Guid id)
    {
        await complexFormTypesApi.DeleteComplexFormType(id);
    }

    public async Task AddComplexFormType(Guid entryId, Guid complexFormTypeId)
    {
        await complexFormTypesApi.AddComplexFormType(entryId, complexFormTypeId);
    }

    public async Task RemoveComplexFormType(Guid entryId, Guid complexFormTypeId)
    {
        await complexFormTypesApi.RemoveComplexFormType(entryId, complexFormTypeId);
    }
    #endregion

    #region ComplexFormComponentApi
    public async Task SubmitCreateComplexFormComponent(ComplexFormComponent complexFormComponent, BetweenPosition<ComplexFormComponent>? between = null)
    {
        await complexFormComponentApi.SubmitCreateComplexFormComponent(complexFormComponent, between);
    }

    public async Task<ComplexFormComponent> CreateComplexFormComponent(ComplexFormComponent complexFormComponent, BetweenPosition<ComplexFormComponent>? between = null)
    {
        return await complexFormComponentApi.CreateComplexFormComponent(complexFormComponent, between);
    }

    public async Task MoveComplexFormComponent(ComplexFormComponent component, BetweenPosition<ComplexFormComponent> between)
    {
        await complexFormComponentApi.MoveComplexFormComponent(component, between);
    }

    public async Task SubmitMoveComplexFormComponent(ComplexFormComponent component, BetweenPosition<ComplexFormComponent> between)
    {
        await complexFormComponentApi.SubmitMoveComplexFormComponent(component, between);
    }

    public async Task DeleteComplexFormComponent(ComplexFormComponent complexFormComponent)
    {
        await complexFormComponentApi.DeleteComplexFormComponent(complexFormComponent);
    }
    #endregion

    #region MorphTypeApi
    public IAsyncEnumerable<MorphType> GetMorphTypes()
    {
        return morphTypeApi.GetMorphTypes();
    }

    public async Task<MorphType?> GetMorphType(Guid id)
    {
        return await morphTypeApi.GetMorphType(id);
    }

    public async Task<MorphType?> GetMorphType(MorphTypeKind kind)
    {
        return await morphTypeApi.GetMorphType(kind);
    }

    public async Task<MorphType> CreateMorphType(MorphType morphType)
    {
        return await morphTypeApi.CreateMorphType(morphType);
    }

    public async Task<MorphType> UpdateMorphType(Guid id, UpdateObjectInput<MorphType> update)
    {
        return await morphTypeApi.UpdateMorphType(id, update);
    }

    public async Task<MorphType> UpdateMorphType(MorphType before, MorphType after, IMiniLcmApi? api = null)
    {
        return await morphTypeApi.UpdateMorphType(before, after, api ?? this);
    }
    #endregion

    #region EntryApi
    public async Task<int> CountEntries(string? query = null, FilterQueryOptions? options = null)
    {
        return await entryApi.CountEntries(query, options);
    }

    public IAsyncEnumerable<Entry> GetEntries(QueryOptions? options = null)
    {
        return entryApi.GetEntries(options);
    }

    public IAsyncEnumerable<Entry> SearchEntries(string? query, QueryOptions? options = null)
    {
        return entryApi.SearchEntries(query, options);
    }

    public async Task<Entry?> GetEntry(Guid id)
    {
        return await entryApi.GetEntry(id);
    }

    public async Task<int> GetEntryIndex(Guid entryId, string? query = null, IndexQueryOptions? options = null)
    {
        return await entryApi.GetEntryIndex(entryId, query, options);
    }

    public async Task BulkCreateEntries(IAsyncEnumerable<Entry> entries)
    {
        await entryApi.BulkCreateEntries(entries);
    }

    public async Task<Entry> CreateEntry(Entry entry, CreateEntryOptions? options = null)
    {
        return await entryApi.CreateEntry(entry, options);
    }

    public async Task SubmitUpdateEntry(Guid id, UpdateObjectInput<Entry> update)
    {
        await entryApi.SubmitUpdateEntry(id, update);
    }

    public async Task<Entry> UpdateEntry(Guid id,
        UpdateObjectInput<Entry> update)
    {
        return await entryApi.UpdateEntry(id, update);
    }

    public async Task<Entry> UpdateEntry(Entry before, Entry after, IMiniLcmApi? api = null)
    {
        return await entryApi.UpdateEntry(before, after, api ?? this);
    }

    public async Task DeleteEntry(Guid id)
    {
        await entryApi.DeleteEntry(id);
    }
    #endregion

    #region SenseApi
    public async Task<Sense?> GetSense(Guid senseId)
    {
        return await senseApi.GetSense(senseId);
    }

    public async Task<Sense?> GetSense(Guid entryId, Guid senseId)
    {
        return await senseApi.GetSense(entryId, senseId);
    }

    public async Task SubmitCreateSense(Guid entryId, Sense sense, BetweenPosition? between = null)
    {
        await senseApi.SubmitCreateSense(entryId, sense, between);
    }

    public async Task<Sense> CreateSense(Guid entryId, Sense sense, BetweenPosition? between = null)
    {
        return await senseApi.CreateSense(entryId, sense, between);
    }

    public async Task SubmitUpdateSense(Guid entryId, Guid senseId, UpdateObjectInput<Sense> update)
    {
        await senseApi.SubmitUpdateSense(entryId, senseId, update);
    }

    public async Task<Sense> UpdateSense(Guid entryId,
        Guid senseId,
        UpdateObjectInput<Sense> update)
    {
        return await senseApi.UpdateSense(entryId, senseId, update);
    }

    public async Task<Sense> UpdateSense(Guid entryId, Sense before, Sense after, IMiniLcmApi? api = null)
    {
        return await senseApi.UpdateSense(entryId, before, after, api ?? this);
    }

    public async Task MoveSense(Guid entryId, Guid senseId, BetweenPosition between, MoveKind kind = MoveKind.Reorder)
    {
        await senseApi.MoveSense(entryId, senseId, between, kind);
    }

    public async Task SubmitMoveSense(Guid entryId, Guid senseId, BetweenPosition position, MoveKind kind = MoveKind.Reorder)
    {
        await senseApi.SubmitMoveSense(entryId, senseId, position, kind);
    }

    public async Task DeleteSense(Guid entryId, Guid senseId)
    {
        await senseApi.DeleteSense(entryId, senseId);
    }
    #endregion

    #region ExampleSentenceApi
    public async Task SubmitCreateExampleSentence(Guid entryId,
        Guid senseId,
        ExampleSentence exampleSentence,
        BetweenPosition? between = null)
    {
        await exampleSentenceApi.SubmitCreateExampleSentence(entryId, senseId, exampleSentence, between);
    }

    public async Task<ExampleSentence> CreateExampleSentence(Guid entryId,
        Guid senseId,
        ExampleSentence exampleSentence,
        BetweenPosition? between = null)
    {
        return await exampleSentenceApi.CreateExampleSentence(entryId, senseId, exampleSentence, between);
    }

    public async Task<ExampleSentence?> GetExampleSentence(Guid entryId, Guid senseId, Guid id)
    {
        return await exampleSentenceApi.GetExampleSentence(entryId, senseId, id);
    }

    public async Task SubmitUpdateExampleSentence(Guid entryId,
        Guid senseId,
        Guid exampleSentenceId,
        UpdateObjectInput<ExampleSentence> update)
    {
        await exampleSentenceApi.SubmitUpdateExampleSentence(entryId, senseId, exampleSentenceId, update);
    }

    public async Task<ExampleSentence> UpdateExampleSentence(Guid entryId,
        Guid senseId,
        Guid exampleSentenceId,
        UpdateObjectInput<ExampleSentence> update)
    {
        return await exampleSentenceApi.UpdateExampleSentence(entryId, senseId, exampleSentenceId, update);
    }

    public async Task<ExampleSentence> UpdateExampleSentence(Guid entryId,
        Guid senseId,
        ExampleSentence before,
        ExampleSentence after,
        IMiniLcmApi? api = null)
    {
        return await exampleSentenceApi.UpdateExampleSentence(entryId, senseId, before, after, api ?? this);
    }

    public async Task MoveExampleSentence(Guid entryId, Guid senseId, Guid exampleId, BetweenPosition between, MoveKind kind = MoveKind.Reorder)
    {
        await exampleSentenceApi.MoveExampleSentence(entryId, senseId, exampleId, between, kind);
    }

    public async Task SubmitMoveExampleSentence(Guid entryId, Guid senseId, Guid exampleSentenceId, BetweenPosition position, MoveKind kind = MoveKind.Reorder)
    {
        await exampleSentenceApi.SubmitMoveExampleSentence(entryId, senseId, exampleSentenceId, position, kind);
    }

    public async Task DeleteExampleSentence(Guid entryId, Guid senseId, Guid exampleSentenceId)
    {
        await exampleSentenceApi.DeleteExampleSentence(entryId, senseId, exampleSentenceId);
    }

    public async Task AddTranslation(Guid entryId, Guid senseId, Guid exampleSentenceId, Translation translation)
    {
        await exampleSentenceApi.AddTranslation(entryId, senseId, exampleSentenceId, translation);
    }

    public async Task RemoveTranslation(Guid entryId, Guid senseId, Guid exampleSentenceId, Guid translationId)
    {
        await exampleSentenceApi.RemoveTranslation(entryId, senseId, exampleSentenceId, translationId);
    }

    public async Task UpdateTranslation(Guid entryId,
        Guid senseId,
        Guid exampleSentenceId,
        Guid translationId,
        UpdateObjectInput<Translation> update)
    {
        await exampleSentenceApi.UpdateTranslation(entryId, senseId, exampleSentenceId, translationId, update);
    }

    [Obsolete($"Use {nameof(AddTranslation)} instead")]
    public async Task SetFirstTranslationIds(IDictionary<Guid, Guid> exampleSentenceIdToTranslationId)
    {
        await exampleSentenceApi.SetFirstTranslationIds(exampleSentenceIdToTranslationId);
    }
    #endregion

    #region PictureApi
    public async Task<Picture> CreatePicture(Guid entryId,
        Guid senseId,
        Picture picture,
        BetweenPosition? between = null)
    {
        return await pictureApi.CreatePicture(entryId, senseId, picture, between);
    }

    public async Task<Picture?> GetPicture(Guid entryId, Guid senseId, Guid id)
    {
        return await pictureApi.GetPicture(entryId, senseId, id);
    }

    public async Task SubmitUpdatePicture(Guid entryId,
        Guid senseId,
        Guid pictureId,
        UpdateObjectInput<Picture> update)
    {
        await pictureApi.SubmitUpdatePicture(entryId, senseId, pictureId, update);
    }

    public async Task<Picture> UpdatePicture(Guid entryId,
        Guid senseId,
        Guid pictureId,
        UpdateObjectInput<Picture> update)
    {
        return await pictureApi.UpdatePicture(entryId, senseId, pictureId, update);
    }

    public async Task<Picture> UpdatePicture(Guid entryId,
        Guid senseId,
        Picture before,
        Picture after,
        IMiniLcmApi? api = null)
    {
        return await pictureApi.UpdatePicture(entryId, senseId, before, after, api ?? this);
    }

    public async Task MovePicture(Guid entryId, Guid senseId, Guid pictureId, BetweenPosition between)
    {
        await pictureApi.MovePicture(entryId, senseId, pictureId, between);
    }

    public async Task DeletePicture(Guid entryId, Guid senseId, Guid pictureId)
    {
        await pictureApi.DeletePicture(entryId, senseId, pictureId);
    }
    #endregion

    #region MediaApi
    public async Task<ReadFileResponse> GetFileStream(MediaUri mediaUri, bool downloadIfMissing = true)
    {
        return await mediaApi.GetFileStream(mediaUri, downloadIfMissing);
    }

    public async Task<UploadFileResponse> SaveFile(Stream stream, LcmFileMetadata metadata)
    {
        return await mediaApi.SaveFile(stream, metadata);
    }
    #endregion

    #region CustomViewApi
    public IAsyncEnumerable<CustomView> GetCustomViews()
    {
        return customViewApi.GetCustomViews();
    }

    public async Task<CustomView?> GetCustomView(Guid id)
    {
        return await customViewApi.GetCustomView(id);
    }

    public async Task<CustomView> CreateCustomView(CustomView customView)
    {
        return await customViewApi.CreateCustomView(customView);
    }

    public async Task<CustomView> UpdateCustomView(CustomView customView)
    {
        return await customViewApi.UpdateCustomView(customView);
    }

    public async Task DeleteCustomView(Guid id)
    {
        await customViewApi.DeleteCustomView(id);
    }
    #endregion

    #region CommentApi
    public IAsyncEnumerable<CommentThread> GetCommentThreads(SubjectType subjectType, Guid subjectId, bool includeComments = false)
    {
        return commentApi.GetCommentThreads(subjectType, subjectId, includeComments);
    }

    public async Task<CommentThread?> GetCommentThread(Guid id)
    {
        return await commentApi.GetCommentThread(id);
    }

    public IAsyncEnumerable<UserComment> GetUserComments(Guid threadId)
    {
        return commentApi.GetUserComments(threadId);
    }

    public async Task<UserComment?> GetUserComment(Guid id)
    {
        return await commentApi.GetUserComment(id);
    }

    public IAsyncEnumerable<UserComment> GetUnreadComments(Guid? threadId = null)
    {
        return commentApi.GetUnreadComments(threadId);
    }

    public IAsyncEnumerable<UserComment> GetUnreadCommentsForSubject(SubjectType subjectType, Guid subjectId)
    {
        return commentApi.GetUnreadCommentsForSubject(subjectType, subjectId);
    }

    public async Task<int> CountUnreadComments(Guid? threadId = null)
    {
        return await commentApi.CountUnreadComments(threadId);
    }

    public async Task<CommentThread> CreateCommentThread(CommentThread thread, UserComment firstComment)
    {
        return await commentApi.CreateCommentThread(thread, firstComment);
    }

    public async Task<UserComment> AddUserComment(Guid threadId, UserComment comment)
    {
        return await commentApi.AddUserComment(threadId, comment);
    }

    public async Task<UserComment> EditUserComment(Guid commentId, string text)
    {
        return await commentApi.EditUserComment(commentId, text);
    }

    public async Task<CommentThread> SetCommentThreadStatus(Guid threadId, ThreadStatus status)
    {
        return await commentApi.SetCommentThreadStatus(threadId, status);
    }

    public async Task DeleteUserComment(Guid commentId)
    {
        await commentApi.DeleteUserComment(commentId);
    }

    public async Task DeleteCommentThread(Guid threadId)
    {
        await commentApi.DeleteCommentThread(threadId);
    }

    public async Task MarkCommentRead(Guid commentId)
    {
        await commentApi.MarkCommentRead(commentId);
    }

    public async Task MarkCommentThreadUnread(Guid threadId)
    {
        await commentApi.MarkCommentThreadUnread(threadId);
    }

    public async Task MarkCommentThreadRead(Guid threadId)
    {
        await commentApi.MarkCommentThreadRead(threadId);
    }

    public async Task MarkAllCommentsRead()
    {
        await commentApi.MarkAllCommentsRead();
    }
    #endregion

    public void Dispose()
    {
    }
}
