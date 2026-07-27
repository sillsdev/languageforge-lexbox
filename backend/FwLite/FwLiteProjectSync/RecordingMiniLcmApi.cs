using MiniLcm;
using MiniLcm.Models;
using MiniLcm.SyncHelpers;

namespace FwLiteProjectSync;

/// <summary>
/// Records each write and forwards it to the wrapped api. Used for both sides of a dry-run sync, differing only
/// in what it wraps: the CRDT side wraps a throwaway copy of the project (see
/// <see cref="LcmCrdt.CrdtProjectsService.OpenTemporaryProjectCopy"/>), so writes really apply and the sync's
/// read-back of its own writes is faithful; the fwdata side wraps a <see cref="ReadonlyMiniLcmApi"/>, so writes
/// are recorded but discarded (its file must not change).
///
/// The record strings are kept identical to what the pre-split dry-run api produced. Reads are forwarded by
/// BeaKona; writes are NOT auto-forwarded, so the compiler enforces that every one is implemented here and thus
/// recorded — nothing can slip through unrecorded.
/// </summary>
public partial class RecordingMiniLcmApi(IMiniLcmApi api) : IMiniLcmApi
{
    private readonly IMiniLcmApi _api = api;

    [BeaKona.AutoInterface]
    private IMiniLcmReadApi ReadApi => _api;

    public List<DryRunRecord> DryRunRecords { get; } = [];

    // Don't dispose the wrapped api: it belongs to the caller (the CRDT copy is disposed by its
    // TempCrdtProjectCopy handle; the fwdata api by whoever opened it).
    public void Dispose()
    {
    }

    public Task<WritingSystem> CreateWritingSystem(WritingSystem writingSystem, BetweenPosition<WritingSystemId?>? position = null)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(CreateWritingSystem),
        $"Create writing system {writingSystem.Type} between {position?.Previous} and {position?.Next}"));
        return _api.CreateWritingSystem(writingSystem, position);
    }

    public Task<WritingSystem> UpdateWritingSystem(WritingSystemId id,
        WritingSystemType type,
        UpdateObjectInput<WritingSystem> update)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(UpdateWritingSystem),
            $"Update writing system {type}, changes: {update.Summarize()}"));
        return _api.UpdateWritingSystem(id, type, update);
    }

    public Task<WritingSystem> UpdateWritingSystem(WritingSystem before, WritingSystem after, IMiniLcmApi? api)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(UpdateWritingSystem), $"Update {after.Type} writing system {after.WsId}"));
        return _api.UpdateWritingSystem(before, after, api);
    }

    public Task MoveWritingSystem(WritingSystemId id, WritingSystemType type, BetweenPosition<WritingSystemId?> between)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(MoveWritingSystem), $"Move writing system {id} between {between.Previous} and {between.Next}"));
        return _api.MoveWritingSystem(id, type, between);
    }

    public Task<PartOfSpeech> CreatePartOfSpeech(PartOfSpeech partOfSpeech)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(CreatePartOfSpeech), $"Create part of speech {partOfSpeech.Name}"));
        return _api.CreatePartOfSpeech(partOfSpeech);
    }

    public Task<PartOfSpeech> UpdatePartOfSpeech(Guid id, UpdateObjectInput<PartOfSpeech> update)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(UpdatePartOfSpeech), $"Update part of speech {id}"));
        return _api.UpdatePartOfSpeech(id, update);
    }

    public Task<PartOfSpeech> UpdatePartOfSpeech(PartOfSpeech before, PartOfSpeech after, IMiniLcmApi? api)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(UpdatePartOfSpeech), $"Update part of speech {after.Id}"));
        return _api.UpdatePartOfSpeech(before, after, api);
    }

    public Task DeletePartOfSpeech(Guid id)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(DeletePartOfSpeech), $"Delete part of speech {id}"));
        return _api.DeletePartOfSpeech(id);
    }

    public Task<SemanticDomain> CreateSemanticDomain(SemanticDomain semanticDomain)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(CreateSemanticDomain),
            $"Create semantic domain {semanticDomain.Name}"));
        return _api.CreateSemanticDomain(semanticDomain);
    }

    public Task<SemanticDomain> UpdateSemanticDomain(Guid id, UpdateObjectInput<SemanticDomain> update)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(UpdateSemanticDomain), $"Update semantic domain {id}"));
        return _api.UpdateSemanticDomain(id, update);
    }

    public Task<SemanticDomain> UpdateSemanticDomain(SemanticDomain before, SemanticDomain after, IMiniLcmApi? api)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(UpdateSemanticDomain), $"Update semantic domain {after.Id}"));
        return _api.UpdateSemanticDomain(before, after, api);
    }

    public Task DeleteSemanticDomain(Guid id)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(DeleteSemanticDomain), $"Delete semantic domain {id}"));
        return _api.DeleteSemanticDomain(id);
    }

    public Task<ComplexFormType> CreateComplexFormType(ComplexFormType complexFormType)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(CreateComplexFormType),
            $"Create complex form type {complexFormType.Name}"));
        return _api.CreateComplexFormType(complexFormType);
    }

    public Task<ComplexFormType> UpdateComplexFormType(Guid id, UpdateObjectInput<ComplexFormType> update)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(UpdateComplexFormType), $"Update complex form type {id}"));
        return _api.UpdateComplexFormType(id, update);
    }

    public Task<ComplexFormType> UpdateComplexFormType(ComplexFormType before, ComplexFormType after, IMiniLcmApi? api)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(UpdateComplexFormType), $"Update complex form type {after.Id}"));
        return _api.UpdateComplexFormType(before, after, api);
    }

    public Task DeleteComplexFormType(Guid id)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(DeleteComplexFormType), $"Delete complex form type {id}"));
        return _api.DeleteComplexFormType(id);
    }

    public Task<MorphType> CreateMorphType(MorphType morphType)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(CreateMorphType), $"Create morph type {morphType.Kind} ({morphType.Id})"));
        return _api.CreateMorphType(morphType);
    }

    public Task<MorphType> UpdateMorphType(Guid id, UpdateObjectInput<MorphType> update)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(UpdateMorphType), $"Update morph type {id}"));
        return _api.UpdateMorphType(id, update);
    }

    public Task<MorphType> UpdateMorphType(MorphType before, MorphType after, IMiniLcmApi? api)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(UpdateMorphType), $"Update morph type {after.Id}"));
        return _api.UpdateMorphType(before, after, api);
    }

    public Task<Entry> CreateEntry(Entry entry, CreateEntryOptions? options = null)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(CreateEntry), $"Create entry {entry.Headword()} ({options ?? new CreateEntryOptions()})"));
        return _api.CreateEntry(entry, options);
    }

    public Task<Entry> UpdateEntry(Guid id, UpdateObjectInput<Entry> update)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(UpdateEntry), $"Update entry {id}"));
        return _api.UpdateEntry(id, update);
    }

    public Task<Entry> UpdateEntry(Entry before, Entry after, IMiniLcmApi? api)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(UpdateEntry), $"Update entry {after.Id}"));
        return _api.UpdateEntry(before, after, api);
    }

    public Task DeleteEntry(Guid id)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(DeleteEntry), $"Delete entry {id}"));
        return _api.DeleteEntry(id);
    }

    public Task RemoveComplexFormType(Guid entryId, Guid complexFormTypeId)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(RemoveComplexFormType), $"Remove complex form type {complexFormTypeId}, from entry {entryId}"));
        return _api.RemoveComplexFormType(entryId, complexFormTypeId);
    }

    public Task<Sense> CreateSense(Guid entryId, Sense sense, BetweenPosition? position = null)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(CreateSense), $"Create sense {sense.Gloss} between {position?.Previous} and {position?.Next}"));
        return _api.CreateSense(entryId, sense, position);
    }

    public Task<Sense> UpdateSense(Guid entryId, Guid senseId, UpdateObjectInput<Sense> update)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(UpdateSense),
            $"Update sense {senseId}, changes: {update.Summarize()}"));
        return _api.UpdateSense(entryId, senseId, update);
    }

    public Task<Sense> UpdateSense(Guid entryId, Sense before, Sense after, IMiniLcmApi? api)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(UpdateSense),
            $"Update sense {after.Id}"));
        return _api.UpdateSense(entryId, before, after, api);
    }

    public Task MoveSense(Guid entryId, Guid senseId, BetweenPosition between)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(MoveSense), $"Move sense {senseId} between {between.Previous} and {between.Next}"));
        return _api.MoveSense(entryId, senseId, between);
    }

    public Task DeleteSense(Guid entryId, Guid senseId)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(DeleteSense), $"Delete sense {senseId}"));
        return _api.DeleteSense(entryId, senseId);
    }

    public Task AddSemanticDomainToSense(Guid senseId, SemanticDomain semanticDomain)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(AddSemanticDomainToSense), $"Add semantic domain {semanticDomain.Name}"));
        return _api.AddSemanticDomainToSense(senseId, semanticDomain);
    }

    public Task RemoveSemanticDomainFromSense(Guid senseId, Guid semanticDomainId)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(RemoveSemanticDomainFromSense), $"Remove semantic domain {semanticDomainId}"));
        return _api.RemoveSemanticDomainFromSense(senseId, semanticDomainId);
    }

    public Task SetSensePartOfSpeech(Guid senseId, Guid? partOfSpeechId)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(SetSensePartOfSpeech), $"Set part of speech {partOfSpeechId}"));
        return _api.SetSensePartOfSpeech(senseId, partOfSpeechId);
    }

    public Task<ExampleSentence> CreateExampleSentence(Guid entryId, Guid senseId, ExampleSentence exampleSentence, BetweenPosition? position = null)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(CreateExampleSentence), $"Create example sentence {exampleSentence.Sentence} between {position?.Previous} and {position?.Next}"));
        return _api.CreateExampleSentence(entryId, senseId, exampleSentence, position);
    }

    public Task<ExampleSentence> UpdateExampleSentence(Guid entryId,
        Guid senseId,
        Guid exampleSentenceId,
        UpdateObjectInput<ExampleSentence> update)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(UpdateExampleSentence),
            $"Update example sentence {exampleSentenceId}, changes: {update.Summarize()}"));
        return _api.UpdateExampleSentence(entryId, senseId, exampleSentenceId, update);
    }

    public Task<ExampleSentence> UpdateExampleSentence(Guid entryId,
        Guid senseId,
        ExampleSentence before,
        ExampleSentence after,
        IMiniLcmApi? api)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(UpdateExampleSentence), $"Update example sentence {after.Id}"));
        return _api.UpdateExampleSentence(entryId, senseId, before, after, api);
    }

    public Task MoveExampleSentence(Guid entryId, Guid senseId, Guid exampleId, BetweenPosition between)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(MoveExampleSentence), $"Move example sentence {exampleId} between {between.Previous} and {between.Next}"));
        return _api.MoveExampleSentence(entryId, senseId, exampleId, between);
    }

    public Task DeleteExampleSentence(Guid entryId, Guid senseId, Guid exampleSentenceId)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(DeleteExampleSentence), $"Delete example sentence {exampleSentenceId}"));
        return _api.DeleteExampleSentence(entryId, senseId, exampleSentenceId);
    }

    public Task AddTranslation(Guid entryId, Guid senseId, Guid exampleSentenceId, Translation translation)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(AddTranslation), $"Add translation {translation.Id} to example sentence {exampleSentenceId}"));
        return _api.AddTranslation(entryId, senseId, exampleSentenceId, translation);
    }

    public Task RemoveTranslation(Guid entryId, Guid senseId, Guid exampleSentenceId, Guid translationId)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(RemoveTranslation), $"Remove translation {translationId} from example sentence {exampleSentenceId}"));
        return _api.RemoveTranslation(entryId, senseId, exampleSentenceId, translationId);
    }

    public Task UpdateTranslation(Guid entryId,
        Guid senseId,
        Guid exampleSentenceId,
        Guid translationId,
        UpdateObjectInput<Translation> update)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(UpdateTranslation), $"Update translation {translationId} in example sentence {exampleSentenceId}"));
        return _api.UpdateTranslation(entryId, senseId, exampleSentenceId, translationId, update);
    }


    public Task<Picture> CreatePicture(Guid entryId, Guid senseId, Picture picture, BetweenPosition? position = null)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(CreatePicture), $"Create picture {picture.Caption} between {position?.Previous} and {position?.Next}"));
        return _api.CreatePicture(entryId, senseId, picture, position);
    }

    public Task<Picture> UpdatePicture(Guid entryId,
        Guid senseId,
        Guid pictureId,
        UpdateObjectInput<Picture> update)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(UpdatePicture),
            $"Update picture {pictureId}, changes: {update.Summarize()}"));
        return _api.UpdatePicture(entryId, senseId, pictureId, update);
    }

    public Task<Picture> UpdatePicture(Guid entryId,
        Guid senseId,
        Picture before,
        Picture after,
        IMiniLcmApi? api)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(UpdatePicture), $"Update picture {after.Id}"));
        return _api.UpdatePicture(entryId, senseId, before, after, api);
    }

    public Task MovePicture(Guid entryId, Guid senseId, Guid exampleId, BetweenPosition between)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(MovePicture), $"Move picture {exampleId} between {between.Previous} and {between.Next}"));
        return _api.MovePicture(entryId, senseId, exampleId, between);
    }

    public Task DeletePicture(Guid entryId, Guid senseId, Guid pictureId)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(DeletePicture), $"Delete picture {pictureId}"));
        return _api.DeletePicture(entryId, senseId, pictureId);
    }

    public Task<ComplexFormComponent> CreateComplexFormComponent(ComplexFormComponent complexFormComponent, BetweenPosition<ComplexFormComponent>? between = null)
    {
        var complexFormName = ComplexFormName(complexFormComponent);
        var componentName = ComplexFormComponentName(complexFormComponent);
        var previous = ComplexFormComponentName(between?.Previous);
        var next = ComplexFormComponentName(between?.Next);
        DryRunRecords.Add(new DryRunRecord(nameof(CreateComplexFormComponent), $"Create complex form component complex entry: {complexFormName}, component entry: {componentName}, between {previous} and {next}"));
        return _api.CreateComplexFormComponent(complexFormComponent, between);
    }

    public Task MoveComplexFormComponent(ComplexFormComponent complexFormComponent, BetweenPosition<ComplexFormComponent> between)
    {
        var componentName = ComplexFormComponentName(complexFormComponent);
        var previous = ComplexFormComponentName(between.Previous);
        var next = ComplexFormComponentName(between.Next);
        DryRunRecords.Add(new DryRunRecord(nameof(MoveComplexFormComponent), $"Move complex form component {componentName} between {previous} and {next}"));
        return _api.MoveComplexFormComponent(complexFormComponent, between);
    }

    public Task DeleteComplexFormComponent(ComplexFormComponent complexFormComponent)
    {
        var componentName = ComplexFormComponentName(complexFormComponent);
        DryRunRecords.Add(new DryRunRecord(nameof(DeleteComplexFormComponent), $"Delete complex form component: {componentName}"));
        return _api.DeleteComplexFormComponent(complexFormComponent);
    }

    public Task AddComplexFormType(Guid entryId, Guid complexFormTypeId)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(AddComplexFormType), $"Add complex form type {complexFormTypeId}, to entry {entryId}"));
        return _api.AddComplexFormType(entryId, complexFormTypeId);
    }

    public Task<Publication> CreatePublication(Publication pub)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(CreatePublication), $"Create publication {pub.Id}"));
        return _api.CreatePublication(pub);
    }

    public Task<Publication> UpdatePublication(Guid id, UpdateObjectInput<Publication> update)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(UpdatePublication), $"Update publication {id}"));
        return _api.UpdatePublication(id, update);
    }

    public Task<Publication> UpdatePublication(Publication before, Publication after, IMiniLcmApi? api = null)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(UpdatePublication), $"Update publication {before.Id}"));
        return _api.UpdatePublication(before, after, api);
    }

    public Task DeletePublication(Guid id)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(DeletePublication), $"Delete publication {id}"));
        return _api.DeletePublication(id);
    }

    public Task AddPublication(Guid entryId, Guid publicationId)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(AddPublication), $"Add publication {publicationId} to entry {entryId}"));
        return _api.AddPublication(entryId, publicationId);
    }

    public Task RemovePublication(Guid entryId, Guid publicationId)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(RemovePublication), $"Remove publication {publicationId} from entry {entryId}"));
        return _api.RemovePublication(entryId, publicationId);
    }

    #region Submit (sync's result-less write variants)
    // Forward to the wrapped api's Submit* so the CRDT side keeps its delete-wins behaviour (and the fwdata
    // ReadonlyMiniLcmApi swallows it). The two Submit* the pre-split api left unimplemented still default to
    // the returning Move*/Update* above, which record.
    public Task SubmitUpdateEntry(Guid id, UpdateObjectInput<Entry> update)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(SubmitUpdateEntry), $"Update entry {id}"));
        return _api.SubmitUpdateEntry(id, update);
    }

    public Task SubmitUpdateSense(Guid entryId, Guid senseId, UpdateObjectInput<Sense> update)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(SubmitUpdateSense), $"Update sense {senseId}, changes: {update.Summarize()}"));
        return _api.SubmitUpdateSense(entryId, senseId, update);
    }

    public Task SubmitUpdateExampleSentence(Guid entryId, Guid senseId, Guid exampleSentenceId, UpdateObjectInput<ExampleSentence> update)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(SubmitUpdateExampleSentence), $"Update example sentence {exampleSentenceId}, changes: {update.Summarize()}"));
        return _api.SubmitUpdateExampleSentence(entryId, senseId, exampleSentenceId, update);
    }

    public Task SubmitCreateSense(Guid entryId, Sense sense, BetweenPosition? position = null)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(SubmitCreateSense), $"Create sense {sense.Gloss}"));
        return _api.SubmitCreateSense(entryId, sense, position);
    }

    public Task SubmitCreateExampleSentence(Guid entryId, Guid senseId, ExampleSentence exampleSentence, BetweenPosition? position = null)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(SubmitCreateExampleSentence), $"Create example sentence {exampleSentence.Sentence}"));
        return _api.SubmitCreateExampleSentence(entryId, senseId, exampleSentence, position);
    }

    public Task SubmitCreateComplexFormComponent(ComplexFormComponent complexFormComponent, BetweenPosition<ComplexFormComponent>? position = null)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(SubmitCreateComplexFormComponent), $"Create complex form component {ComplexFormComponentName(complexFormComponent)}"));
        return _api.SubmitCreateComplexFormComponent(complexFormComponent, position);
    }

    public Task SubmitUpdatePartOfSpeech(Guid id, UpdateObjectInput<PartOfSpeech> update)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(SubmitUpdatePartOfSpeech), $"Update part of speech {id}"));
        return _api.SubmitUpdatePartOfSpeech(id, update);
    }

    public Task SubmitUpdatePublication(Guid id, UpdateObjectInput<Publication> update)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(SubmitUpdatePublication), $"Update publication {id}"));
        return _api.SubmitUpdatePublication(id, update);
    }

    public Task SubmitUpdateSemanticDomain(Guid id, UpdateObjectInput<SemanticDomain> update)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(SubmitUpdateSemanticDomain), $"Update semantic domain {id}"));
        return _api.SubmitUpdateSemanticDomain(id, update);
    }

    public Task SubmitUpdateComplexFormType(Guid id, UpdateObjectInput<ComplexFormType> update)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(SubmitUpdateComplexFormType), $"Update complex form type {id}"));
        return _api.SubmitUpdateComplexFormType(id, update);
    }
    #endregion

    private string ComplexFormComponentName(ComplexFormComponent? component)
    {
        if (component == null) return "null";
        return $"{component.ComponentHeadword} ({component.ComponentEntryId}:{component.ComponentSenseId})";
    }

    private string ComplexFormName(ComplexFormComponent? component)
    {
        if (component == null) return "null";
        return $"{component.ComplexFormHeadword} ({component.ComplexFormEntryId})";
    }
}
