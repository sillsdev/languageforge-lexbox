using MiniLcm;
using MiniLcm.Models;
using MiniLcm.SyncHelpers;

namespace FwLiteProjectSync;

public partial class RecordingMiniLcmApi(IMiniLcmApi api) : IMiniLcmApi
{

    public record RunRecord(string Method, string Description);

    private readonly IMiniLcmApi _api = api;

    // Writes aren't auto-forwarded, so the compiler forces each to be recorded
    [BeaKona.AutoInterface]
    private IMiniLcmReadApi ReadApi => _api;

    public List<RunRecord> RunRecords { get; } = [];

    // Wrapped api is the caller's to dispose
    public void Dispose()
    {
    }

    public Task<WritingSystem> CreateWritingSystem(WritingSystem writingSystem, BetweenPosition<WritingSystemId?>? position = null)
    {
        RunRecords.Add(new RunRecord(nameof(CreateWritingSystem),
        $"Create writing system {writingSystem.Type} between {position?.Previous} and {position?.Next}"));
        return _api.CreateWritingSystem(writingSystem, position);
    }

    public Task<WritingSystem> UpdateWritingSystem(WritingSystemId id,
        WritingSystemType type,
        UpdateObjectInput<WritingSystem> update)
    {
        RunRecords.Add(new RunRecord(nameof(UpdateWritingSystem),
            $"Update writing system {type}, changes: {update.Summarize()}"));
        return _api.UpdateWritingSystem(id, type, update);
    }

    public Task<WritingSystem> UpdateWritingSystem(WritingSystem before, WritingSystem after, IMiniLcmApi? api)
    {
        RunRecords.Add(new RunRecord(nameof(UpdateWritingSystem), $"Update {after.Type} writing system {after.WsId}"));
        return _api.UpdateWritingSystem(before, after, api);
    }

    public Task MoveWritingSystem(WritingSystemId id, WritingSystemType type, BetweenPosition<WritingSystemId?> between)
    {
        RunRecords.Add(new RunRecord(nameof(MoveWritingSystem), $"Move writing system {id} between {between.Previous} and {between.Next}"));
        return _api.MoveWritingSystem(id, type, between);
    }

    public Task<PartOfSpeech> CreatePartOfSpeech(PartOfSpeech partOfSpeech)
    {
        RunRecords.Add(new RunRecord(nameof(CreatePartOfSpeech), $"Create part of speech {partOfSpeech.Name}"));
        return _api.CreatePartOfSpeech(partOfSpeech);
    }

    public Task<PartOfSpeech> UpdatePartOfSpeech(Guid id, UpdateObjectInput<PartOfSpeech> update)
    {
        RunRecords.Add(new RunRecord(nameof(UpdatePartOfSpeech), $"Update part of speech {id}"));
        return _api.UpdatePartOfSpeech(id, update);
    }

    public Task<PartOfSpeech> UpdatePartOfSpeech(PartOfSpeech before, PartOfSpeech after, IMiniLcmApi? api)
    {
        RunRecords.Add(new RunRecord(nameof(UpdatePartOfSpeech), $"Update part of speech {after.Id}"));
        return _api.UpdatePartOfSpeech(before, after, api);
    }

    public Task DeletePartOfSpeech(Guid id)
    {
        RunRecords.Add(new RunRecord(nameof(DeletePartOfSpeech), $"Delete part of speech {id}"));
        return _api.DeletePartOfSpeech(id);
    }

    public Task<SemanticDomain> CreateSemanticDomain(SemanticDomain semanticDomain)
    {
        RunRecords.Add(new RunRecord(nameof(CreateSemanticDomain),
            $"Create semantic domain {semanticDomain.Name}"));
        return _api.CreateSemanticDomain(semanticDomain);
    }

    public Task<SemanticDomain> UpdateSemanticDomain(Guid id, UpdateObjectInput<SemanticDomain> update)
    {
        RunRecords.Add(new RunRecord(nameof(UpdateSemanticDomain), $"Update semantic domain {id}"));
        return _api.UpdateSemanticDomain(id, update);
    }

    public Task<SemanticDomain> UpdateSemanticDomain(SemanticDomain before, SemanticDomain after, IMiniLcmApi? api)
    {
        RunRecords.Add(new RunRecord(nameof(UpdateSemanticDomain), $"Update semantic domain {after.Id}"));
        return _api.UpdateSemanticDomain(before, after, api);
    }

    public Task DeleteSemanticDomain(Guid id)
    {
        RunRecords.Add(new RunRecord(nameof(DeleteSemanticDomain), $"Delete semantic domain {id}"));
        return _api.DeleteSemanticDomain(id);
    }

    public Task<ComplexFormType> CreateComplexFormType(ComplexFormType complexFormType)
    {
        RunRecords.Add(new RunRecord(nameof(CreateComplexFormType),
            $"Create complex form type {complexFormType.Name}"));
        return _api.CreateComplexFormType(complexFormType);
    }

    public Task<ComplexFormType> UpdateComplexFormType(Guid id, UpdateObjectInput<ComplexFormType> update)
    {
        RunRecords.Add(new RunRecord(nameof(UpdateComplexFormType), $"Update complex form type {id}"));
        return _api.UpdateComplexFormType(id, update);
    }

    public Task<ComplexFormType> UpdateComplexFormType(ComplexFormType before, ComplexFormType after, IMiniLcmApi? api)
    {
        RunRecords.Add(new RunRecord(nameof(UpdateComplexFormType), $"Update complex form type {after.Id}"));
        return _api.UpdateComplexFormType(before, after, api);
    }

    public Task DeleteComplexFormType(Guid id)
    {
        RunRecords.Add(new RunRecord(nameof(DeleteComplexFormType), $"Delete complex form type {id}"));
        return _api.DeleteComplexFormType(id);
    }

    public Task<MorphType> CreateMorphType(MorphType morphType)
    {
        RunRecords.Add(new RunRecord(nameof(CreateMorphType), $"Create morph type {morphType.Kind} ({morphType.Id})"));
        return _api.CreateMorphType(morphType);
    }

    public Task<MorphType> UpdateMorphType(Guid id, UpdateObjectInput<MorphType> update)
    {
        RunRecords.Add(new RunRecord(nameof(UpdateMorphType), $"Update morph type {id}"));
        return _api.UpdateMorphType(id, update);
    }

    public Task<MorphType> UpdateMorphType(MorphType before, MorphType after, IMiniLcmApi? api)
    {
        RunRecords.Add(new RunRecord(nameof(UpdateMorphType), $"Update morph type {after.Id}"));
        return _api.UpdateMorphType(before, after, api);
    }

    public Task<Entry> CreateEntry(Entry entry, CreateEntryOptions? options = null)
    {
        RunRecords.Add(new RunRecord(nameof(CreateEntry), $"Create entry {entry.Headword()} ({options ?? new CreateEntryOptions()})"));
        return _api.CreateEntry(entry, options);
    }

    public Task<Entry> UpdateEntry(Guid id, UpdateObjectInput<Entry> update)
    {
        RunRecords.Add(new RunRecord(nameof(UpdateEntry), $"Update entry {id}"));
        return _api.UpdateEntry(id, update);
    }

    public Task<Entry> UpdateEntry(Entry before, Entry after, IMiniLcmApi? api)
    {
        RunRecords.Add(new RunRecord(nameof(UpdateEntry), $"Update entry {after.Id}"));
        return _api.UpdateEntry(before, after, api);
    }

    public Task DeleteEntry(Guid id)
    {
        RunRecords.Add(new RunRecord(nameof(DeleteEntry), $"Delete entry {id}"));
        return _api.DeleteEntry(id);
    }

    public Task RemoveComplexFormType(Guid entryId, Guid complexFormTypeId)
    {
        RunRecords.Add(new RunRecord(nameof(RemoveComplexFormType), $"Remove complex form type {complexFormTypeId}, from entry {entryId}"));
        return _api.RemoveComplexFormType(entryId, complexFormTypeId);
    }

    public Task<Sense> CreateSense(Guid entryId, Sense sense, BetweenPosition? position = null)
    {
        RunRecords.Add(new RunRecord(nameof(CreateSense), $"Create sense {sense.Gloss} between {position?.Previous} and {position?.Next}"));
        return _api.CreateSense(entryId, sense, position);
    }

    public Task<Sense> UpdateSense(Guid entryId, Guid senseId, UpdateObjectInput<Sense> update)
    {
        RunRecords.Add(new RunRecord(nameof(UpdateSense),
            $"Update sense {senseId}, changes: {update.Summarize()}"));
        return _api.UpdateSense(entryId, senseId, update);
    }

    public Task<Sense> UpdateSense(Guid entryId, Sense before, Sense after, IMiniLcmApi? api)
    {
        RunRecords.Add(new RunRecord(nameof(UpdateSense),
            $"Update sense {after.Id}"));
        return _api.UpdateSense(entryId, before, after, api);
    }

    public Task MoveSense(Guid entryId, Guid senseId, BetweenPosition between)
    {
        RunRecords.Add(new RunRecord(nameof(MoveSense), $"Move sense {senseId} between {between.Previous} and {between.Next}"));
        return _api.MoveSense(entryId, senseId, between);
    }

    public Task DeleteSense(Guid entryId, Guid senseId)
    {
        RunRecords.Add(new RunRecord(nameof(DeleteSense), $"Delete sense {senseId}"));
        return _api.DeleteSense(entryId, senseId);
    }

    public Task AddSemanticDomainToSense(Guid senseId, SemanticDomain semanticDomain)
    {
        RunRecords.Add(new RunRecord(nameof(AddSemanticDomainToSense), $"Add semantic domain {semanticDomain.Name}"));
        return _api.AddSemanticDomainToSense(senseId, semanticDomain);
    }

    public Task RemoveSemanticDomainFromSense(Guid senseId, Guid semanticDomainId)
    {
        RunRecords.Add(new RunRecord(nameof(RemoveSemanticDomainFromSense), $"Remove semantic domain {semanticDomainId}"));
        return _api.RemoveSemanticDomainFromSense(senseId, semanticDomainId);
    }

    public Task SetSensePartOfSpeech(Guid senseId, Guid? partOfSpeechId)
    {
        RunRecords.Add(new RunRecord(nameof(SetSensePartOfSpeech), $"Set part of speech {partOfSpeechId}"));
        return _api.SetSensePartOfSpeech(senseId, partOfSpeechId);
    }

    public Task<ExampleSentence> CreateExampleSentence(Guid entryId, Guid senseId, ExampleSentence exampleSentence, BetweenPosition? position = null)
    {
        RunRecords.Add(new RunRecord(nameof(CreateExampleSentence), $"Create example sentence {exampleSentence.Sentence} between {position?.Previous} and {position?.Next}"));
        return _api.CreateExampleSentence(entryId, senseId, exampleSentence, position);
    }

    public Task<ExampleSentence> UpdateExampleSentence(Guid entryId,
        Guid senseId,
        Guid exampleSentenceId,
        UpdateObjectInput<ExampleSentence> update)
    {
        RunRecords.Add(new RunRecord(nameof(UpdateExampleSentence),
            $"Update example sentence {exampleSentenceId}, changes: {update.Summarize()}"));
        return _api.UpdateExampleSentence(entryId, senseId, exampleSentenceId, update);
    }

    public Task<ExampleSentence> UpdateExampleSentence(Guid entryId,
        Guid senseId,
        ExampleSentence before,
        ExampleSentence after,
        IMiniLcmApi? api)
    {
        RunRecords.Add(new RunRecord(nameof(UpdateExampleSentence), $"Update example sentence {after.Id}"));
        return _api.UpdateExampleSentence(entryId, senseId, before, after, api);
    }

    public Task MoveExampleSentence(Guid entryId, Guid senseId, Guid exampleId, BetweenPosition between)
    {
        RunRecords.Add(new RunRecord(nameof(MoveExampleSentence), $"Move example sentence {exampleId} between {between.Previous} and {between.Next}"));
        return _api.MoveExampleSentence(entryId, senseId, exampleId, between);
    }

    public Task DeleteExampleSentence(Guid entryId, Guid senseId, Guid exampleSentenceId)
    {
        RunRecords.Add(new RunRecord(nameof(DeleteExampleSentence), $"Delete example sentence {exampleSentenceId}"));
        return _api.DeleteExampleSentence(entryId, senseId, exampleSentenceId);
    }

    public Task AddTranslation(Guid entryId, Guid senseId, Guid exampleSentenceId, Translation translation)
    {
        RunRecords.Add(new RunRecord(nameof(AddTranslation), $"Add translation {translation.Id} to example sentence {exampleSentenceId}"));
        return _api.AddTranslation(entryId, senseId, exampleSentenceId, translation);
    }

    public Task RemoveTranslation(Guid entryId, Guid senseId, Guid exampleSentenceId, Guid translationId)
    {
        RunRecords.Add(new RunRecord(nameof(RemoveTranslation), $"Remove translation {translationId} from example sentence {exampleSentenceId}"));
        return _api.RemoveTranslation(entryId, senseId, exampleSentenceId, translationId);
    }

    public Task UpdateTranslation(Guid entryId,
        Guid senseId,
        Guid exampleSentenceId,
        Guid translationId,
        UpdateObjectInput<Translation> update)
    {
        RunRecords.Add(new RunRecord(nameof(UpdateTranslation), $"Update translation {translationId} in example sentence {exampleSentenceId}"));
        return _api.UpdateTranslation(entryId, senseId, exampleSentenceId, translationId, update);
    }


    public Task<Picture> CreatePicture(Guid entryId, Guid senseId, Picture picture, BetweenPosition? position = null)
    {
        RunRecords.Add(new RunRecord(nameof(CreatePicture), $"Create picture {picture.Caption} between {position?.Previous} and {position?.Next}"));
        return _api.CreatePicture(entryId, senseId, picture, position);
    }

    public Task<Picture> UpdatePicture(Guid entryId,
        Guid senseId,
        Guid pictureId,
        UpdateObjectInput<Picture> update)
    {
        RunRecords.Add(new RunRecord(nameof(UpdatePicture),
            $"Update picture {pictureId}, changes: {update.Summarize()}"));
        return _api.UpdatePicture(entryId, senseId, pictureId, update);
    }

    public Task<Picture> UpdatePicture(Guid entryId,
        Guid senseId,
        Picture before,
        Picture after,
        IMiniLcmApi? api)
    {
        RunRecords.Add(new RunRecord(nameof(UpdatePicture), $"Update picture {after.Id}"));
        return _api.UpdatePicture(entryId, senseId, before, after, api);
    }

    public Task MovePicture(Guid entryId, Guid senseId, Guid exampleId, BetweenPosition between)
    {
        RunRecords.Add(new RunRecord(nameof(MovePicture), $"Move picture {exampleId} between {between.Previous} and {between.Next}"));
        return _api.MovePicture(entryId, senseId, exampleId, between);
    }

    public Task DeletePicture(Guid entryId, Guid senseId, Guid pictureId)
    {
        RunRecords.Add(new RunRecord(nameof(DeletePicture), $"Delete picture {pictureId}"));
        return _api.DeletePicture(entryId, senseId, pictureId);
    }

    public Task<ComplexFormComponent> CreateComplexFormComponent(ComplexFormComponent complexFormComponent, BetweenPosition<ComplexFormComponent>? between = null)
    {
        var complexFormName = ComplexFormName(complexFormComponent);
        var componentName = ComplexFormComponentName(complexFormComponent);
        var previous = ComplexFormComponentName(between?.Previous);
        var next = ComplexFormComponentName(between?.Next);
        RunRecords.Add(new RunRecord(nameof(CreateComplexFormComponent), $"Create complex form component complex entry: {complexFormName}, component entry: {componentName}, between {previous} and {next}"));
        return _api.CreateComplexFormComponent(complexFormComponent, between);
    }

    public Task MoveComplexFormComponent(ComplexFormComponent complexFormComponent, BetweenPosition<ComplexFormComponent> between)
    {
        var componentName = ComplexFormComponentName(complexFormComponent);
        var previous = ComplexFormComponentName(between.Previous);
        var next = ComplexFormComponentName(between.Next);
        RunRecords.Add(new RunRecord(nameof(MoveComplexFormComponent), $"Move complex form component {componentName} between {previous} and {next}"));
        return _api.MoveComplexFormComponent(complexFormComponent, between);
    }

    public Task DeleteComplexFormComponent(ComplexFormComponent complexFormComponent)
    {
        var componentName = ComplexFormComponentName(complexFormComponent);
        RunRecords.Add(new RunRecord(nameof(DeleteComplexFormComponent), $"Delete complex form component: {componentName}"));
        return _api.DeleteComplexFormComponent(complexFormComponent);
    }

    public Task AddComplexFormType(Guid entryId, Guid complexFormTypeId)
    {
        RunRecords.Add(new RunRecord(nameof(AddComplexFormType), $"Add complex form type {complexFormTypeId}, to entry {entryId}"));
        return _api.AddComplexFormType(entryId, complexFormTypeId);
    }

    public Task<Publication> CreatePublication(Publication pub)
    {
        RunRecords.Add(new RunRecord(nameof(CreatePublication), $"Create publication {pub.Id}"));
        return _api.CreatePublication(pub);
    }

    public Task<Publication> UpdatePublication(Guid id, UpdateObjectInput<Publication> update)
    {
        RunRecords.Add(new RunRecord(nameof(UpdatePublication), $"Update publication {id}"));
        return _api.UpdatePublication(id, update);
    }

    public Task<Publication> UpdatePublication(Publication before, Publication after, IMiniLcmApi? api = null)
    {
        RunRecords.Add(new RunRecord(nameof(UpdatePublication), $"Update publication {before.Id}"));
        return _api.UpdatePublication(before, after, api);
    }

    public Task DeletePublication(Guid id)
    {
        RunRecords.Add(new RunRecord(nameof(DeletePublication), $"Delete publication {id}"));
        return _api.DeletePublication(id);
    }

    public Task AddPublication(Guid entryId, Guid publicationId)
    {
        RunRecords.Add(new RunRecord(nameof(AddPublication), $"Add publication {publicationId} to entry {entryId}"));
        return _api.AddPublication(entryId, publicationId);
    }

    public Task RemovePublication(Guid entryId, Guid publicationId)
    {
        RunRecords.Add(new RunRecord(nameof(RemovePublication), $"Remove publication {publicationId} from entry {entryId}"));
        return _api.RemovePublication(entryId, publicationId);
    }

    #region Submit (sync's result-less write variants)
    // Submit* are writes, so they're implemented here to record and forward. Any not listed falls back to the
    // interface default, which routes to the recording Update*/Move* above.
    public Task SubmitUpdateEntry(Guid id, UpdateObjectInput<Entry> update)
    {
        RunRecords.Add(new RunRecord(nameof(SubmitUpdateEntry), $"Update entry {id}"));
        return _api.SubmitUpdateEntry(id, update);
    }

    public Task SubmitUpdateSense(Guid entryId, Guid senseId, UpdateObjectInput<Sense> update)
    {
        RunRecords.Add(new RunRecord(nameof(SubmitUpdateSense), $"Update sense {senseId}, changes: {update.Summarize()}"));
        return _api.SubmitUpdateSense(entryId, senseId, update);
    }

    public Task SubmitUpdateExampleSentence(Guid entryId, Guid senseId, Guid exampleSentenceId, UpdateObjectInput<ExampleSentence> update)
    {
        RunRecords.Add(new RunRecord(nameof(SubmitUpdateExampleSentence), $"Update example sentence {exampleSentenceId}, changes: {update.Summarize()}"));
        return _api.SubmitUpdateExampleSentence(entryId, senseId, exampleSentenceId, update);
    }

    public Task SubmitCreateSense(Guid entryId, Sense sense, BetweenPosition? position = null)
    {
        RunRecords.Add(new RunRecord(nameof(SubmitCreateSense), $"Create sense {sense.Gloss}"));
        return _api.SubmitCreateSense(entryId, sense, position);
    }

    public Task SubmitCreateExampleSentence(Guid entryId, Guid senseId, ExampleSentence exampleSentence, BetweenPosition? position = null)
    {
        RunRecords.Add(new RunRecord(nameof(SubmitCreateExampleSentence), $"Create example sentence {exampleSentence.Sentence}"));
        return _api.SubmitCreateExampleSentence(entryId, senseId, exampleSentence, position);
    }

    public Task SubmitCreateComplexFormComponent(ComplexFormComponent complexFormComponent, BetweenPosition<ComplexFormComponent>? position = null)
    {
        RunRecords.Add(new RunRecord(nameof(SubmitCreateComplexFormComponent), $"Create complex form component {ComplexFormComponentName(complexFormComponent)}"));
        return _api.SubmitCreateComplexFormComponent(complexFormComponent, position);
    }

    public Task SubmitUpdatePartOfSpeech(Guid id, UpdateObjectInput<PartOfSpeech> update)
    {
        RunRecords.Add(new RunRecord(nameof(SubmitUpdatePartOfSpeech), $"Update part of speech {id}"));
        return _api.SubmitUpdatePartOfSpeech(id, update);
    }

    public Task SubmitUpdatePublication(Guid id, UpdateObjectInput<Publication> update)
    {
        RunRecords.Add(new RunRecord(nameof(SubmitUpdatePublication), $"Update publication {id}"));
        return _api.SubmitUpdatePublication(id, update);
    }

    public Task SubmitUpdateSemanticDomain(Guid id, UpdateObjectInput<SemanticDomain> update)
    {
        RunRecords.Add(new RunRecord(nameof(SubmitUpdateSemanticDomain), $"Update semantic domain {id}"));
        return _api.SubmitUpdateSemanticDomain(id, update);
    }

    public Task SubmitUpdateComplexFormType(Guid id, UpdateObjectInput<ComplexFormType> update)
    {
        RunRecords.Add(new RunRecord(nameof(SubmitUpdateComplexFormType), $"Update complex form type {id}"));
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
