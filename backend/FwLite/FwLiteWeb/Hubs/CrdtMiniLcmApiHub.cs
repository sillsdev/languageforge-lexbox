using FwLiteShared;
using FwLiteShared.Events;
using FwLiteShared.Projects;
using FwLiteShared.Sync;
using LcmCrdt;
using LcmCrdt.Data;
using FwLiteWeb.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;
using MiniLcm;
using MiniLcm.Models;
using MiniLcm.Normalization;
using MiniLcm.Validators;
using SystemTextJsonPatch;

namespace FwLiteWeb.Hubs;

public class CrdtMiniLcmApiHub(
    IMiniLcmApi miniLcmApi,
    BackgroundSyncService backgroundSyncService,
    SyncService syncService,
    ProjectEventBus projectEventBus,
    CurrentProjectService projectContext,
    LexboxProjectService lexboxProjectService,
    IMemoryCache memoryCache,
    IHubContext<CrdtMiniLcmApiHub, ILexboxHubClient> hubContext,
    MiniLcmApiValidationWrapperFactory validationWrapperFactory,
    MiniLcmWriteApiNormalizationWrapperFactory writeNormalizationWrapperFactory
) : MiniLcmApiHubBase(miniLcmApi, validationWrapperFactory, writeNormalizationWrapperFactory)
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
            projectEventBus.OnEntryChanged(projectContext.Project).Subscribe(e => OnEntryChangedExternal(e.Entry, hubContext, memoryCache, Context.ConnectionId))
        ];

        await lexboxProjectService.ListenForProjectChanges(projectContext.ProjectData, Context.ConnectionAborted);
    }

    private void TriggerSync()
    {
        backgroundSyncService.TriggerSync(projectContext.Project);
    }

    private static void OnEntryChangedExternal(Entry entry,
        IHubContext<CrdtMiniLcmApiHub, ILexboxHubClient> hubContext,
        IMemoryCache cache,
        string connectionId)
    {
        var currentFilter = CurrentProjectFilter(cache, connectionId);
        if (currentFilter.Invoke(entry))
        {
            _ = hubContext.Clients.Client(connectionId).OnEntryUpdated(entry);
        }
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);
        foreach (var disposable in Cleanup)
        {
            disposable.Dispose();
        }
        memoryCache.Remove($"CurrentFilter|HubConnectionId={Context.ConnectionId}");
    }

    private Func<Entry, bool> CurrentFilter
    {
        set => memoryCache.Set($"CurrentFilter|HubConnectionId={Context.ConnectionId}", value);
    }

    public static Func<Entry, bool> CurrentProjectFilter(IMemoryCache memoryCache, string connectionId)
    {
        return memoryCache.Get<Func<Entry, bool>>(
            $"CurrentFilter|HubConnectionId={connectionId}") ?? (_ => true);
    }

    public override IAsyncEnumerable<Entry> GetEntries(QueryOptions? options = null)
    {
        CurrentFilter =
            Filtering.CompiledFilter(null, options?.Exemplar?.WritingSystem ?? "default", options?.Exemplar?.Value);
        return base.GetEntries(options);
    }

    public override IAsyncEnumerable<Entry> SearchEntries(string query, QueryOptions? options = null)
    {
        CurrentFilter = Filtering.CompiledFilter(query,
            options?.Exemplar?.WritingSystem ?? "default",
            options?.Exemplar?.Value);
        return base.SearchEntries(query, options);
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
