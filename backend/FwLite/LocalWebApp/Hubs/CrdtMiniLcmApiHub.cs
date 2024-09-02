using LcmCrdt;
using LocalWebApp.Services;
using Microsoft.AspNetCore.SignalR.Client;
using MiniLcm;
using SystemTextJsonPatch;

namespace LocalWebApp.Hubs;

public class CrdtMiniLcmApiHub(
    ILexboxApi lexboxApi,
    BackgroundSyncService backgroundSyncService,
    SyncService syncService,
    ChangeEventBus changeEventBus,
    CurrentProjectService projectContext,
    LexboxProjectService lexboxProjectService) : MiniLcmApiHubBase(lexboxApi)
{
    public const string ProjectRouteKey = "project";
    public static string ProjectGroup(string projectName) => "crdt-" + projectName;

    public override async Task OnConnectedAsync()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, ProjectGroup(projectContext.Project.Name));
        await syncService.ExecuteSync();
        changeEventBus.SetupGlobalSignalRSubscription();

        await lexboxProjectService.ListenForProjectChanges(projectContext.ProjectData, Context.ConnectionAborted);
    }

    public override async Task<WritingSystem> CreateWritingSystem(WritingSystemType type, WritingSystem writingSystem)
    {
        var newWritingSystem = await base.CreateWritingSystem(type, writingSystem);
        backgroundSyncService.TriggerSync();
        return newWritingSystem;
    }

    public override async Task<WritingSystem> UpdateWritingSystem(WritingSystemId id,
        WritingSystemType type,
        JsonPatchDocument<WritingSystem> update)
    {
        var writingSystem = await base.UpdateWritingSystem(id, type, update);
        backgroundSyncService.TriggerSync();
        return writingSystem;
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
