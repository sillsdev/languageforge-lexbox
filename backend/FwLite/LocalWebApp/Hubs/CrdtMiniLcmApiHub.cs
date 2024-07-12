using Microsoft.AspNetCore.SignalR;
using MiniLcm;
using SystemTextJsonPatch;

namespace LocalWebApp.Hubs;

public class CrdtMiniLcmApiHub(
    ILexboxApi lexboxApi,
    BackgroundSyncService backgroundSyncService,
    SyncService syncService) : MiniLcmApiHubBase(lexboxApi)
{
    public const string ProjectRouteKey = "project";
    public override async Task OnConnectedAsync()
    {
        await syncService.ExecuteSync();
    }

    public override async Task<WritingSystem> CreateWritingSystem(WritingSystemType type, WritingSystem writingSystem)
    {
        var newWritingSystem = await base.CreateWritingSystem(type, writingSystem);
        backgroundSyncService.TriggerSync();
        return newWritingSystem;
    }

    public override async Task<WritingSystem> UpdateWritingSystem(WritingSystemId id, WritingSystemType type, JsonPatchDocument<WritingSystem> update)
    {
        var writingSystem = await base.UpdateWritingSystem(id, type, update);
        backgroundSyncService.TriggerSync();
        return writingSystem;
    }

    public override async Task<Entry> CreateEntry(Entry entry)
    {
        var newEntry = await base.CreateEntry(entry);
        await NotifyEntryUpdated(newEntry);
        return newEntry;
    }

    public override async Task<Entry> UpdateEntry(Guid id, JsonPatchDocument<Entry> update)
    {
        var entry = await base.UpdateEntry(id, update);
        await NotifyEntryUpdated(entry);
        return entry;
    }

    public override async Task<Sense> CreateSense(Guid entryId, Sense sense)
    {
        var createdSense = await base.CreateSense(entryId, sense);
        backgroundSyncService.TriggerSync();
        return createdSense;
    }

    public override async Task<Sense> UpdateSense(Guid entryId, Guid senseId, JsonPatchDocument<Sense> update)
    {
        var sense = await base.UpdateSense(entryId, senseId, update);
        backgroundSyncService.TriggerSync();
        return sense;
    }

    public override async Task<ExampleSentence> CreateExampleSentence(Guid entryId,
        Guid senseId,
        ExampleSentence exampleSentence)
    {
        var createdSentence = await base.CreateExampleSentence(entryId, senseId, exampleSentence);
        backgroundSyncService.TriggerSync();
        return createdSentence;
    }

    public override async Task<ExampleSentence> UpdateExampleSentence(Guid entryId,
        Guid senseId,
        Guid exampleSentenceId,
        JsonPatchDocument<ExampleSentence> update)
    {
        var sentence = await base.UpdateExampleSentence(entryId, senseId, exampleSentenceId, update);
        backgroundSyncService.TriggerSync();
        return sentence;
    }

    protected override async Task NotifyEntryUpdated(Entry entry)
    {
        await base.NotifyEntryUpdated(entry);
        backgroundSyncService.TriggerSync();
    }
}
