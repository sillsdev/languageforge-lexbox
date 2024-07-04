using FwDataMiniLcmBridge;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using MiniLcm;
using SystemTextJsonPatch;

namespace LocalWebApp.Hubs;

public class FwDataMiniLcmHub([FromKeyedServices(FwDataBridgeKernel.FwDataApiKey)] ILexboxApi lexboxApi, FwDataFactory fwDataFactory,
    FwDataProjectContext context) : Hub<ILexboxClient>
{
    public const string ProjectRouteKey = "fwdata";
    public override async Task OnConnectedAsync()
    {
        var project = context.Project;
        if (project is null)
        {
            throw new InvalidOperationException("No project is set in the context.");
        }
        await Groups.AddToGroupAsync(Context.ConnectionId, project.Name);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        //todo if multiple clients are connected, this will close the project for all of them.
        fwDataFactory.CloseCurrentProject();
        var project = context.Project;
        if (project is null)
        {
            throw new InvalidOperationException("No project is set in the context.");
        }
        await Clients.OthersInGroup(project.Name).OnProjectClosed();
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, project.Name);
    }

    public async Task<WritingSystems> GetWritingSystems()
    {
        return await lexboxApi.GetWritingSystems();
    }

    public async Task<WritingSystem> CreateWritingSystem(WritingSystemType type, WritingSystem writingSystem)
    {
        var newWritingSystem = await lexboxApi.CreateWritingSystem(type, writingSystem);
        return newWritingSystem;
    }

    public async Task<WritingSystem> UpdateWritingSystem(WritingSystemId id, WritingSystemType type, JsonPatchDocument<WritingSystem> update)
    {
        var writingSystem = await lexboxApi.UpdateWritingSystem(id, type, new JsonPatchUpdateInput<WritingSystem>(update));
        return writingSystem;
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

    public async Task<Entry> UpdateEntry(Guid id, JsonPatchDocument<Entry> update)
    {
        var entry = await lexboxApi.UpdateEntry(id, new JsonPatchUpdateInput<Entry>(update));
        await NotifyEntryUpdated(entry);
        return entry;
    }

    public async Task DeleteEntry(Guid id)
    {
        await lexboxApi.DeleteEntry(id);
    }

    public async Task<Sense> CreateSense(Guid entryId, Sense sense)
    {
        var createdSense = await lexboxApi.CreateSense(entryId, sense);
        return createdSense;
    }

    public async Task<Sense> UpdateSense(Guid entryId, Guid senseId, JsonPatchDocument<Sense> update)
    {
        var sense = await lexboxApi.UpdateSense(entryId, senseId, new JsonPatchUpdateInput<Sense>(update));
        return sense;
    }

    public async Task DeleteSense(Guid entryId, Guid senseId)
    {
        await lexboxApi.DeleteSense(entryId, senseId);
    }

    public async Task<ExampleSentence> CreateExampleSentence(Guid entryId,
        Guid senseId,
        ExampleSentence exampleSentence)
    {
        var createdSentence = await lexboxApi.CreateExampleSentence(entryId, senseId, exampleSentence);
        return createdSentence;
    }

    public async Task<ExampleSentence> UpdateExampleSentence(Guid entryId,
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

    private async Task NotifyEntryUpdated(Entry entry)
    {
        await Clients.Others.OnEntryUpdated(entry);
    }
}
