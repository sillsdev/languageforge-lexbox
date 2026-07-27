using MiniLcm;
using MiniLcm.Exceptions;
using MiniLcm.Models;
using MiniLcm.SyncHelpers;

namespace FwLiteProjectSync;

/// <summary>
/// Reads pass through to the wrapped api; writes are ignored and return a plausible value (the input, or the
/// current state) instead of being applied. Used as the inner api for the fwdata side of a dry run, wrapped by
/// <see cref="RecordingMiniLcmApi"/>: the fwdata file must not change and is never read back mid-sync, so a
/// write that leaves reads untouched is exactly what the dry run wants.
/// </summary>
// The api is typed IMiniLcmReadApi so this class can't forward a write even by accident. Reads are forwarded by
// BeaKona; every write is implemented below (the compiler enforces it, since IMiniLcmWriteApi isn't generated).
public partial class WriteIgnoringMiniLcmApi(IMiniLcmReadApi api) : IMiniLcmApi
{
    [BeaKona.AutoInterface]
    private IMiniLcmReadApi ReadApi => api;

    public void Dispose()
    {
    }

    public Task<WritingSystem> CreateWritingSystem(WritingSystem writingSystem, BetweenPosition<WritingSystemId?>? position = null)
    {
        return Task.FromResult(writingSystem);
    }

    public async Task<WritingSystem> UpdateWritingSystem(WritingSystemId id,
        WritingSystemType type,
        UpdateObjectInput<WritingSystem> update)
    {
        var ws = await api.GetWritingSystems();
        return (type switch
        {
            WritingSystemType.Vernacular => ws.Vernacular,
            WritingSystemType.Analysis => ws.Analysis,
            _ => throw new InvalidOperationException("unknown type " + type)
        }).First(w => w.WsId == id);
    }

    public Task<WritingSystem> UpdateWritingSystem(WritingSystem before, WritingSystem after, IMiniLcmApi? api)
    {
        return Task.FromResult(after);
    }

    public Task MoveWritingSystem(WritingSystemId id, WritingSystemType type, BetweenPosition<WritingSystemId?> between)
    {
        return Task.CompletedTask;
    }

    public Task<PartOfSpeech> CreatePartOfSpeech(PartOfSpeech partOfSpeech)
    {
        // Reads won't surface an ignored write, so return the input rather than re-reading.
        return Task.FromResult(partOfSpeech);
    }

    public Task<PartOfSpeech> UpdatePartOfSpeech(Guid id, UpdateObjectInput<PartOfSpeech> update)
    {
        return api.GetPartOfSpeech(id)!;
    }

    public Task<PartOfSpeech> UpdatePartOfSpeech(PartOfSpeech before, PartOfSpeech after, IMiniLcmApi? api)
    {
        return Task.FromResult(after);
    }

    public Task DeletePartOfSpeech(Guid id)
    {
        return Task.CompletedTask;
    }

    public Task<SemanticDomain> CreateSemanticDomain(SemanticDomain semanticDomain)
    {
        return Task.FromResult(semanticDomain);
    }

    public Task<SemanticDomain> UpdateSemanticDomain(Guid id, UpdateObjectInput<SemanticDomain> update)
    {
        return api.GetSemanticDomain(id)!;
    }

    public Task<SemanticDomain> UpdateSemanticDomain(SemanticDomain before, SemanticDomain after, IMiniLcmApi? api)
    {
        return Task.FromResult(after);
    }

    public Task DeleteSemanticDomain(Guid id)
    {
        return Task.CompletedTask;
    }

    public Task<ComplexFormType> CreateComplexFormType(ComplexFormType complexFormType)
    {
        return Task.FromResult(complexFormType);
    }

    public async Task<ComplexFormType> UpdateComplexFormType(Guid id, UpdateObjectInput<ComplexFormType> update)
    {
        return await api.GetComplexFormType(id) ?? throw new NullReferenceException($"unable to find complex form type with id {id}");
    }

    public Task<ComplexFormType> UpdateComplexFormType(ComplexFormType before, ComplexFormType after, IMiniLcmApi? api)
    {
        return Task.FromResult(after);
    }

    public Task DeleteComplexFormType(Guid id)
    {
        return Task.CompletedTask;
    }

    public Task<MorphType> CreateMorphType(MorphType morphType)
    {
        return Task.FromResult(morphType);
    }

    public async Task<MorphType> UpdateMorphType(Guid id, UpdateObjectInput<MorphType> update)
    {
        return await api.GetMorphType(id) ?? throw new NullReferenceException($"unable to find morph type with id {id}");
    }

    public Task<MorphType> UpdateMorphType(MorphType before, MorphType after, IMiniLcmApi? api)
    {
        return Task.FromResult(after);
    }

    public Task<Entry> CreateEntry(Entry entry, CreateEntryOptions? options)
    {
        options ??= new CreateEntryOptions();
        if (options.IncludeComplexFormsAndComponents)
            return Task.FromResult(entry);
        else
            return Task.FromResult(entry with { Components = [], ComplexForms = [] });
    }

    public Task<Entry> UpdateEntry(Guid id, UpdateObjectInput<Entry> update)
    {
        return api.GetEntry(id)!;
    }

    public Task<Entry> UpdateEntry(Entry before, Entry after, IMiniLcmApi? api)
    {
        return Task.FromResult(after);
    }

    public Task DeleteEntry(Guid id)
    {
        return Task.CompletedTask;
    }

    public Task RemoveComplexFormType(Guid entryId, Guid complexFormTypeId)
    {
        return Task.CompletedTask;
    }

    public Task<Sense> CreateSense(Guid entryId, Sense sense, BetweenPosition? position = null)
    {
        return Task.FromResult(sense);
    }

    public async Task<Sense> UpdateSense(Guid entryId, Guid senseId, UpdateObjectInput<Sense> update)
    {
        var entry = await api.GetEntry(entryId) ??
                    throw new NullReferenceException($"unable to find entry with id {entryId}");
        var sense = entry.Senses.First(s => s.Id == senseId);
        return sense;
    }

    public async Task<Sense> UpdateSense(Guid entryId, Sense before, Sense after, IMiniLcmApi? api)
    {
        return await ReadApi.GetSense(entryId, after.Id) ?? throw new NullReferenceException($"unable to find sense with id {after.Id}");
    }

    public Task MoveSense(Guid entryId, Guid senseId, BetweenPosition between)
    {
        return Task.CompletedTask;
    }

    public Task DeleteSense(Guid entryId, Guid senseId)
    {
        return Task.CompletedTask;
    }

    public Task AddSemanticDomainToSense(Guid senseId, SemanticDomain semanticDomain)
    {
        return Task.CompletedTask;
    }

    public Task RemoveSemanticDomainFromSense(Guid senseId, Guid semanticDomainId)
    {
        return Task.CompletedTask;
    }

    public Task SetSensePartOfSpeech(Guid senseId, Guid? partOfSpeechId)
    {
        return Task.CompletedTask;
    }

    public Task<ExampleSentence> CreateExampleSentence(Guid entryId, Guid senseId, ExampleSentence exampleSentence, BetweenPosition? position = null)
    {
        return Task.FromResult(exampleSentence);
    }

    public async Task<ExampleSentence> UpdateExampleSentence(Guid entryId,
        Guid senseId,
        Guid exampleSentenceId,
        UpdateObjectInput<ExampleSentence> update)
    {
        var exampleSentence = await api.GetExampleSentence(entryId, senseId, exampleSentenceId);
        return exampleSentence ?? throw new NullReferenceException($"unable to find example sentence with id {exampleSentenceId}");
    }

    public Task<ExampleSentence> UpdateExampleSentence(Guid entryId,
        Guid senseId,
        ExampleSentence before,
        ExampleSentence after,
        IMiniLcmApi? api)
    {
        return Task.FromResult(after);
    }

    public Task MoveExampleSentence(Guid entryId, Guid senseId, Guid exampleId, BetweenPosition between)
    {
        return Task.CompletedTask;
    }

    public Task DeleteExampleSentence(Guid entryId, Guid senseId, Guid exampleSentenceId)
    {
        return Task.CompletedTask;
    }

    public Task AddTranslation(Guid entryId, Guid senseId, Guid exampleSentenceId, Translation translation)
    {
        return Task.CompletedTask;
    }

    public Task RemoveTranslation(Guid entryId, Guid senseId, Guid exampleSentenceId, Guid translationId)
    {
        return Task.CompletedTask;
    }

    public Task UpdateTranslation(Guid entryId,
        Guid senseId,
        Guid exampleSentenceId,
        Guid translationId,
        UpdateObjectInput<Translation> update)
    {
        return Task.CompletedTask;
    }


    public Task<Picture> CreatePicture(Guid entryId, Guid senseId, Picture picture, BetweenPosition? position = null)
    {
        return Task.FromResult(picture);
    }

    public async Task<Picture> UpdatePicture(Guid entryId,
        Guid senseId,
        Guid pictureId,
        UpdateObjectInput<Picture> update)
    {
        var picture = await api.GetPicture(entryId, senseId, pictureId);
        return picture ?? throw new NullReferenceException($"unable to find picture with id {pictureId}");
    }

    public Task<Picture> UpdatePicture(Guid entryId,
        Guid senseId,
        Picture before,
        Picture after,
        IMiniLcmApi? api)
    {
        return Task.FromResult(after);
    }

    public Task MovePicture(Guid entryId, Guid senseId, Guid exampleId, BetweenPosition between)
    {
        return Task.CompletedTask;
    }

    public Task DeletePicture(Guid entryId, Guid senseId, Guid pictureId)
    {
        return Task.CompletedTask;
    }

    public Task<ComplexFormComponent> CreateComplexFormComponent(ComplexFormComponent complexFormComponent, BetweenPosition<ComplexFormComponent>? between = null)
    {
        return Task.FromResult(complexFormComponent);
    }

    public Task MoveComplexFormComponent(ComplexFormComponent complexFormComponent, BetweenPosition<ComplexFormComponent> between)
    {
        return Task.CompletedTask;
    }

    public Task DeleteComplexFormComponent(ComplexFormComponent complexFormComponent)
    {
        return Task.CompletedTask;
    }

    public Task AddComplexFormType(Guid entryId, Guid complexFormTypeId)
    {
        return Task.CompletedTask;
    }

    public Task<Publication> CreatePublication(Publication pub)
    {
        return Task.FromResult(pub);
    }

    public async Task<Publication> UpdatePublication(Guid id, UpdateObjectInput<Publication> update)
    {
        return await api.GetPublication(id) ?? throw NotFoundException.ForType<Publication>(id);
    }

    public async Task<Publication> UpdatePublication(Publication before, Publication after, IMiniLcmApi? api = null)
    {
        return await ReadApi.GetPublication(before.Id) ?? throw NotFoundException.ForType<Publication>(before.Id);
    }

    public Task DeletePublication(Guid id)
    {
        return Task.CompletedTask;
    }

    public Task AddPublication(Guid entryId, Guid publicationId)
    {
        return Task.CompletedTask;
    }

    public Task RemovePublication(Guid entryId, Guid publicationId)
    {
        return Task.CompletedTask;
    }

    #region Submit (sync's result-less write variants)
    // Implemented explicitly rather than falling back to the interface default, which routes to the returning
    // Update* and re-reads the object — that would throw when a dry run of a conflicted project hits an object
    // the other side has already deleted.
    public Task SubmitUpdateEntry(Guid id, UpdateObjectInput<Entry> update)
    {
        return Task.CompletedTask;
    }

    public Task SubmitUpdateSense(Guid entryId, Guid senseId, UpdateObjectInput<Sense> update)
    {
        return Task.CompletedTask;
    }

    public Task SubmitUpdateExampleSentence(Guid entryId, Guid senseId, Guid exampleSentenceId, UpdateObjectInput<ExampleSentence> update)
    {
        return Task.CompletedTask;
    }

    public Task SubmitCreateSense(Guid entryId, Sense sense, BetweenPosition? position = null)
    {
        return Task.CompletedTask;
    }

    public Task SubmitCreateExampleSentence(Guid entryId, Guid senseId, ExampleSentence exampleSentence, BetweenPosition? position = null)
    {
        return Task.CompletedTask;
    }

    public Task SubmitCreateComplexFormComponent(ComplexFormComponent complexFormComponent, BetweenPosition<ComplexFormComponent>? position = null)
    {
        return Task.CompletedTask;
    }

    public Task SubmitUpdatePartOfSpeech(Guid id, UpdateObjectInput<PartOfSpeech> update)
    {
        return Task.CompletedTask;
    }

    public Task SubmitUpdatePublication(Guid id, UpdateObjectInput<Publication> update)
    {
        return Task.CompletedTask;
    }

    public Task SubmitUpdateSemanticDomain(Guid id, UpdateObjectInput<SemanticDomain> update)
    {
        return Task.CompletedTask;
    }

    public Task SubmitUpdateComplexFormType(Guid id, UpdateObjectInput<ComplexFormType> update)
    {
        return Task.CompletedTask;
    }
    #endregion
}
