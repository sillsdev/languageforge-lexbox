using MiniLcm;
using MiniLcm.Models;
using MiniLcm.SyncHelpers;

namespace FwLiteProjectSync;

/// <summary>
/// Records each write and forwards it to the wrapped api. Used for both sides of a dry-run sync, differing only
/// in what it wraps: the CRDT side wraps a throwaway copy of the project (see
/// <see cref="LcmCrdt.CrdtProjectsService.OpenProjectCopy"/>), so writes really apply and the sync's read-back of
/// its own writes is faithful; the fwdata side wraps a <see cref="ReadonlyMiniLcmApi"/>, so writes are recorded
/// but discarded (its file must not change).
///
/// Reads, <c>Submit*</c>, and any write not overridden here are auto-forwarded by AutoInterface, so the class is
/// correct even where it doesn't record — the overrides only add the human-readable record entries.
/// </summary>
public partial class RecordingMiniLcmApi(IMiniLcmApi api) : IMiniLcmApi
{
    [BeaKona.AutoInterface(IncludeBaseInterfaces = true, MemberMatch = BeaKona.MemberMatchTypes.Any)]
    private readonly IMiniLcmApi _api = api;

    public List<DryRunRecord> DryRunRecords { get; } = [];

    // Don't dispose the wrapped api: it belongs to the copy's service scope, which is disposed by the
    // TempCrdtProjectCopy handle.
    public void Dispose() { }

    private Task<T> Record<T>(string method, string description, Task<T> operation)
    {
        DryRunRecords.Add(new DryRunRecord(method, description));
        return operation;
    }

    private Task Record(string method, string description, Task operation)
    {
        DryRunRecords.Add(new DryRunRecord(method, description));
        return operation;
    }

    #region WritingSystem
    public Task<WritingSystem> CreateWritingSystem(WritingSystem writingSystem, BetweenPosition<WritingSystemId?>? between = null) =>
        Record(nameof(CreateWritingSystem), $"Create writing system {writingSystem.Type} {writingSystem.WsId}", _api.CreateWritingSystem(writingSystem, between));

    public Task<WritingSystem> UpdateWritingSystem(WritingSystemId id, WritingSystemType type, UpdateObjectInput<WritingSystem> update) =>
        Record(nameof(UpdateWritingSystem), $"Update writing system {type} {id}", _api.UpdateWritingSystem(id, type, update));

    public Task<WritingSystem> UpdateWritingSystem(WritingSystem before, WritingSystem after, IMiniLcmApi? api = null) =>
        Record(nameof(UpdateWritingSystem), $"Update writing system {after.Type} {after.WsId}", _api.UpdateWritingSystem(before, after, api));

    public Task MoveWritingSystem(WritingSystemId id, WritingSystemType type, BetweenPosition<WritingSystemId?> between) =>
        Record(nameof(MoveWritingSystem), $"Move writing system {type} {id}", _api.MoveWritingSystem(id, type, between));
    #endregion

    #region PartOfSpeech
    public Task<PartOfSpeech> CreatePartOfSpeech(PartOfSpeech partOfSpeech) =>
        Record(nameof(CreatePartOfSpeech), $"Create part of speech {partOfSpeech.Id}", _api.CreatePartOfSpeech(partOfSpeech));

    public Task<PartOfSpeech> UpdatePartOfSpeech(Guid id, UpdateObjectInput<PartOfSpeech> update) =>
        Record(nameof(UpdatePartOfSpeech), $"Update part of speech {id}", _api.UpdatePartOfSpeech(id, update));

    public Task<PartOfSpeech> UpdatePartOfSpeech(PartOfSpeech before, PartOfSpeech after, IMiniLcmApi? api = null) =>
        Record(nameof(UpdatePartOfSpeech), $"Update part of speech {after.Id}", _api.UpdatePartOfSpeech(before, after, api));

    public Task DeletePartOfSpeech(Guid id) =>
        Record(nameof(DeletePartOfSpeech), $"Delete part of speech {id}", _api.DeletePartOfSpeech(id));
    #endregion

    #region Publication
    public Task<Publication> CreatePublication(Publication pub) =>
        Record(nameof(CreatePublication), $"Create publication {pub.Id}", _api.CreatePublication(pub));

    public Task<Publication> UpdatePublication(Guid id, UpdateObjectInput<Publication> update) =>
        Record(nameof(UpdatePublication), $"Update publication {id}", _api.UpdatePublication(id, update));

    public Task<Publication> UpdatePublication(Publication before, Publication after, IMiniLcmApi? api = null) =>
        Record(nameof(UpdatePublication), $"Update publication {after.Id}", _api.UpdatePublication(before, after, api));

    public Task DeletePublication(Guid id) =>
        Record(nameof(DeletePublication), $"Delete publication {id}", _api.DeletePublication(id));
    #endregion

    #region SemanticDomain
    public Task<SemanticDomain> CreateSemanticDomain(SemanticDomain semanticDomain) =>
        Record(nameof(CreateSemanticDomain), $"Create semantic domain {semanticDomain.Id}", _api.CreateSemanticDomain(semanticDomain));

    public Task<SemanticDomain> UpdateSemanticDomain(Guid id, UpdateObjectInput<SemanticDomain> update) =>
        Record(nameof(UpdateSemanticDomain), $"Update semantic domain {id}", _api.UpdateSemanticDomain(id, update));

    public Task<SemanticDomain> UpdateSemanticDomain(SemanticDomain before, SemanticDomain after, IMiniLcmApi? api = null) =>
        Record(nameof(UpdateSemanticDomain), $"Update semantic domain {after.Id}", _api.UpdateSemanticDomain(before, after, api));

    public Task DeleteSemanticDomain(Guid id) =>
        Record(nameof(DeleteSemanticDomain), $"Delete semantic domain {id}", _api.DeleteSemanticDomain(id));
    #endregion

    #region ComplexFormType
    public Task<ComplexFormType> CreateComplexFormType(ComplexFormType complexFormType) =>
        Record(nameof(CreateComplexFormType), $"Create complex form type {complexFormType.Id}", _api.CreateComplexFormType(complexFormType));

    public Task<ComplexFormType> UpdateComplexFormType(Guid id, UpdateObjectInput<ComplexFormType> update) =>
        Record(nameof(UpdateComplexFormType), $"Update complex form type {id}", _api.UpdateComplexFormType(id, update));

    public Task<ComplexFormType> UpdateComplexFormType(ComplexFormType before, ComplexFormType after, IMiniLcmApi? api = null) =>
        Record(nameof(UpdateComplexFormType), $"Update complex form type {after.Id}", _api.UpdateComplexFormType(before, after, api));

    public Task DeleteComplexFormType(Guid id) =>
        Record(nameof(DeleteComplexFormType), $"Delete complex form type {id}", _api.DeleteComplexFormType(id));
    #endregion

    #region MorphType
    public Task<MorphType> CreateMorphType(MorphType morphType) =>
        Record(nameof(CreateMorphType), $"Create morph type {morphType.Kind} ({morphType.Id})", _api.CreateMorphType(morphType));

    public Task<MorphType> UpdateMorphType(Guid id, UpdateObjectInput<MorphType> update) =>
        Record(nameof(UpdateMorphType), $"Update morph type {id}", _api.UpdateMorphType(id, update));

    public Task<MorphType> UpdateMorphType(MorphType before, MorphType after, IMiniLcmApi? api = null) =>
        Record(nameof(UpdateMorphType), $"Update morph type {after.Id}", _api.UpdateMorphType(before, after, api));
    #endregion

    #region Entry
    public Task<Entry> CreateEntry(Entry entry, CreateEntryOptions? options = null) =>
        Record(nameof(CreateEntry), $"Create entry {entry.Id}", _api.CreateEntry(entry, options));

    public Task<Entry> UpdateEntry(Guid id, UpdateObjectInput<Entry> update) =>
        Record(nameof(UpdateEntry), $"Update entry {id}", _api.UpdateEntry(id, update));

    public Task<Entry> UpdateEntry(Entry before, Entry after, IMiniLcmApi? api = null) =>
        Record(nameof(UpdateEntry), $"Update entry {after.Id}", _api.UpdateEntry(before, after, api));

    public Task DeleteEntry(Guid id) =>
        Record(nameof(DeleteEntry), $"Delete entry {id}", _api.DeleteEntry(id));

    public Task<ComplexFormComponent> CreateComplexFormComponent(ComplexFormComponent complexFormComponent, BetweenPosition<ComplexFormComponent>? position = null) =>
        Record(nameof(CreateComplexFormComponent), $"Create complex form component {complexFormComponent.ComplexFormEntryId} -> {complexFormComponent.ComponentEntryId}", _api.CreateComplexFormComponent(complexFormComponent, position));

    public Task MoveComplexFormComponent(ComplexFormComponent complexFormComponent, BetweenPosition<ComplexFormComponent> between) =>
        Record(nameof(MoveComplexFormComponent), $"Move complex form component {complexFormComponent.ComplexFormEntryId} -> {complexFormComponent.ComponentEntryId}", _api.MoveComplexFormComponent(complexFormComponent, between));

    public Task DeleteComplexFormComponent(ComplexFormComponent complexFormComponent) =>
        Record(nameof(DeleteComplexFormComponent), $"Delete complex form component {complexFormComponent.ComplexFormEntryId} -> {complexFormComponent.ComponentEntryId}", _api.DeleteComplexFormComponent(complexFormComponent));

    public Task AddComplexFormType(Guid entryId, Guid complexFormTypeId) =>
        Record(nameof(AddComplexFormType), $"Add complex form type {complexFormTypeId} to entry {entryId}", _api.AddComplexFormType(entryId, complexFormTypeId));

    public Task RemoveComplexFormType(Guid entryId, Guid complexFormTypeId) =>
        Record(nameof(RemoveComplexFormType), $"Remove complex form type {complexFormTypeId} from entry {entryId}", _api.RemoveComplexFormType(entryId, complexFormTypeId));

    public Task AddPublication(Guid entryId, Guid publicationId) =>
        Record(nameof(AddPublication), $"Add publication {publicationId} to entry {entryId}", _api.AddPublication(entryId, publicationId));

    public Task RemovePublication(Guid entryId, Guid publicationId) =>
        Record(nameof(RemovePublication), $"Remove publication {publicationId} from entry {entryId}", _api.RemovePublication(entryId, publicationId));
    #endregion

    #region Sense
    public Task<Sense> CreateSense(Guid entryId, Sense sense, BetweenPosition? position = null) =>
        Record(nameof(CreateSense), $"Create sense {sense.Id} on entry {entryId}", _api.CreateSense(entryId, sense, position));

    public Task<Sense> UpdateSense(Guid entryId, Guid senseId, UpdateObjectInput<Sense> update) =>
        Record(nameof(UpdateSense), $"Update sense {senseId}", _api.UpdateSense(entryId, senseId, update));

    public Task<Sense> UpdateSense(Guid entryId, Sense before, Sense after, IMiniLcmApi? api = null) =>
        Record(nameof(UpdateSense), $"Update sense {after.Id}", _api.UpdateSense(entryId, before, after, api));

    public Task MoveSense(Guid entryId, Guid senseId, BetweenPosition position) =>
        Record(nameof(MoveSense), $"Move sense {senseId}", _api.MoveSense(entryId, senseId, position));

    public Task DeleteSense(Guid entryId, Guid senseId) =>
        Record(nameof(DeleteSense), $"Delete sense {senseId}", _api.DeleteSense(entryId, senseId));

    public Task AddSemanticDomainToSense(Guid senseId, SemanticDomain semanticDomain) =>
        Record(nameof(AddSemanticDomainToSense), $"Add semantic domain {semanticDomain.Id} to sense {senseId}", _api.AddSemanticDomainToSense(senseId, semanticDomain));

    public Task RemoveSemanticDomainFromSense(Guid senseId, Guid semanticDomainId) =>
        Record(nameof(RemoveSemanticDomainFromSense), $"Remove semantic domain {semanticDomainId} from sense {senseId}", _api.RemoveSemanticDomainFromSense(senseId, semanticDomainId));

    public Task SetSensePartOfSpeech(Guid senseId, Guid? partOfSpeechId) =>
        Record(nameof(SetSensePartOfSpeech), $"Set part of speech {partOfSpeechId} on sense {senseId}", _api.SetSensePartOfSpeech(senseId, partOfSpeechId));
    #endregion

    #region ExampleSentence
    public Task<ExampleSentence> CreateExampleSentence(Guid entryId, Guid senseId, ExampleSentence exampleSentence, BetweenPosition? position = null) =>
        Record(nameof(CreateExampleSentence), $"Create example sentence {exampleSentence.Id} on sense {senseId}", _api.CreateExampleSentence(entryId, senseId, exampleSentence, position));

    public Task<ExampleSentence> UpdateExampleSentence(Guid entryId, Guid senseId, Guid exampleSentenceId, UpdateObjectInput<ExampleSentence> update) =>
        Record(nameof(UpdateExampleSentence), $"Update example sentence {exampleSentenceId}", _api.UpdateExampleSentence(entryId, senseId, exampleSentenceId, update));

    public Task<ExampleSentence> UpdateExampleSentence(Guid entryId, Guid senseId, ExampleSentence before, ExampleSentence after, IMiniLcmApi? api = null) =>
        Record(nameof(UpdateExampleSentence), $"Update example sentence {after.Id}", _api.UpdateExampleSentence(entryId, senseId, before, after, api));

    public Task MoveExampleSentence(Guid entryId, Guid senseId, Guid exampleSentenceId, BetweenPosition position) =>
        Record(nameof(MoveExampleSentence), $"Move example sentence {exampleSentenceId}", _api.MoveExampleSentence(entryId, senseId, exampleSentenceId, position));

    public Task DeleteExampleSentence(Guid entryId, Guid senseId, Guid exampleSentenceId) =>
        Record(nameof(DeleteExampleSentence), $"Delete example sentence {exampleSentenceId}", _api.DeleteExampleSentence(entryId, senseId, exampleSentenceId));

    public Task AddTranslation(Guid entryId, Guid senseId, Guid exampleSentenceId, Translation translation) =>
        Record(nameof(AddTranslation), $"Add translation {translation.Id} to example sentence {exampleSentenceId}", _api.AddTranslation(entryId, senseId, exampleSentenceId, translation));

    public Task RemoveTranslation(Guid entryId, Guid senseId, Guid exampleSentenceId, Guid translationId) =>
        Record(nameof(RemoveTranslation), $"Remove translation {translationId} from example sentence {exampleSentenceId}", _api.RemoveTranslation(entryId, senseId, exampleSentenceId, translationId));

    public Task UpdateTranslation(Guid entryId, Guid senseId, Guid exampleSentenceId, Guid translationId, UpdateObjectInput<Translation> update) =>
        Record(nameof(UpdateTranslation), $"Update translation {translationId} in example sentence {exampleSentenceId}", _api.UpdateTranslation(entryId, senseId, exampleSentenceId, translationId, update));
    #endregion

    #region Picture
    public Task<Picture> CreatePicture(Guid entryId, Guid senseId, Picture picture, BetweenPosition? position = null) =>
        Record(nameof(CreatePicture), $"Create picture {picture.Id} on sense {senseId}", _api.CreatePicture(entryId, senseId, picture, position));

    public Task<Picture> UpdatePicture(Guid entryId, Guid senseId, Guid pictureId, UpdateObjectInput<Picture> update) =>
        Record(nameof(UpdatePicture), $"Update picture {pictureId}", _api.UpdatePicture(entryId, senseId, pictureId, update));

    public Task<Picture> UpdatePicture(Guid entryId, Guid senseId, Picture before, Picture after, IMiniLcmApi? api = null) =>
        Record(nameof(UpdatePicture), $"Update picture {after.Id}", _api.UpdatePicture(entryId, senseId, before, after, api));

    public Task MovePicture(Guid entryId, Guid senseId, Guid pictureId, BetweenPosition position) =>
        Record(nameof(MovePicture), $"Move picture {pictureId}", _api.MovePicture(entryId, senseId, pictureId, position));

    public Task DeletePicture(Guid entryId, Guid senseId, Guid pictureId) =>
        Record(nameof(DeletePicture), $"Delete picture {pictureId}", _api.DeletePicture(entryId, senseId, pictureId));
    #endregion
}
