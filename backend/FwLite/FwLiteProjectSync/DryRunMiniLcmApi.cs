using MiniLcm;
using MiniLcm.Models;
using MiniLcm.SyncHelpers;

namespace FwLiteProjectSync;

public class DryRunMiniLcmApi(IMiniLcmApi api) : IMiniLcmApi
{
    public List<DryRunRecord> DryRunRecords { get; } = [];

    public record DryRunRecord(string Method, string Description);

    public Task<WritingSystems> GetWritingSystems()
    {
        return api.GetWritingSystems();
    }

    public Task<WritingSystem> CreateWritingSystem(WritingSystemType type, WritingSystem writingSystem)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(CreateWritingSystem), $"Create writing system {type}"));
        return Task.FromResult(writingSystem);
    }

    public async Task<WritingSystem> UpdateWritingSystem(WritingSystemId id,
        WritingSystemType type,
        UpdateObjectInput<WritingSystem> update)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(UpdateWritingSystem),
            $"Update writing system {type}, changes: {update.Summarize()}"));
        var ws = await api.GetWritingSystems();
        return (type switch
        {
            WritingSystemType.Vernacular => ws.Vernacular,
            WritingSystemType.Analysis => ws.Analysis
        }).First(w => w.WsId == id);
    }

    public Task<WritingSystem> UpdateWritingSystem(WritingSystem before, WritingSystem after)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(UpdateEntry), $"Update {after.Type} writing system {after.WsId}"));
        return Task.FromResult(after);
    }

    public IAsyncEnumerable<PartOfSpeech> GetPartsOfSpeech()
    {
        return api.GetPartsOfSpeech();
    }

    public Task<PartOfSpeech?> GetPartOfSpeech(Guid id)
    {
        return api.GetPartOfSpeech(id);
    }

    public Task<PartOfSpeech> CreatePartOfSpeech(PartOfSpeech partOfSpeech)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(CreatePartOfSpeech), $"Create part of speech {partOfSpeech.Name}"));
        return Task.FromResult(partOfSpeech); // Since this is a dry run, api.GetPartOfSpeech would return null
    }

    public Task<PartOfSpeech> UpdatePartOfSpeech(Guid id, UpdateObjectInput<PartOfSpeech> update)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(UpdatePartOfSpeech), $"Update part of speech {id}"));
        return GetPartOfSpeech(id)!;
    }

    public Task<PartOfSpeech> UpdatePartOfSpeech(PartOfSpeech before, PartOfSpeech after)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(UpdatePartOfSpeech), $"Update part of speech {after.Id}"));
        return Task.FromResult(after);
    }

    public Task DeletePartOfSpeech(Guid id)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(DeletePartOfSpeech), $"Delete part of speech {id}"));
        return Task.CompletedTask;
    }

    public IAsyncEnumerable<SemanticDomain> GetSemanticDomains()
    {
        return api.GetSemanticDomains();
    }

    public Task<SemanticDomain?> GetSemanticDomain(Guid id)
    {
        return api.GetSemanticDomain(id);
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
        return GetSemanticDomain(id)!;
    }

    public Task<SemanticDomain> UpdateSemanticDomain(SemanticDomain before, SemanticDomain after)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(UpdateSemanticDomain), $"Update semantic domain {after.Id}"));
        return Task.FromResult(after);
    }

    public Task DeleteSemanticDomain(Guid id)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(DeleteSemanticDomain), $"Delete semantic domain {id}"));
        return Task.CompletedTask;
    }

    public IAsyncEnumerable<ComplexFormType> GetComplexFormTypes()
    {
        return api.GetComplexFormTypes();
    }

    public Task<ComplexFormType?> GetComplexFormType(Guid id)
    {
        return api.GetComplexFormType(id);
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
        return await GetComplexFormType(id) ?? throw new NullReferenceException($"unable to find complex form type with id {id}");
    }

    public Task<ComplexFormType> UpdateComplexFormType(ComplexFormType before, ComplexFormType after)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(UpdateComplexFormType), $"Update complex form type {after.Id}"));
        return Task.FromResult(after);
    }

    public Task DeleteComplexFormType(Guid id)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(DeleteComplexFormType), $"Delete complex form type {id}"));
        return Task.CompletedTask;
    }

    public IAsyncEnumerable<Entry> GetEntries(QueryOptions? options = null)
    {
        return api.GetEntries(options);
    }

    public IAsyncEnumerable<Entry> SearchEntries(string query, QueryOptions? options = null)
    {
        return api.SearchEntries(query, options);
    }

    public Task<Entry?> GetEntry(Guid id)
    {
        return api.GetEntry(id);
    }

    public Task<Entry> CreateEntry(Entry entry)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(CreateEntry), $"Create entry {entry.Headword()}"));
        return Task.FromResult(entry);
    }

    public Task<Entry> UpdateEntry(Guid id, UpdateObjectInput<Entry> update)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(UpdateEntry), $"Update entry {id}"));
        return GetEntry(id)!;
    }

    public Task<Entry> UpdateEntry(Entry before, Entry after)
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

    public Task<Sense?> GetSense(Guid entryId, Guid id)
    {
        return api.GetSense(entryId, id);
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
        var entry = await GetEntry(entryId) ??
                    throw new NullReferenceException($"unable to find entry with id {entryId}");
        var sense = entry.Senses.First(s => s.Id == senseId);
        return sense;
    }

    public async Task<Sense> UpdateSense(Guid entryId, Sense before, Sense after)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(UpdateSense),
            $"Update sense {after.Id}"));
        return await GetSense(entryId, after.Id) ?? throw new NullReferenceException($"unable to find sense with id {after.Id}");
    }

    public Task<Sense> MoveSense(Guid entryId, Sense sense, BetweenPosition between)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(MoveSense), $"Move sense {sense.Gloss} between {between.Previous} and {between.Next}"));
        return Task.FromResult(sense);
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

    public Task<ExampleSentence?> GetExampleSentence(Guid entryId, Guid senseId, Guid id)
    {
        return api.GetExampleSentence(entryId, senseId, id);
    }

    public Task<ExampleSentence> CreateExampleSentence(Guid entryId, Guid senseId, ExampleSentence exampleSentence)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(CreateExampleSentence), $"Create example sentence {exampleSentence.Sentence}"));
        return Task.FromResult(exampleSentence);
    }

    public async Task<ExampleSentence> UpdateExampleSentence(Guid entryId,
        Guid senseId,
        Guid exampleSentenceId,
        UpdateObjectInput<ExampleSentence> update)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(UpdateExampleSentence),
            $"Update example sentence {exampleSentenceId}, changes: {update.Summarize()}"));
        var exampleSentence = await GetExampleSentence(entryId, senseId, exampleSentenceId);
        return exampleSentence ?? throw new NullReferenceException($"unable to find example sentence with id {exampleSentenceId}");
    }

    public Task<ExampleSentence> UpdateExampleSentence(Guid entryId,
        Guid senseId,
        ExampleSentence before,
        ExampleSentence after)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(UpdateExampleSentence), $"Update example sentence {after.Id}"));
        return Task.FromResult(after);
    }

    public Task DeleteExampleSentence(Guid entryId, Guid senseId, Guid exampleSentenceId)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(DeleteExampleSentence), $"Delete example sentence {exampleSentenceId}"));
        return Task.CompletedTask;
    }

    public Task<ComplexFormComponent> CreateComplexFormComponent(ComplexFormComponent complexFormComponent)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(CreateComplexFormComponent), $"Create complex form component complex entry: {complexFormComponent.ComplexFormHeadword}, component entry: {complexFormComponent.ComponentHeadword}"));
        return Task.FromResult(complexFormComponent);
    }

    public Task DeleteComplexFormComponent(ComplexFormComponent complexFormComponent)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(DeleteComplexFormComponent), $"Delete complex form component: {complexFormComponent}"));
        return Task.CompletedTask;
    }

    public async Task AddComplexFormType(Guid entryId, Guid complexFormTypeId)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(AddComplexFormType), $"Add complex form type {complexFormTypeId}, to entry {entryId}"));
        await Task.CompletedTask;
    }
}
