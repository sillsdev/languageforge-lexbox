using FwLiteShared.Events;
using LexCore.Utils;
using MiniLcm;
using MiniLcm.Models;
using MiniLcm.SyncHelpers;
using MiniLcm.Wrappers;

namespace FwLiteShared.Services;

public class MiniLcmApiNotifyWrapperFactory(ProjectEventBus bus) : IMiniLcmWrapperFactory
{
    public IMiniLcmApi Create(IMiniLcmApi api, IProjectIdentifier project)
    {
        return new MiniLcmApiNotifyWrapper(api, bus, project);
    }
}

public partial class MiniLcmApiNotifyWrapper(
    IMiniLcmApi api,
    ProjectEventBus bus,
    IProjectIdentifier project) : IMiniLcmApi
{
    [BeaKona.AutoInterface(IncludeBaseInterfaces = true, MemberMatch = BeaKona.MemberMatchTypes.Any)]
    private readonly IMiniLcmApi _api = api;

    private PendingChangeNotifications? _pendingChanges;

    private IAsyncDisposable BeginTrackingChanges()
    {
        if (_pendingChanges is not null) return Defer.NoopAsync;
        return _pendingChanges = new PendingChangeNotifications(this);
    }

    private record PendingChangeNotifications(MiniLcmApiNotifyWrapper Wrapper): IAsyncDisposable
    {
        public HashSet<Guid> ChangedEntryIds { get; init; } = [];
        public HashSet<Guid> DeletedEntryIds { get; init; } = [];

        public ValueTask DisposeAsync()
        {
            Wrapper._pendingChanges = null;
            Wrapper.PublishChanges([.. ChangedEntryIds.Except(DeletedEntryIds)], [.. DeletedEntryIds]);
            return ValueTask.CompletedTask;
        }
    }

    public void NotifyEntryChanged(Entry entry) => NotifyEntryChanged(entry.Id);

    public void NotifyEntryChanged(Guid entryId)
    {
        if (_pendingChanges is not null)
        {
            _pendingChanges.ChangedEntryIds.Add(entryId);
            return;
        }
        PublishChanges([entryId], []);
    }

    public void NotifyEntryDeleted(Guid entryId)
    {
        if (_pendingChanges is not null)
        {
            _pendingChanges.DeletedEntryIds.Add(entryId);
            return;
        }
        PublishChanges([], [entryId]);
    }

    private void PublishChanges(Guid[] changedEntryIds, Guid[] deletedEntryIds)
    {
        if (changedEntryIds.Length == 0 && deletedEntryIds.Length == 0) return;
        bus.PublishEntriesChanged(project, changedEntryIds, deletedEntryIds);
    }

    // ********** Overrides go here **********

    async Task<Entry> IMiniLcmWriteApi.CreateEntry(Entry entry, CreateEntryOptions? options)
    {
        await using var _ = BeginTrackingChanges();
        var result = await _api.CreateEntry(entry, options);
        NotifyEntryChanged(result);
        return result;
    }

    async Task<Entry> IMiniLcmWriteApi.UpdateEntry(Entry before, Entry after, IMiniLcmApi? api)
    {
        await using var _ = BeginTrackingChanges();
        var result = await _api.UpdateEntry(before, after, api ?? this);
        NotifyEntryChanged(result);
        return result;
    }

    async Task<Sense> IMiniLcmWriteApi.CreateSense(Guid entryId, Sense sense, BetweenPosition? position)
    {
        await using var _ = BeginTrackingChanges();
        var result = await _api.CreateSense(entryId, sense, position);
        var entry = await _api.GetEntry(entryId) ?? throw new NullReferenceException($"Entry {entryId} not found");
        NotifyEntryChanged(entry);
        return result;
    }

    async Task<Sense> IMiniLcmWriteApi.UpdateSense(Guid entryId, Sense before, Sense after, IMiniLcmApi? api)
    {
        await using var _ = BeginTrackingChanges();
        var result = await _api.UpdateSense(entryId, before, after, api ?? this);
        var entry = await _api.GetEntry(entryId) ?? throw new NullReferenceException($"Entry {entryId} not found");
        NotifyEntryChanged(entry);
        return result; 
    }

    async Task<ExampleSentence> IMiniLcmWriteApi.CreateExampleSentence(Guid entryId, Guid senseId, ExampleSentence exampleSentence, BetweenPosition? position)
    {
        await using var _ = BeginTrackingChanges();
        var result = await _api.CreateExampleSentence(entryId, senseId, exampleSentence, position);
        var entry = await _api.GetEntry(entryId) ?? throw new NullReferenceException($"Entry {entryId} not found");
        NotifyEntryChanged(entry);
        return result;
    }

    async Task<ExampleSentence> IMiniLcmWriteApi.UpdateExampleSentence(Guid entryId, Guid senseId, ExampleSentence before, ExampleSentence after, IMiniLcmApi? api)
    {
        await using var _ = BeginTrackingChanges();
        var result = await _api.UpdateExampleSentence(entryId, senseId, before, after, api ?? this);
        var entry = await _api.GetEntry(entryId) ?? throw new NullReferenceException($"Entry {entryId} not found");
        NotifyEntryChanged(entry);
        return result;
    }

    async Task<ComplexFormComponent> IMiniLcmWriteApi.CreateComplexFormComponent(ComplexFormComponent complexFormComponent, BetweenPosition<ComplexFormComponent>? position)
    {
        var result = await _api.CreateComplexFormComponent(complexFormComponent, position);
        NotifyEntryChanged(result.ComplexFormEntryId);
        NotifyEntryChanged(result.ComponentEntryId);
        return result;
    }

    async Task IMiniLcmWriteApi.DeleteComplexFormComponent(ComplexFormComponent complexFormComponent)
    {
        await _api.DeleteComplexFormComponent(complexFormComponent);
        NotifyEntryChanged(complexFormComponent.ComplexFormEntryId);
        NotifyEntryChanged(complexFormComponent.ComponentEntryId);
    }

    async Task IMiniLcmWriteApi.DeleteEntry(Guid id)
    {
        await _api.DeleteEntry(id);
        NotifyEntryDeleted(id);
    }

    async Task IMiniLcmWriteApi.DeleteSense(Guid entryId, Guid senseId)
    {
        await _api.DeleteSense(entryId, senseId);
        NotifyEntryChanged(entryId);
    }

    async Task IMiniLcmWriteApi.DeleteExampleSentence(Guid entryId, Guid senseId, Guid exampleSentenceId)
    {
        await _api.DeleteExampleSentence(entryId, senseId, exampleSentenceId);
        NotifyEntryChanged(entryId);
    }

    void IDisposable.Dispose()
    {
    }
}
