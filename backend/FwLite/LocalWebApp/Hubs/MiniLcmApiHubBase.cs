using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using MiniLcm;
using MiniLcm.Models;
using SystemTextJsonPatch;

namespace LocalWebApp.Hubs;

public abstract class MiniLcmApiHubBase(IMiniLcmApi miniLcmApi) : Hub<ILexboxHubClient>
{
    public async Task<WritingSystems> GetWritingSystems()
    {
        return await miniLcmApi.GetWritingSystems();
    }

    public virtual async Task<WritingSystem> CreateWritingSystem(WritingSystemType type, WritingSystem writingSystem)
    {
        var newWritingSystem = await miniLcmApi.CreateWritingSystem(type, writingSystem);
        return newWritingSystem;
    }

    public virtual async Task<WritingSystem> UpdateWritingSystem(WritingSystemId id,
        WritingSystemType type,
        JsonPatchDocument<WritingSystem> update)
    {
        var writingSystem =
            await miniLcmApi.UpdateWritingSystem(id, type, new UpdateObjectInput<WritingSystem>(update));
        return writingSystem;
    }

    public IAsyncEnumerable<PartOfSpeech> GetPartsOfSpeech()
    {
        return miniLcmApi.GetPartsOfSpeech();
    }

    public IAsyncEnumerable<SemanticDomain> GetSemanticDomains()
    {
        return miniLcmApi.GetSemanticDomains();
    }

    public IAsyncEnumerable<Entry> GetEntries(QueryOptions? options = null)
    {
        return miniLcmApi.GetEntries(options);
    }

    public IAsyncEnumerable<Entry> SearchEntries(string query, QueryOptions? options = null)
    {
        return miniLcmApi.SearchEntries(query, options);
    }

    public async Task<Entry?> GetEntry(Guid id)
    {
        return await miniLcmApi.GetEntry(id);
    }

    public virtual async Task<Entry> CreateEntry(Entry entry)
    {
        var newEntry = await miniLcmApi.CreateEntry(entry);
        await NotifyEntryUpdated(newEntry);
        return newEntry;
    }

    public virtual async Task<Entry> UpdateEntry(Guid id, JsonPatchDocument<Entry> update)
    {
        var entry = await miniLcmApi.UpdateEntry(id, new UpdateObjectInput<Entry>(update));
        await NotifyEntryUpdated(entry);
        return entry;
    }

    public async Task DeleteEntry(Guid id)
    {
        await miniLcmApi.DeleteEntry(id);
    }

    public virtual async Task<Sense> CreateSense(Guid entryId, Sense sense)
    {
        var createdSense = await miniLcmApi.CreateSense(entryId, sense);
        return createdSense;
    }

    public virtual async Task<Sense> UpdateSense(Guid entryId, Guid senseId, JsonPatchDocument<Sense> update)
    {
        var sense = await miniLcmApi.UpdateSense(entryId, senseId, new UpdateObjectInput<Sense>(update));
        return sense;
    }

    public async Task DeleteSense(Guid entryId, Guid senseId)
    {
        await miniLcmApi.DeleteSense(entryId, senseId);
    }

    public virtual async Task<ExampleSentence> CreateExampleSentence(Guid entryId,
        Guid senseId,
        ExampleSentence exampleSentence)
    {
        var createdSentence = await miniLcmApi.CreateExampleSentence(entryId, senseId, exampleSentence);
        return createdSentence;
    }

    public virtual async Task<ExampleSentence> UpdateExampleSentence(Guid entryId,
        Guid senseId,
        Guid exampleSentenceId,
        JsonPatchDocument<ExampleSentence> update)
    {
        var sentence = await miniLcmApi.UpdateExampleSentence(entryId,
            senseId,
            exampleSentenceId,
            new UpdateObjectInput<ExampleSentence>(update));
        return sentence;
    }

    public async Task DeleteExampleSentence(Guid entryId, Guid senseId, Guid exampleSentenceId)
    {
        await miniLcmApi.DeleteExampleSentence(entryId, senseId, exampleSentenceId);
    }

    protected virtual async Task NotifyEntryUpdated(Entry entry)
    {
        await Clients.Others.OnEntryUpdated(entry);
    }
}
