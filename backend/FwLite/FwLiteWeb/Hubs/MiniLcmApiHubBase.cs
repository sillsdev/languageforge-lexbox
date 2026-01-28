using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using MiniLcm;
using MiniLcm.Models;
using MiniLcm.Normalization;
using MiniLcm.Validators;
using SystemTextJsonPatch;

namespace FwLiteWeb.Hubs;

public abstract class MiniLcmApiHubBase : Hub<ILexboxHubClient>
{
    private readonly IMiniLcmApi _miniLcmApi;

    protected MiniLcmApiHubBase(
        IMiniLcmApi miniLcmApi,
        MiniLcmApiValidationWrapperFactory validationWrapperFactory,
        MiniLcmWriteApiNormalizationWrapperFactory writeNormalizationWrapperFactory)
    {
        _miniLcmApi = writeNormalizationWrapperFactory.Create(validationWrapperFactory.Create(miniLcmApi));
    }

    protected MiniLcmApiHubBase(
        IMiniLcmApi miniLcmApi,
        MiniLcmApiValidationWrapperFactory validationWrapperFactory,
        bool skipWriteNormalization)
    {
        if (skipWriteNormalization)
        {
            // FwData APIs already normalize strings internally via LCModel
            _miniLcmApi = validationWrapperFactory.Create(miniLcmApi);
        }
        else
        {
            throw new ArgumentException("Use the other constructor with writeNormalizationWrapperFactory", nameof(skipWriteNormalization));
        }
    }

    public async Task<WritingSystems> GetWritingSystems()
    {
        return await _miniLcmApi.GetWritingSystems();
    }

    public virtual async Task<WritingSystem> CreateWritingSystem(WritingSystem writingSystem)
    {
        var newWritingSystem = await _miniLcmApi.CreateWritingSystem(writingSystem);
        return newWritingSystem;
    }

    public virtual async Task<WritingSystem> UpdateWritingSystem(WritingSystemId id,
        WritingSystemType type,
        JsonPatchDocument<WritingSystem> update)
    {
        var writingSystem =
            await _miniLcmApi.UpdateWritingSystem(id, type, new UpdateObjectInput<WritingSystem>(update));
        return writingSystem;
    }

    public IAsyncEnumerable<PartOfSpeech> GetPartsOfSpeech()
    {
        return _miniLcmApi.GetPartsOfSpeech();
    }

    public IAsyncEnumerable<SemanticDomain> GetSemanticDomains()
    {
        return _miniLcmApi.GetSemanticDomains();
    }

    public IAsyncEnumerable<ComplexFormType> GetComplexFormTypes()
    {
        return _miniLcmApi.GetComplexFormTypes();
    }

    public virtual IAsyncEnumerable<Entry> GetEntries(QueryOptions? options = null)
    {
        return _miniLcmApi.GetEntries(options);
    }

    public virtual IAsyncEnumerable<Entry> SearchEntries(string query, QueryOptions? options = null)
    {
        return _miniLcmApi.SearchEntries(query, options);
    }

    public async Task<Entry?> GetEntry(Guid id)
    {
        return await _miniLcmApi.GetEntry(id);
    }

    public virtual async Task<Entry> CreateEntry(Entry entry)
    {
        var newEntry = await _miniLcmApi.CreateEntry(entry);
        await NotifyEntryUpdated(newEntry);
        return newEntry;
    }

    public virtual async Task<Entry> UpdateEntry(Entry before, Entry after)
    {
        var entry = await _miniLcmApi.UpdateEntry(before, after);
        await NotifyEntryUpdated(entry);
        return entry;
    }

    public async Task DeleteEntry(Guid id)
    {
        await _miniLcmApi.DeleteEntry(id);
    }

    public virtual async Task<Sense> CreateSense(Guid entryId, Sense sense)
    {
        var createdSense = await _miniLcmApi.CreateSense(entryId, sense);
        return createdSense;
    }

    public virtual async Task<Sense> UpdateSense(Guid entryId, Guid senseId, JsonPatchDocument<Sense> update)
    {
        var sense = await _miniLcmApi.UpdateSense(entryId, senseId, new UpdateObjectInput<Sense>(update));
        return sense;
    }

    public async Task DeleteSense(Guid entryId, Guid senseId)
    {
        await _miniLcmApi.DeleteSense(entryId, senseId);
    }

    public virtual async Task<ExampleSentence> CreateExampleSentence(Guid entryId,
        Guid senseId,
        ExampleSentence exampleSentence)
    {
        var createdSentence = await _miniLcmApi.CreateExampleSentence(entryId, senseId, exampleSentence);
        return createdSentence;
    }

    public virtual async Task<ExampleSentence> UpdateExampleSentence(Guid entryId,
        Guid senseId,
        Guid exampleSentenceId,
        JsonPatchDocument<ExampleSentence> update)
    {
        var sentence = await _miniLcmApi.UpdateExampleSentence(entryId,
            senseId,
            exampleSentenceId,
            new UpdateObjectInput<ExampleSentence>(update));
        return sentence;
    }

    public async Task DeleteExampleSentence(Guid entryId, Guid senseId, Guid exampleSentenceId)
    {
        await _miniLcmApi.DeleteExampleSentence(entryId, senseId, exampleSentenceId);
    }

    protected virtual async Task NotifyEntryUpdated(Entry entry)
    {
        await Clients.Others.OnEntryUpdated(entry);
    }
}
