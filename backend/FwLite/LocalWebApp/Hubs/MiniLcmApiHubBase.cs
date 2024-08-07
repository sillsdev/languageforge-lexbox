﻿using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using MiniLcm;
using SystemTextJsonPatch;

namespace LocalWebApp.Hubs;

public abstract class MiniLcmApiHubBase(ILexboxApi lexboxApi) : Hub<ILexboxHubClient>
{
    public async Task<WritingSystems> GetWritingSystems()
    {
        return await lexboxApi.GetWritingSystems();
    }

    public virtual async Task<WritingSystem> CreateWritingSystem(WritingSystemType type, WritingSystem writingSystem)
    {
        var newWritingSystem = await lexboxApi.CreateWritingSystem(type, writingSystem);
        return newWritingSystem;
    }

    public virtual async Task<WritingSystem> UpdateWritingSystem(WritingSystemId id,
        WritingSystemType type,
        JsonPatchDocument<WritingSystem> update)
    {
        var writingSystem =
            await lexboxApi.UpdateWritingSystem(id, type, new JsonPatchUpdateInput<WritingSystem>(update));
        return writingSystem;
    }

    public IAsyncEnumerable<PartOfSpeech> GetPartsOfSpeech()
    {
        return lexboxApi.GetPartsOfSpeech();
    }

    public IAsyncEnumerable<SemanticDomain> GetSemanticDomains()
    {
        return lexboxApi.GetSemanticDomains();
    }

    public IAsyncEnumerable<Entry> GetEntries(QueryOptions? options = null)
    {
        return lexboxApi.GetEntries(options);
    }

    public IAsyncEnumerable<Entry> SearchEntries(string query, QueryOptions? options = null)
    {
        return lexboxApi.SearchEntries(query, options);
    }

    public async Task<Entry?> GetEntry(Guid id)
    {
        return await lexboxApi.GetEntry(id);
    }

    public virtual async Task<Entry> CreateEntry(Entry entry)
    {
        var newEntry = await lexboxApi.CreateEntry(entry);
        await NotifyEntryUpdated(newEntry);
        return newEntry;
    }

    public virtual async Task<Entry> UpdateEntry(Guid id, JsonPatchDocument<Entry> update)
    {
        var entry = await lexboxApi.UpdateEntry(id, new JsonPatchUpdateInput<Entry>(update));
        await NotifyEntryUpdated(entry);
        return entry;
    }

    public async Task DeleteEntry(Guid id)
    {
        await lexboxApi.DeleteEntry(id);
    }

    public virtual async Task<Sense> CreateSense(Guid entryId, Sense sense)
    {
        var createdSense = await lexboxApi.CreateSense(entryId, sense);
        return createdSense;
    }

    public virtual async Task<Sense> UpdateSense(Guid entryId, Guid senseId, JsonPatchDocument<Sense> update)
    {
        var sense = await lexboxApi.UpdateSense(entryId, senseId, new JsonPatchUpdateInput<Sense>(update));
        return sense;
    }

    public async Task DeleteSense(Guid entryId, Guid senseId)
    {
        await lexboxApi.DeleteSense(entryId, senseId);
    }

    public virtual async Task<ExampleSentence> CreateExampleSentence(Guid entryId,
        Guid senseId,
        ExampleSentence exampleSentence)
    {
        var createdSentence = await lexboxApi.CreateExampleSentence(entryId, senseId, exampleSentence);
        return createdSentence;
    }

    public virtual async Task<ExampleSentence> UpdateExampleSentence(Guid entryId,
        Guid senseId,
        Guid exampleSentenceId,
        JsonPatchDocument<ExampleSentence> update)
    {
        var sentence = await lexboxApi.UpdateExampleSentence(entryId,
            senseId,
            exampleSentenceId,
            new JsonPatchUpdateInput<ExampleSentence>(update));
        return sentence;
    }

    public async Task DeleteExampleSentence(Guid entryId, Guid senseId, Guid exampleSentenceId)
    {
        await lexboxApi.DeleteExampleSentence(entryId, senseId, exampleSentenceId);
    }

    protected virtual async Task NotifyEntryUpdated(Entry entry)
    {
        await Clients.Others.OnEntryUpdated(entry);
    }
}
