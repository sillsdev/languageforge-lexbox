using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using MiniLcm;
using SystemTextJsonPatch;
using SystemTextJsonPatch.Operations;

namespace LocalWebApp;

public record JsonOperation(string Op, string Path, object? Value = null, string? From = null);

public interface ILexboxClient
{
    Task OnEntryUpdated(Entry entry);
}

public class LexboxApiHub(ILexboxApi lexboxApi, IOptions<JsonOptions> jsonOptions) : Hub<ILexboxClient>
{
    public async Task<WritingSystems> GetWritingSystems()
    {
        return await lexboxApi.GetWritingSystems();
    }

    public IAsyncEnumerable<Entry> GetEntriesForExemplar(string exemplar, QueryOptions? options = null)
    {
        throw new NotImplementedException();
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

    public async Task<Entry> CreateEntry(Entry entry)
    {
        var newEntry = await lexboxApi.CreateEntry(entry);
        await NotifyEntryUpdated(newEntry);
        return newEntry;
    }

    public async Task<Entry> UpdateEntry(Guid id, JsonOperation[] update)
    {
        var entry = await lexboxApi.UpdateEntry(id, FromOperations<Entry>(update));
        await NotifyEntryUpdated(entry);
        return entry;
    }

    public async Task DeleteEntry(Guid id)
    {
        await lexboxApi.DeleteEntry(id);
    }

    public async Task<Sense> CreateSense(Guid entryId, Sense sense)
    {
        return await lexboxApi.CreateSense(entryId, sense);
    }

    public async Task<Sense> UpdateSense(Guid entryId, Guid senseId, JsonOperation[] update)
    {
        return await lexboxApi.UpdateSense(entryId, senseId, FromOperations<Sense>(update));
    }

    public async Task DeleteSense(Guid entryId, Guid senseId)
    {
        await lexboxApi.DeleteSense(entryId, senseId);
    }

    public async Task<ExampleSentence> CreateExampleSentence(Guid entryId,
        Guid senseId,
        ExampleSentence exampleSentence)
    {
        return await lexboxApi.CreateExampleSentence(entryId, senseId, exampleSentence);
    }

    public async Task<ExampleSentence> UpdateExampleSentence(Guid entryId,
        Guid senseId,
        Guid exampleSentenceId,
        JsonOperation[] update)
    {
        return await lexboxApi.UpdateExampleSentence(entryId,
            senseId,
            exampleSentenceId,
            FromOperations<ExampleSentence>(update));
    }

    public async Task DeleteExampleSentence(Guid entryId, Guid senseId, Guid exampleSentenceId)
    {
        await lexboxApi.DeleteExampleSentence(entryId, senseId, exampleSentenceId);
    }

    private JsonPatchUpdateInput<T> FromOperations<T>(JsonOperation[] operations) where T : class
    {
        return new JsonPatchUpdateInput<T>(
            new JsonPatchDocument<T>(operations.Select(o => new Operation<T>(o.Op, o.Path, o.From, o.Value)).ToList(),
                jsonOptions.Value.SerializerOptions)
        );
    }

    private async Task NotifyEntryUpdated(Entry entry)
    {
        await Clients.Others.OnEntryUpdated(entry);
    }
}
