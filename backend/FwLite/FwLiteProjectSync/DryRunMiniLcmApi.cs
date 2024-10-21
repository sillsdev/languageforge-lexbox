using MiniLcm;
using MiniLcm.Models;

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

    public IAsyncEnumerable<PartOfSpeech> GetPartsOfSpeech()
    {
        return api.GetPartsOfSpeech();
    }

    public Task CreatePartOfSpeech(PartOfSpeech partOfSpeech)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(CreatePartOfSpeech), $"Create part of speech {partOfSpeech.Name}"));
        return Task.CompletedTask;
    }

    public IAsyncEnumerable<SemanticDomain> GetSemanticDomains()
    {
        return api.GetSemanticDomains();
    }

    public Task CreateSemanticDomain(SemanticDomain semanticDomain)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(CreateSemanticDomain),
            $"Create semantic domain {semanticDomain.Name}"));
        return Task.CompletedTask;
    }

    public IAsyncEnumerable<ComplexFormType> GetComplexFormTypes()
    {
        return api.GetComplexFormTypes();
    }

    public Task<ComplexFormType> CreateComplexFormType(ComplexFormType complexFormType)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(CreateComplexFormType),
            $"Create complex form type {complexFormType.Name}"));
        return Task.FromResult(complexFormType);
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

    public Task DeleteEntry(Guid id)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(DeleteEntry), $"Delete entry {id}"));
        return Task.CompletedTask;
    }

    public Task<Sense> CreateSense(Guid entryId, Sense sense)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(CreateSense), $"Create sense {sense.Gloss}"));
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
        var entry = await GetEntry(entryId) ??
                    throw new NullReferenceException($"unable to find entry with id {entryId}");
        var sense = entry.Senses.First(s => s.Id == senseId);
        var exampleSentence = sense.ExampleSentences.First(s => s.Id == exampleSentenceId);
        return exampleSentence;
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
        DryRunRecords.Add(new DryRunRecord(nameof(DeleteComplexFormComponent), $"Delete complex form component complex entry: {complexFormComponent.ComplexFormHeadword}, component entry: {complexFormComponent.ComponentHeadword}"));
        return Task.CompletedTask;
    }

    public Task ReplaceComplexFormComponent(ComplexFormComponent old, ComplexFormComponent @new)
    {
        DryRunRecords.Add(new DryRunRecord(nameof(ReplaceComplexFormComponent), $"Replace complex form component complex entry: {old.ComplexFormHeadword}, component entry: {old.ComponentHeadword} with complex entry: {@new.ComplexFormHeadword}, component entry: {@new.ComponentHeadword}"));
        return Task.CompletedTask;
    }
}
