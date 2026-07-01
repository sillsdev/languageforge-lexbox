using FwLiteShared.Events;
using FwLiteShared.Projects;
using FwLiteShared.Sync;
using LcmCrdt;
using LcmCrdt.Data;
using Microsoft.AspNetCore.SignalR;
using MiniLcm;
using MiniLcm.Models;
using MiniLcm.Wrappers;
using SystemTextJsonPatch;

namespace FwLiteWeb.Hubs;

public class CrdtMiniLcmApiHub(
    IMiniLcmApi miniLcmApi,
    IBackgroundSyncService backgroundSyncService,
    SyncService syncService,
    ProjectEventBus projectEventBus,
    CurrentProjectService projectContext,
    LexboxProjectService lexboxProjectService,
    IHubContext<CrdtMiniLcmApiHub, ILexboxHubClient> hubContext,
    MiniLcmApiUserFacingWrappers userFacingWrappers
) : MiniLcmApiHubBase(miniLcmApi, userFacingWrappers, projectContext.Project)
{
    public const string ProjectRouteKey = "project";
    public static string ProjectGroup(string projectName) => "crdt-" + projectName;
    private IDisposable[] Cleanup
    {
        get => Context.Items["cleanup"] as IDisposable[] ?? [];
        set => Context.Items["cleanup"] = value;
    }

    public override async Task OnConnectedAsync()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, ProjectGroup(projectContext.Project.Name));
        await syncService.SafeExecuteSync(true);
        Cleanup =
        [
            projectEventBus.OnEntriesChanged(projectContext.Project).Subscribe(e => OnEntriesChangedExternal(e, hubContext, Context.ConnectionId))
        ];

        await lexboxProjectService.ListenForProjectChanges(projectContext.ProjectData, Context.ConnectionAborted);
    }

    private void TriggerSync()
    {
        backgroundSyncService.TriggerSync(projectContext.Project);
    }

    private static void OnEntriesChangedExternal(EntriesChangedEvent e,
        IHubContext<CrdtMiniLcmApiHub, ILexboxHubClient> hubContext,
        string connectionId)
    {
        _ = hubContext.Clients.Client(connectionId).OnEntriesChanged(e.ChangedEntryIds, e.DeletedEntryIds);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);
        foreach (var disposable in Cleanup)
        {
            disposable.Dispose();
        }
    }

    public override async Task<MorphType> UpdateMorphType(Guid id, JsonPatchDocument<MorphType> update)
    {
        var updatedMorphType = await base.UpdateMorphType(id, update);
        TriggerSync();
        return updatedMorphType;
    }

    public override async Task<WritingSystem> CreateWritingSystem(WritingSystem writingSystem)
    {
        var newWritingSystem = await base.CreateWritingSystem(writingSystem);
        TriggerSync();
        return newWritingSystem;
    }

    public override async Task<WritingSystem> UpdateWritingSystem(WritingSystemId id,
        WritingSystemType type,
        JsonPatchDocument<WritingSystem> update)
    {
        var writingSystem = await base.UpdateWritingSystem(id, type, update);
        TriggerSync();
        return writingSystem;
    }

    public override async Task<Sense> CreateSense(Guid entryId, Sense sense)
    {
        var createdSense = await base.CreateSense(entryId, sense);
        TriggerSync();
        return createdSense;
    }

    public override async Task<Sense> UpdateSense(Guid entryId, Guid senseId, JsonPatchDocument<Sense> update)
    {
        var sense = await base.UpdateSense(entryId, senseId, update);
        TriggerSync();
        return sense;
    }

    public override async Task<ExampleSentence> CreateExampleSentence(Guid entryId,
        Guid senseId,
        ExampleSentence exampleSentence)
    {
        var createdSentence = await base.CreateExampleSentence(entryId, senseId, exampleSentence);
        TriggerSync();
        return createdSentence;
    }

    public override async Task<ExampleSentence> UpdateExampleSentence(Guid entryId,
        Guid senseId,
        Guid exampleSentenceId,
        JsonPatchDocument<ExampleSentence> update)
    {
        var sentence = await base.UpdateExampleSentence(entryId, senseId, exampleSentenceId, update);
        TriggerSync();
        return sentence;
    }

    protected override async Task NotifyEntryUpdated(Entry entry)
    {
        await base.NotifyEntryUpdated(entry);
        TriggerSync();
    }
}
