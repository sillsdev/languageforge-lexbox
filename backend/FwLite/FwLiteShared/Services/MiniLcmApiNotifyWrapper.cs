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
    [BeaKona.AutoInterface(IncludeBaseInterfaces = true)]
    private readonly IMiniLcmApi _api = api;

    private PendingChangeNotifications? _pendingChanges;

    private IAsyncDisposable BeginTrackingChanges()
    {
        if (_pendingChanges is not null) return Defer.NoopAsync;
        return _pendingChanges = new PendingChangeNotifications(this);
    }

    private record PendingChangeNotifications(MiniLcmApiNotifyWrapper Wrapper): IAsyncDisposable
    {
        public HashSet<Guid> EntryIds { get; init; } = [];
        public Dictionary<Guid, Entry> Entries { get; init; } = [];

        public async ValueTask DisposeAsync()
        {
            Wrapper._pendingChanges = null;
            foreach (var (id, entry) in Entries)
            {
                Wrapper.NotifyEntryChanged(entry);
                EntryIds.Remove(id);
            }
            foreach (var entryId in EntryIds)
            {
                await Wrapper.NotifyEntryChangedAsync(entryId);
            }
        }
    }

    public void NotifyEntryChanged(Entry entry)
    {
        if (_pendingChanges is not null)
        {
            _pendingChanges.Entries[entry.Id] = entry;
            return;
        }
        bus.PublishEntryChangedEvent(project, entry);
    }

    public async Task NotifyEntryChangedAsync(Guid entryId)
    {
        if (_pendingChanges is not null)
        {
            _pendingChanges.EntryIds.Add(entryId);
            return;
        }
        var entry = await _api.GetEntry(entryId);
        if (entry is null) return;
        bus.PublishEntryChangedEvent(project, entry);
    }

    public void NotifyEntryDeleted(Guid entryId)
    {
        bus.PublishEvent(project, new EntryDeletedEvent(entryId));
    }

    // ********** Overrides go here **********

    async Task<Entry> IMiniLcmWriteApi.CreateEntry(Entry entry)
    {
        await using var _ = BeginTrackingChanges();
        var result = await _api.CreateEntry(entry);
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

    async Task<ComplexFormComponent> IMiniLcmWriteApi.CreateComplexFormComponent(ComplexFormComponent complexFormComponent, BetweenPosition<ComplexFormComponent>? position)
    {
        var result = await _api.CreateComplexFormComponent(complexFormComponent, position);
        await NotifyEntryChangedAsync(result.ComplexFormEntryId);
        await NotifyEntryChangedAsync(result.ComponentEntryId);
        return result;
    }

    async Task IMiniLcmWriteApi.DeleteComplexFormComponent(ComplexFormComponent complexFormComponent)
    {
        await _api.DeleteComplexFormComponent(complexFormComponent);
        await NotifyEntryChangedAsync(complexFormComponent.ComplexFormEntryId);
        await NotifyEntryChangedAsync(complexFormComponent.ComponentEntryId);
    }

    async Task IMiniLcmWriteApi.DeleteEntry(Guid id)
    {
        await _api.DeleteEntry(id);
        NotifyEntryDeleted(id);
    }

    async Task IMiniLcmWriteApi.DeleteSense(Guid entryId, Guid senseId)
    {
        await _api.DeleteSense(entryId, senseId);
        await NotifyEntryChangedAsync(entryId);
    }

    async Task IMiniLcmWriteApi.DeleteExampleSentence(Guid entryId, Guid senseId, Guid exampleSentenceId)
    {
        await _api.DeleteExampleSentence(entryId, senseId, exampleSentenceId);
        await NotifyEntryChangedAsync(entryId);
    }

    void IDisposable.Dispose()
    {
    }
}
