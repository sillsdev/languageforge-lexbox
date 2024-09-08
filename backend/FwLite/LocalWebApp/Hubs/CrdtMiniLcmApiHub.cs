using LcmCrdt;
using LcmCrdt.Data;
using LocalWebApp.Services;
using Microsoft.Extensions.Caching.Memory;
using MiniLcm;
using SystemTextJsonPatch;

namespace LocalWebApp.Hubs;

public class CrdtMiniLcmApiHub(
    ILexboxApi lexboxApi,
    BackgroundSyncService backgroundSyncService,
    SyncService syncService,
    ChangeEventBus changeEventBus,
    CurrentProjectService projectContext,
    LexboxProjectService lexboxProjectService,
    IMemoryCache memoryCache) : MiniLcmApiHubBase(lexboxApi)
{
    public const string ProjectRouteKey = "project";
    public static string ProjectGroup(string projectName) => "crdt-" + projectName;

    public override async Task OnConnectedAsync()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, ProjectGroup(projectContext.Project.Name));
        await syncService.ExecuteSync();
        IDisposable[] cleanup =
        [
            //todo this results in a memory leak, due to holding on to the hub instance, it will be disposed even if the context items are not.
            changeEventBus.ListenForEntryChanges(projectContext.Project.Name, Context.ConnectionId)
        ];
        Context.Items["clanup"] = cleanup;

        await lexboxProjectService.ListenForProjectChanges(projectContext.ProjectData, Context.ConnectionAborted);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);
        foreach (var disposable in Context.Items["cleanup"] as IDisposable[] ?? [])
        {
            disposable.Dispose();
        }
    }

    private Func<LcmCrdt.Objects.Entry, bool> CurrentFilter
    {
        set => memoryCache.Set($"CurrentFilter|HubConnectionId={Context.ConnectionId}", value);
    }

    public static Func<LcmCrdt.Objects.Entry, bool> CurrentProjectFilter(IMemoryCache memoryCache, string connectionId)
    {
        return memoryCache.Get<Func<LcmCrdt.Objects.Entry, bool>>(
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
