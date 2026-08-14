using MiniLcm;
using MiniLcm.Models;
using MiniLcm.SyncHelpers;

namespace FwLiteProjectSync;

/// <summary>
/// Records every write a dry-run sync would make. The records are the run's only output, so each one must
/// identify its object by id: headwords, glosses and names are not unique.
/// </summary>
public partial class RecordingMiniLcmApi(IMiniLcmApi api) : IMiniLcmApi
{

    public record RunRecord(string Method, string Description)
    {
        /// <summary>Kept to one line: LogRecordedRun writes a record per log line, and real project text carries its own breaks.</summary>
        public string Description { get; } = Description.ReplaceLineEndings("\\n");
    }

    private readonly IMiniLcmApi _api = api;

    // Writes aren't auto-forwarded, so the compiler forces each to be recorded
    [BeaKona.AutoInterface]
    private IMiniLcmReadApi ReadApi => _api;

    public List<RunRecord> RunRecords { get; } = [];

    // Wrapped api is the caller's to dispose
    public void Dispose()
    {
    }

    public async Task<WritingSystem> CreateWritingSystem(WritingSystem writingSystem, BetweenPosition<WritingSystemId?>? position = null)
    {
        RunRecords.Add(new RunRecord(nameof(CreateWritingSystem),
            $"Create {writingSystem.Type} writing system {writingSystem.WsId} ({writingSystem.Name}) {Position(position)}"));
        return await _api.CreateWritingSystem(writingSystem, position);
    }

    public async Task<WritingSystem> UpdateWritingSystem(WritingSystemId id,
        WritingSystemType type,
        UpdateObjectInput<WritingSystem> update)
    {
        RunRecords.Add(new RunRecord(nameof(UpdateWritingSystem),
            $"Update {type} writing system {id}, changes: {update.Summarize()}"));
        return await _api.UpdateWritingSystem(id, type, update);
    }

    public async Task<WritingSystem> UpdateWritingSystem(WritingSystem before, WritingSystem after, IMiniLcmApi? api)
    {
        RunRecords.Add(new RunRecord(nameof(UpdateWritingSystem), $"Update {after.Type} writing system {after.WsId}"));
        return await _api.UpdateWritingSystem(before, after, api);
    }

    public async Task MoveWritingSystem(WritingSystemId id, WritingSystemType type, BetweenPosition<WritingSystemId?> between)
    {
        RunRecords.Add(new RunRecord(nameof(MoveWritingSystem), $"Move {type} writing system {id} {Position(between)}"));
        await _api.MoveWritingSystem(id, type, between);
    }

    public async Task<PartOfSpeech> CreatePartOfSpeech(PartOfSpeech partOfSpeech)
    {
        RunRecords.Add(new RunRecord(nameof(CreatePartOfSpeech), $"Create part of speech {partOfSpeech.Name} ({partOfSpeech.Id})"));
        return await _api.CreatePartOfSpeech(partOfSpeech);
    }

    public async Task<PartOfSpeech> UpdatePartOfSpeech(Guid id, UpdateObjectInput<PartOfSpeech> update)
    {
        RunRecords.Add(new RunRecord(nameof(UpdatePartOfSpeech), $"Update part of speech {id}, changes: {update.Summarize()}"));
        return await _api.UpdatePartOfSpeech(id, update);
    }

    public async Task<PartOfSpeech> UpdatePartOfSpeech(PartOfSpeech before, PartOfSpeech after, IMiniLcmApi? api)
    {
        RunRecords.Add(new RunRecord(nameof(UpdatePartOfSpeech), $"Update part of speech {after.Id}"));
        return await _api.UpdatePartOfSpeech(before, after, api);
    }

    public async Task DeletePartOfSpeech(Guid id)
    {
        RunRecords.Add(new RunRecord(nameof(DeletePartOfSpeech), $"Delete part of speech {id}"));
        await _api.DeletePartOfSpeech(id);
    }

    public async Task<SemanticDomain> CreateSemanticDomain(SemanticDomain semanticDomain)
    {
        RunRecords.Add(new RunRecord(nameof(CreateSemanticDomain),
            $"Create semantic domain {semanticDomain.Code} {semanticDomain.Name} ({semanticDomain.Id})"));
        return await _api.CreateSemanticDomain(semanticDomain);
    }

    public async Task<SemanticDomain> UpdateSemanticDomain(Guid id, UpdateObjectInput<SemanticDomain> update)
    {
        RunRecords.Add(new RunRecord(nameof(UpdateSemanticDomain), $"Update semantic domain {id}, changes: {update.Summarize()}"));
        return await _api.UpdateSemanticDomain(id, update);
    }

    public async Task<SemanticDomain> UpdateSemanticDomain(SemanticDomain before, SemanticDomain after, IMiniLcmApi? api)
    {
        RunRecords.Add(new RunRecord(nameof(UpdateSemanticDomain), $"Update semantic domain {after.Id}"));
        return await _api.UpdateSemanticDomain(before, after, api);
    }

    public async Task DeleteSemanticDomain(Guid id)
    {
        RunRecords.Add(new RunRecord(nameof(DeleteSemanticDomain), $"Delete semantic domain {id}"));
        await _api.DeleteSemanticDomain(id);
    }

    public async Task<ComplexFormType> CreateComplexFormType(ComplexFormType complexFormType)
    {
        RunRecords.Add(new RunRecord(nameof(CreateComplexFormType),
            $"Create complex form type {complexFormType.Name} ({complexFormType.Id})"));
        return await _api.CreateComplexFormType(complexFormType);
    }

    public async Task<ComplexFormType> UpdateComplexFormType(Guid id, UpdateObjectInput<ComplexFormType> update)
    {
        RunRecords.Add(new RunRecord(nameof(UpdateComplexFormType), $"Update complex form type {id}, changes: {update.Summarize()}"));
        return await _api.UpdateComplexFormType(id, update);
    }

    public async Task<ComplexFormType> UpdateComplexFormType(ComplexFormType before, ComplexFormType after, IMiniLcmApi? api)
    {
        RunRecords.Add(new RunRecord(nameof(UpdateComplexFormType), $"Update complex form type {after.Id}"));
        return await _api.UpdateComplexFormType(before, after, api);
    }

    public async Task DeleteComplexFormType(Guid id)
    {
        RunRecords.Add(new RunRecord(nameof(DeleteComplexFormType), $"Delete complex form type {id}"));
        await _api.DeleteComplexFormType(id);
    }

    public async Task<MorphType> CreateMorphType(MorphType morphType)
    {
        RunRecords.Add(new RunRecord(nameof(CreateMorphType), $"Create morph type {morphType.Kind} ({morphType.Id})"));
        return await _api.CreateMorphType(morphType);
    }

    public async Task<MorphType> UpdateMorphType(Guid id, UpdateObjectInput<MorphType> update)
    {
        RunRecords.Add(new RunRecord(nameof(UpdateMorphType), $"Update morph type {id}, changes: {update.Summarize()}"));
        return await _api.UpdateMorphType(id, update);
    }

    public async Task<MorphType> UpdateMorphType(MorphType before, MorphType after, IMiniLcmApi? api)
    {
        RunRecords.Add(new RunRecord(nameof(UpdateMorphType), $"Update morph type {after.Id}"));
        return await _api.UpdateMorphType(before, after, api);
    }

    public async Task<Entry> CreateEntry(Entry entry, CreateEntryOptions? options = null)
    {
        RunRecords.Add(new RunRecord(nameof(CreateEntry),
            $"Create entry {entry.Headword()} ({entry.Id}), options: {options ?? CreateEntryOptions.WithMainPublication}"));
        return await _api.CreateEntry(entry, options);
    }

    public async Task<Entry> UpdateEntry(Guid id, UpdateObjectInput<Entry> update)
    {
        RunRecords.Add(new RunRecord(nameof(UpdateEntry), $"Update entry {id}, changes: {update.Summarize()}"));
        return await _api.UpdateEntry(id, update);
    }

    public async Task<Entry> UpdateEntry(Entry before, Entry after, IMiniLcmApi? api)
    {
        RunRecords.Add(new RunRecord(nameof(UpdateEntry), $"Update entry {after.Headword()} ({after.Id})"));
        return await _api.UpdateEntry(before, after, api);
    }

    public async Task DeleteEntry(Guid id)
    {
        RunRecords.Add(new RunRecord(nameof(DeleteEntry), $"Delete entry {id}"));
        await _api.DeleteEntry(id);
    }

    public async Task RemoveComplexFormType(Guid entryId, Guid complexFormTypeId)
    {
        RunRecords.Add(new RunRecord(nameof(RemoveComplexFormType), $"Remove complex form type {complexFormTypeId} from entry {entryId}"));
        await _api.RemoveComplexFormType(entryId, complexFormTypeId);
    }

    public async Task<Sense> CreateSense(Guid entryId, Sense sense, BetweenPosition? position = null)
    {
        RunRecords.Add(new RunRecord(nameof(CreateSense),
            $"Create sense {sense.Gloss} ({sense.Id}) in entry {entryId} {Position(position)}"));
        return await _api.CreateSense(entryId, sense, position);
    }

    public async Task<Sense> UpdateSense(Guid entryId, Guid senseId, UpdateObjectInput<Sense> update)
    {
        RunRecords.Add(new RunRecord(nameof(UpdateSense),
            $"Update sense {senseId} in entry {entryId}, changes: {update.Summarize()}"));
        return await _api.UpdateSense(entryId, senseId, update);
    }

    public async Task<Sense> UpdateSense(Guid entryId, Sense before, Sense after, IMiniLcmApi? api)
    {
        RunRecords.Add(new RunRecord(nameof(UpdateSense),
            $"Update sense {after.Gloss} ({after.Id}) in entry {entryId}"));
        return await _api.UpdateSense(entryId, before, after, api);
    }

    public async Task MoveSense(Guid entryId, Guid senseId, BetweenPosition between)
    {
        RunRecords.Add(new RunRecord(nameof(MoveSense), $"Move sense {senseId} in entry {entryId} {Position(between)}"));
        await _api.MoveSense(entryId, senseId, between);
    }

    public async Task DeleteSense(Guid entryId, Guid senseId)
    {
        RunRecords.Add(new RunRecord(nameof(DeleteSense), $"Delete sense {senseId} in entry {entryId}"));
        await _api.DeleteSense(entryId, senseId);
    }

    public async Task AddSemanticDomainToSense(Guid senseId, SemanticDomain semanticDomain)
    {
        RunRecords.Add(new RunRecord(nameof(AddSemanticDomainToSense),
            $"Add semantic domain {semanticDomain.Code} {semanticDomain.Name} ({semanticDomain.Id}) to sense {senseId}"));
        await _api.AddSemanticDomainToSense(senseId, semanticDomain);
    }

    public async Task RemoveSemanticDomainFromSense(Guid senseId, Guid semanticDomainId)
    {
        RunRecords.Add(new RunRecord(nameof(RemoveSemanticDomainFromSense),
            $"Remove semantic domain {semanticDomainId} from sense {senseId}"));
        await _api.RemoveSemanticDomainFromSense(senseId, semanticDomainId);
    }

    public async Task SetSensePartOfSpeech(Guid senseId, Guid? partOfSpeechId)
    {
        RunRecords.Add(new RunRecord(nameof(SetSensePartOfSpeech),
            $"Set part of speech {OrNull(partOfSpeechId)} on sense {senseId}"));
        await _api.SetSensePartOfSpeech(senseId, partOfSpeechId);
    }

    public async Task<ExampleSentence> CreateExampleSentence(Guid entryId, Guid senseId, ExampleSentence exampleSentence, BetweenPosition? position = null)
    {
        RunRecords.Add(new RunRecord(nameof(CreateExampleSentence),
            $"Create example sentence {exampleSentence.Sentence} ({exampleSentence.Id}) in sense {senseId} {Position(position)}"));
        return await _api.CreateExampleSentence(entryId, senseId, exampleSentence, position);
    }

    public async Task<ExampleSentence> UpdateExampleSentence(Guid entryId,
        Guid senseId,
        Guid exampleSentenceId,
        UpdateObjectInput<ExampleSentence> update)
    {
        RunRecords.Add(new RunRecord(nameof(UpdateExampleSentence),
            $"Update example sentence {exampleSentenceId} in sense {senseId}, changes: {update.Summarize()}"));
        return await _api.UpdateExampleSentence(entryId, senseId, exampleSentenceId, update);
    }

    public async Task<ExampleSentence> UpdateExampleSentence(Guid entryId,
        Guid senseId,
        ExampleSentence before,
        ExampleSentence after,
        IMiniLcmApi? api)
    {
        RunRecords.Add(new RunRecord(nameof(UpdateExampleSentence),
            $"Update example sentence {after.Sentence} ({after.Id}) in sense {senseId}"));
        return await _api.UpdateExampleSentence(entryId, senseId, before, after, api);
    }

    public async Task MoveExampleSentence(Guid entryId, Guid senseId, Guid exampleId, BetweenPosition between)
    {
        RunRecords.Add(new RunRecord(nameof(MoveExampleSentence),
            $"Move example sentence {exampleId} in sense {senseId} {Position(between)}"));
        await _api.MoveExampleSentence(entryId, senseId, exampleId, between);
    }

    public async Task DeleteExampleSentence(Guid entryId, Guid senseId, Guid exampleSentenceId)
    {
        RunRecords.Add(new RunRecord(nameof(DeleteExampleSentence),
            $"Delete example sentence {exampleSentenceId} in sense {senseId}"));
        await _api.DeleteExampleSentence(entryId, senseId, exampleSentenceId);
    }

    public async Task AddTranslation(Guid entryId, Guid senseId, Guid exampleSentenceId, Translation translation)
    {
        RunRecords.Add(new RunRecord(nameof(AddTranslation),
            $"Add translation {translation.Text} ({translation.Id}) to example sentence {exampleSentenceId}"));
        await _api.AddTranslation(entryId, senseId, exampleSentenceId, translation);
    }

    public async Task RemoveTranslation(Guid entryId, Guid senseId, Guid exampleSentenceId, Guid translationId)
    {
        RunRecords.Add(new RunRecord(nameof(RemoveTranslation),
            $"Remove translation {translationId} from example sentence {exampleSentenceId}"));
        await _api.RemoveTranslation(entryId, senseId, exampleSentenceId, translationId);
    }

    public async Task UpdateTranslation(Guid entryId,
        Guid senseId,
        Guid exampleSentenceId,
        Guid translationId,
        UpdateObjectInput<Translation> update)
    {
        RunRecords.Add(new RunRecord(nameof(UpdateTranslation),
            $"Update translation {translationId} in example sentence {exampleSentenceId}, changes: {update.Summarize()}"));
        await _api.UpdateTranslation(entryId, senseId, exampleSentenceId, translationId, update);
    }


    public async Task<Picture> CreatePicture(Guid entryId, Guid senseId, Picture picture, BetweenPosition? position = null)
    {
        RunRecords.Add(new RunRecord(nameof(CreatePicture),
            $"Create picture {picture.Caption} ({picture.Id}) in sense {senseId} {Position(position)}"));
        return await _api.CreatePicture(entryId, senseId, picture, position);
    }

    public async Task<Picture> UpdatePicture(Guid entryId,
        Guid senseId,
        Guid pictureId,
        UpdateObjectInput<Picture> update)
    {
        RunRecords.Add(new RunRecord(nameof(UpdatePicture),
            $"Update picture {pictureId} in sense {senseId}, changes: {update.Summarize()}"));
        return await _api.UpdatePicture(entryId, senseId, pictureId, update);
    }

    public async Task<Picture> UpdatePicture(Guid entryId,
        Guid senseId,
        Picture before,
        Picture after,
        IMiniLcmApi? api)
    {
        RunRecords.Add(new RunRecord(nameof(UpdatePicture), $"Update picture {after.Caption} ({after.Id}) in sense {senseId}"));
        return await _api.UpdatePicture(entryId, senseId, before, after, api);
    }

    public async Task MovePicture(Guid entryId, Guid senseId, Guid pictureId, BetweenPosition between)
    {
        RunRecords.Add(new RunRecord(nameof(MovePicture), $"Move picture {pictureId} in sense {senseId} {Position(between)}"));
        await _api.MovePicture(entryId, senseId, pictureId, between);
    }

    public async Task DeletePicture(Guid entryId, Guid senseId, Guid pictureId)
    {
        RunRecords.Add(new RunRecord(nameof(DeletePicture), $"Delete picture {pictureId} in sense {senseId}"));
        await _api.DeletePicture(entryId, senseId, pictureId);
    }

    public async Task<ComplexFormComponent> CreateComplexFormComponent(ComplexFormComponent complexFormComponent, BetweenPosition<ComplexFormComponent>? between = null)
    {
        RunRecords.Add(new RunRecord(nameof(CreateComplexFormComponent),
            $"Create complex form component {ComplexFormLink(complexFormComponent)} {Position(between)}"));
        return await _api.CreateComplexFormComponent(complexFormComponent, between);
    }

    public async Task MoveComplexFormComponent(ComplexFormComponent complexFormComponent, BetweenPosition<ComplexFormComponent> between)
    {
        RunRecords.Add(new RunRecord(nameof(MoveComplexFormComponent),
            $"Move complex form component {ComplexFormLink(complexFormComponent)} {Position(between)}"));
        await _api.MoveComplexFormComponent(complexFormComponent, between);
    }

    public async Task DeleteComplexFormComponent(ComplexFormComponent complexFormComponent)
    {
        RunRecords.Add(new RunRecord(nameof(DeleteComplexFormComponent),
            $"Delete complex form component {ComplexFormLink(complexFormComponent)}"));
        await _api.DeleteComplexFormComponent(complexFormComponent);
    }

    public async Task AddComplexFormType(Guid entryId, Guid complexFormTypeId)
    {
        RunRecords.Add(new RunRecord(nameof(AddComplexFormType), $"Add complex form type {complexFormTypeId} to entry {entryId}"));
        await _api.AddComplexFormType(entryId, complexFormTypeId);
    }

    public async Task<Publication> CreatePublication(Publication pub)
    {
        RunRecords.Add(new RunRecord(nameof(CreatePublication), $"Create publication {pub.Name} ({pub.Id})"));
        return await _api.CreatePublication(pub);
    }

    public async Task<Publication> UpdatePublication(Guid id, UpdateObjectInput<Publication> update)
    {
        RunRecords.Add(new RunRecord(nameof(UpdatePublication), $"Update publication {id}, changes: {update.Summarize()}"));
        return await _api.UpdatePublication(id, update);
    }

    public async Task<Publication> UpdatePublication(Publication before, Publication after, IMiniLcmApi? api = null)
    {
        RunRecords.Add(new RunRecord(nameof(UpdatePublication), $"Update publication {after.Id}"));
        return await _api.UpdatePublication(before, after, api);
    }

    public async Task DeletePublication(Guid id)
    {
        RunRecords.Add(new RunRecord(nameof(DeletePublication), $"Delete publication {id}"));
        await _api.DeletePublication(id);
    }

    public async Task AddPublication(Guid entryId, Guid publicationId)
    {
        RunRecords.Add(new RunRecord(nameof(AddPublication), $"Add publication {publicationId} to entry {entryId}"));
        await _api.AddPublication(entryId, publicationId);
    }

    public async Task RemovePublication(Guid entryId, Guid publicationId)
    {
        RunRecords.Add(new RunRecord(nameof(RemovePublication), $"Remove publication {publicationId} from entry {entryId}"));
        await _api.RemovePublication(entryId, publicationId);
    }

    #region Submit (sync's result-less write variants)
    public async Task SubmitUpdateEntry(Guid id, UpdateObjectInput<Entry> update)
    {
        RunRecords.Add(new RunRecord(nameof(SubmitUpdateEntry), $"Update entry {id}, changes: {update.Summarize()}"));
        await _api.SubmitUpdateEntry(id, update);
    }

    public async Task SubmitUpdateSense(Guid entryId, Guid senseId, UpdateObjectInput<Sense> update)
    {
        RunRecords.Add(new RunRecord(nameof(SubmitUpdateSense),
            $"Update sense {senseId} in entry {entryId}, changes: {update.Summarize()}"));
        await _api.SubmitUpdateSense(entryId, senseId, update);
    }

    public async Task SubmitUpdateExampleSentence(Guid entryId, Guid senseId, Guid exampleSentenceId, UpdateObjectInput<ExampleSentence> update)
    {
        RunRecords.Add(new RunRecord(nameof(SubmitUpdateExampleSentence),
            $"Update example sentence {exampleSentenceId} in sense {senseId}, changes: {update.Summarize()}"));
        await _api.SubmitUpdateExampleSentence(entryId, senseId, exampleSentenceId, update);
    }

    public async Task SubmitUpdatePicture(Guid entryId, Guid senseId, Guid pictureId, UpdateObjectInput<Picture> update)
    {
        RunRecords.Add(new RunRecord(nameof(SubmitUpdatePicture),
            $"Update picture {pictureId} in sense {senseId}, changes: {update.Summarize()}"));
        await _api.SubmitUpdatePicture(entryId, senseId, pictureId, update);
    }

    public async Task SubmitCreateSense(Guid entryId, Sense sense, BetweenPosition? position = null)
    {
        RunRecords.Add(new RunRecord(nameof(SubmitCreateSense),
            $"Create sense {sense.Gloss} ({sense.Id}) in entry {entryId} {Position(position)}"));
        await _api.SubmitCreateSense(entryId, sense, position);
    }

    public async Task SubmitCreateExampleSentence(Guid entryId, Guid senseId, ExampleSentence exampleSentence, BetweenPosition? position = null)
    {
        RunRecords.Add(new RunRecord(nameof(SubmitCreateExampleSentence),
            $"Create example sentence {exampleSentence.Sentence} ({exampleSentence.Id}) in sense {senseId} {Position(position)}"));
        await _api.SubmitCreateExampleSentence(entryId, senseId, exampleSentence, position);
    }

    public async Task SubmitCreateComplexFormComponent(ComplexFormComponent complexFormComponent, BetweenPosition<ComplexFormComponent>? position = null)
    {
        RunRecords.Add(new RunRecord(nameof(SubmitCreateComplexFormComponent),
            $"Create complex form component {ComplexFormLink(complexFormComponent)} {Position(position)}"));
        await _api.SubmitCreateComplexFormComponent(complexFormComponent, position);
    }

    public async Task SubmitMoveComplexFormComponent(ComplexFormComponent complexFormComponent, BetweenPosition<ComplexFormComponent> between)
    {
        RunRecords.Add(new RunRecord(nameof(SubmitMoveComplexFormComponent),
            $"Move complex form component {ComplexFormLink(complexFormComponent)} {Position(between)}"));
        await _api.SubmitMoveComplexFormComponent(complexFormComponent, between);
    }

    public async Task SubmitUpdatePartOfSpeech(Guid id, UpdateObjectInput<PartOfSpeech> update)
    {
        RunRecords.Add(new RunRecord(nameof(SubmitUpdatePartOfSpeech), $"Update part of speech {id}, changes: {update.Summarize()}"));
        await _api.SubmitUpdatePartOfSpeech(id, update);
    }

    public async Task SubmitUpdatePublication(Guid id, UpdateObjectInput<Publication> update)
    {
        RunRecords.Add(new RunRecord(nameof(SubmitUpdatePublication), $"Update publication {id}, changes: {update.Summarize()}"));
        await _api.SubmitUpdatePublication(id, update);
    }

    public async Task SubmitUpdateSemanticDomain(Guid id, UpdateObjectInput<SemanticDomain> update)
    {
        RunRecords.Add(new RunRecord(nameof(SubmitUpdateSemanticDomain), $"Update semantic domain {id}, changes: {update.Summarize()}"));
        await _api.SubmitUpdateSemanticDomain(id, update);
    }

    public async Task SubmitUpdateComplexFormType(Guid id, UpdateObjectInput<ComplexFormType> update)
    {
        RunRecords.Add(new RunRecord(nameof(SubmitUpdateComplexFormType), $"Update complex form type {id}, changes: {update.Summarize()}"));
        await _api.SubmitUpdateComplexFormType(id, update);
    }
    #endregion

    // One component is joined to several complex forms, so it takes both entries to identify a link
    // (fwdata components carry no link id).
    private static string ComplexFormLink(ComplexFormComponent component)
    {
        return $"complex form {ComplexFormName(component)}, component {ComplexFormComponentName(component)} (link {OrNull(component.MaybeId)})";
    }

    private static string ComplexFormComponentName(ComplexFormComponent? component)
    {
        if (component == null) return "null";
        return $"{component.ComponentHeadword} (entry {component.ComponentEntryId}, sense {OrNull(component.ComponentSenseId)})";
    }

    private static string ComplexFormName(ComplexFormComponent? component)
    {
        if (component == null) return "null";
        return $"{component.ComplexFormHeadword} ({component.ComplexFormEntryId})";
    }

    // No position and neighbourless are the same append to OrderPicker, so they read the same here.
    private static string Position<T>(BetweenPosition<T>? position)
    {
        if (position is null or { Previous: null, Next: null }) return "at the end";
        return $"between {OrNull(position.Previous)} and {OrNull(position.Next)}";
    }

    private static string Position(BetweenPosition<ComplexFormComponent>? position)
    {
        if (position is null or { Previous: null, Next: null }) return "at the end";
        return $"between {ComplexFormComponentName(position.Previous)} and {ComplexFormComponentName(position.Next)}";
    }

    private static string OrNull<T>(T? value)
    {
        return value is null ? "null" : value.ToString() ?? "null";
    }
}
