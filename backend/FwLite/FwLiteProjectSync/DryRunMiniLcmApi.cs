using MiniLcm;
using MiniLcm.Exceptions;
using MiniLcm.Models;
using MiniLcm.SyncHelpers;

namespace FwLiteProjectSync;

public partial class DryRunMiniLcmApi(IMiniLcmApi api) : IMiniLcmApi
{
    [BeaKona.AutoInterface(typeof(IMiniLcmReadApi), MemberMatch = BeaKona.MemberMatchTypes.Any)]
    private readonly IMiniLcmApi _api = api;

    public void Dispose()
    {
    }

    public List<DryRunRecord> DryRunRecords { get; } = [];

    public record DryRunRecord(string Method, string Description);

    public Task<WritingSystem> CreateWritingSystem(WritingSystem writingSystem, BetweenPosition<WritingSystemId?>? position = null)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(CreateWritingSystem),
        $"Create writing system {writingSystem.Type} between {position?.Previous} and {position?.Next}"));
        return Task.FromResult(writingSystem);
    }

    public async Task<WritingSystem> UpdateWritingSystem(WritingSystemId id,
        WritingSystemType type,
        UpdateObjectInput<WritingSystem> update)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(UpdateWritingSystem),
            $"Update writing system {type}, changes: {update.Summarize()}"));
        var ws = await _api.GetWritingSystems();
        return (type switch
        {
            WritingSystemType.Vernacular => ws.Vernacular,
            WritingSystemType.Analysis => ws.Analysis,
            _ => throw new InvalidOperationException("unknown type " + type)
        }).First(w => w.WsId == id);
    }

    public Task<WritingSystem> UpdateWritingSystem(WritingSystem before, WritingSystem after, IMiniLcmApi? api)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(UpdateWritingSystem), $"Update {after.Type} writing system {after.WsId}"));
        return Task.FromResult(after);
    }

    public async Task MoveWritingSystem(WritingSystemId id, WritingSystemType type, BetweenPosition<WritingSystemId?> between)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(MoveWritingSystem), $"Move writing system {id} between {between.Previous} and {between.Next}"));
        await Task.CompletedTask;
    }

    public Task<PartOfSpeech> CreatePartOfSpeech(PartOfSpeech partOfSpeech)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(CreatePartOfSpeech), $"Create part of speech {partOfSpeech.Name}"));
        return Task.FromResult(partOfSpeech); // Since this is a dry run, api.GetPartOfSpeech would return null
    }

    public Task<PartOfSpeech> UpdatePartOfSpeech(Guid id, UpdateObjectInput<PartOfSpeech> update)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(UpdatePartOfSpeech), $"Update part of speech {id}"));
        return _api.GetPartOfSpeech(id)!;
    }

    public Task<PartOfSpeech> UpdatePartOfSpeech(PartOfSpeech before, PartOfSpeech after, IMiniLcmApi? api)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(UpdatePartOfSpeech), $"Update part of speech {after.Id}"));
        return Task.FromResult(after);
    }

    public Task DeletePartOfSpeech(Guid id)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(DeletePartOfSpeech), $"Delete part of speech {id}"));
        return Task.CompletedTask;
    }

    public Task<SemanticDomain> CreateSemanticDomain(SemanticDomain semanticDomain)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(CreateSemanticDomain),
            $"Create semantic domain {semanticDomain.Name}"));
        return Task.FromResult(semanticDomain);
    }

    public Task<SemanticDomain> UpdateSemanticDomain(Guid id, UpdateObjectInput<SemanticDomain> update)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(UpdateSemanticDomain), $"Update semantic domain {id}"));
        return _api.GetSemanticDomain(id)!;
    }

    public Task<SemanticDomain> UpdateSemanticDomain(SemanticDomain before, SemanticDomain after, IMiniLcmApi? api)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(UpdateSemanticDomain), $"Update semantic domain {after.Id}"));
        return Task.FromResult(after);
    }

    public Task DeleteSemanticDomain(Guid id)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(DeleteSemanticDomain), $"Delete semantic domain {id}"));
        return Task.CompletedTask;
    }

    public Task<ComplexFormType> CreateComplexFormType(ComplexFormType complexFormType)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(CreateComplexFormType),
            $"Create complex form type {complexFormType.Name}"));
        return Task.FromResult(complexFormType);
    }

    public async Task<ComplexFormType> UpdateComplexFormType(Guid id, UpdateObjectInput<ComplexFormType> update)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(UpdateComplexFormType), $"Update complex form type {id}"));
        return await _api.GetComplexFormType(id) ?? throw new NullReferenceException($"unable to find complex form type with id {id}");
    }

    public Task<ComplexFormType> UpdateComplexFormType(ComplexFormType before, ComplexFormType after, IMiniLcmApi? api)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(UpdateComplexFormType), $"Update complex form type {after.Id}"));
        return Task.FromResult(after);
    }

    public Task DeleteComplexFormType(Guid id)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(DeleteComplexFormType), $"Delete complex form type {id}"));
        return Task.CompletedTask;
    }

    public Task<MorphTypeData> CreateMorphTypeData(MorphTypeData morphType)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(CreateMorphTypeData),
            $"Create morph type {morphType.Name}"));
        return Task.FromResult(morphType);
    }

    public async Task<MorphTypeData> UpdateMorphTypeData(Guid id, UpdateObjectInput<MorphTypeData> update)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(UpdateMorphTypeData), $"Update morph type {id}"));
        return await _api.GetMorphTypeData(id) ?? throw new NullReferenceException($"unable to find morph type with id {id}");
    }

    public Task<MorphTypeData> UpdateMorphTypeData(MorphTypeData before, MorphTypeData after, IMiniLcmApi? api)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(UpdateMorphTypeData), $"Update morph type {after.Id}"));
        return Task.FromResult(after);
    }

    public Task DeleteMorphTypeData(Guid id)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(DeleteMorphTypeData), $"Delete morph type {id}"));
        return Task.CompletedTask;
    }

    public Task<Entry> CreateEntry(Entry entry, CreateEntryOptions? options)
    {
        options ??= new CreateEntryOptions();
        DryRunRecords.Add(new DryRunRecord(nameof(CreateEntry), $"Create entry {entry.Headword()} ({options})"));
        // Only return what would have been persisted
        if (options.IncludeComplexFormsAndComponents)
            return Task.FromResult(entry);
        else
            return Task.FromResult(entry with { Components = [], ComplexForms = [] });
    }

    public Task<Entry> UpdateEntry(Guid id, UpdateObjectInput<Entry> update)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(UpdateEntry), $"Update entry {id}"));
        return _api.GetEntry(id)!;
    }

    public Task<Entry> UpdateEntry(Entry before, Entry after, IMiniLcmApi? api)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(UpdateEntry), $"Update entry {after.Id}"));
        return Task.FromResult(after);
    }

    public Task DeleteEntry(Guid id)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(DeleteEntry), $"Delete entry {id}"));
        return Task.CompletedTask;
    }

    public async Task RemoveComplexFormType(Guid entryId, Guid complexFormTypeId)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(RemoveComplexFormType), $"Remove complex form type {complexFormTypeId}, from entry {entryId}"));
        await Task.CompletedTask;
    }
    public Task<Sense> CreateSense(Guid entryId, Sense sense, BetweenPosition? position = null)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(CreateSense), $"Create sense {sense.Gloss} between {position?.Previous} and {position?.Next}"));
        return Task.FromResult(sense);
    }

    public async Task<Sense> UpdateSense(Guid entryId, Guid senseId, UpdateObjectInput<Sense> update)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(UpdateSense),
            $"Update sense {senseId}, changes: {update.Summarize()}"));
        var entry = await _api.GetEntry(entryId) ??
                    throw new NullReferenceException($"unable to find entry with id {entryId}");
        var sense = entry.Senses.First(s => s.Id == senseId);
        return sense;
    }

    public async Task<Sense> UpdateSense(Guid entryId, Sense before, Sense after, IMiniLcmApi? api)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(UpdateSense),
            $"Update sense {after.Id}"));
        return await _api.GetSense(entryId, after.Id) ?? throw new NullReferenceException($"unable to find sense with id {after.Id}");
    }

    public Task MoveSense(Guid entryId, Guid senseId, BetweenPosition between)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(MoveSense), $"Move sense {senseId} between {between.Previous} and {between.Next}"));
        return Task.CompletedTask;
    }

    public Task DeleteSense(Guid entryId, Guid senseId)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(DeleteSense), $"Delete sense {senseId}"));
        return Task.CompletedTask;
    }

    public Task AddSemanticDomainToSense(Guid senseId, SemanticDomain semanticDomain)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(AddSemanticDomainToSense), $"Add semantic domain {semanticDomain.Name}"));
        return Task.CompletedTask;
    }

    public Task RemoveSemanticDomainFromSense(Guid senseId, Guid semanticDomainId)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(RemoveSemanticDomainFromSense), $"Remove semantic domain {semanticDomainId}"));
        return Task.CompletedTask;
    }

    public Task SetSensePartOfSpeech(Guid senseId, Guid? partOfSpeechId)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(SetSensePartOfSpeech), $"Set part of speech {partOfSpeechId}"));
        return Task.CompletedTask;
    }

    public Task<ExampleSentence> CreateExampleSentence(Guid entryId, Guid senseId, ExampleSentence exampleSentence, BetweenPosition? position = null)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(CreateExampleSentence), $"Create example sentence {exampleSentence.Sentence} between {position?.Previous} and {position?.Next}"));
        return Task.FromResult(exampleSentence);
    }

    public async Task<ExampleSentence> UpdateExampleSentence(Guid entryId,
        Guid senseId,
        Guid exampleSentenceId,
        UpdateObjectInput<ExampleSentence> update)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(UpdateExampleSentence),
            $"Update example sentence {exampleSentenceId}, changes: {update.Summarize()}"));
        var exampleSentence = await _api.GetExampleSentence(entryId, senseId, exampleSentenceId);
        return exampleSentence ?? throw new NullReferenceException($"unable to find example sentence with id {exampleSentenceId}");
    }

    public Task<ExampleSentence> UpdateExampleSentence(Guid entryId,
        Guid senseId,
        ExampleSentence before,
        ExampleSentence after,
        IMiniLcmApi? api)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(UpdateExampleSentence), $"Update example sentence {after.Id}"));
        return Task.FromResult(after);
    }

    public Task MoveExampleSentence(Guid entryId, Guid senseId, Guid exampleId, BetweenPosition between)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(MoveExampleSentence), $"Move example sentence {exampleId} between {between.Previous} and {between.Next}"));
        return Task.CompletedTask;
    }

    public Task DeleteExampleSentence(Guid entryId, Guid senseId, Guid exampleSentenceId)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(DeleteExampleSentence), $"Delete example sentence {exampleSentenceId}"));
        return Task.CompletedTask;
    }

    public Task AddTranslation(Guid entryId, Guid senseId, Guid exampleSentenceId, Translation translation)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(AddTranslation), $"Add translation {translation.Id} to example sentence {exampleSentenceId}"));
        return Task.CompletedTask;
    }

    public Task RemoveTranslation(Guid entryId, Guid senseId, Guid exampleSentenceId, Guid translationId)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(RemoveTranslation), $"Remove translation {translationId} from example sentence {exampleSentenceId}"));
        return Task.CompletedTask;
    }

    public Task UpdateTranslation(Guid entryId,
        Guid senseId,
        Guid exampleSentenceId,
        Guid translationId,
        UpdateObjectInput<Translation> update)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(UpdateTranslation), $"Update translation {translationId} in example sentence {exampleSentenceId}"));
        return Task.CompletedTask;
    }

    public Task<ComplexFormComponent> CreateComplexFormComponent(ComplexFormComponent complexFormComponent, BetweenPosition<ComplexFormComponent>? between = null)
    {
        var complexFormName = ComplexFormName(complexFormComponent);
        var componentName = ComplexFormComponentName(complexFormComponent);
        var previous = ComplexFormComponentName(between?.Previous);
        var next = ComplexFormComponentName(between?.Next);
        DryRunRecords.Add(new DryRunRecord(nameof(CreateComplexFormComponent), $"Create complex form component complex entry: {complexFormName}, component entry: {componentName}, between {previous} and {next}"));
        return Task.FromResult(complexFormComponent);
    }

    public Task MoveComplexFormComponent(ComplexFormComponent complexFormComponent, BetweenPosition<ComplexFormComponent> between)
    {
        var componentName = ComplexFormComponentName(complexFormComponent);
        var previous = ComplexFormComponentName(between.Previous);
        var next = ComplexFormComponentName(between.Next);
        DryRunRecords.Add(new DryRunRecord(nameof(MoveComplexFormComponent), $"Move complex form component {componentName} between {previous} and {next}"));
        return Task.CompletedTask;
    }

    public Task DeleteComplexFormComponent(ComplexFormComponent complexFormComponent)
    {
        var componentName = ComplexFormComponentName(complexFormComponent);
        DryRunRecords.Add(new DryRunRecord(nameof(DeleteComplexFormComponent), $"Delete complex form component: {componentName}"));
        return Task.CompletedTask;
    }

    public async Task AddComplexFormType(Guid entryId, Guid complexFormTypeId)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(AddComplexFormType), $"Add complex form type {complexFormTypeId}, to entry {entryId}"));
        await Task.CompletedTask;
    }

    public async Task<Publication> CreatePublication(Publication pub)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(CreatePublication), $"Create publication {pub.Id}"));
        return await Task.FromResult(pub);
    }

    public async Task<Publication> UpdatePublication(Guid id, UpdateObjectInput<Publication> update)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(UpdatePublication), $"Update publication {id}"));
        return await _api.GetPublication(id) ?? throw NotFoundException.ForType<Publication>(id);
    }

    public async Task<Publication> UpdatePublication(Publication before, Publication after, IMiniLcmApi? api = null)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(UpdatePublication), $"Update publication {before.Id}"));
        return await _api.GetPublication(before.Id) ?? throw NotFoundException.ForType<Publication>(before.Id);
    }

    public Task DeletePublication(Guid id)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(DeletePublication), $"Delete publication {id}"));
        return Task.CompletedTask;
    }

    public Task AddPublication(Guid entryId, Guid publicationId)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(AddPublication), $"Add publication {publicationId} to entry {entryId}"));
        return Task.CompletedTask;
    }

    public Task RemovePublication(Guid entryId, Guid publicationId)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(RemovePublication), $"Remove publication {publicationId} from entry {entryId}"));
        return Task.CompletedTask;
    }

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

    //this is now called to this method from the ResumableImportApi, but calling GetEntries will fail because there's no writing systems
    async IAsyncEnumerable<Entry> IMiniLcmReadApi.GetEntries(QueryOptions? options)
    {
        if (await _api.CountEntries() > 0)
        {
            await foreach (var entry in _api.GetEntries(options))
            {
                yield return entry;
            }
        }
    }

    public Task<CustomView> CreateCustomView(CustomView customView)
    {
        throw new NotImplementedException();
    }

    public Task<CustomView> UpdateCustomView(Guid id, CustomView customView)
    {
        throw new NotImplementedException();
    }

    public Task DeleteCustomView(Guid id)
    {
        throw new NotImplementedException();
    }
}
