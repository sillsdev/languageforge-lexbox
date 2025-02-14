using FwLiteShared.Events;
using MiniLcm;
using MiniLcm.Models;
using MiniLcm.SyncHelpers;

namespace FwLiteShared.Services;

public class MiniLcmApiNotifyWrapperFactory()
{
    public IMiniLcmApi Create(IMiniLcmApi api, ProjectEventBus bus, IProjectIdentifier project, Func<Guid, Task<Entry?>> entryLookup)
    {
        return new MiniLcmApiNotifyWrapper(api, bus, project, entryLookup).AsIMiniLcmApi();
    }
}

public partial class MiniLcmApiNotifyWrapper(IMiniLcmApi api, ProjectEventBus bus, IProjectIdentifier project, Func<Guid, Task<Entry?>> entryLookup) : IMiniLcmReadApi, IMiniLcmWriteApi, IDisposable
{
    [BeaKona.AutoInterface]
    private readonly IMiniLcmReadApi wrappedReadApi = api;

    [BeaKona.AutoInterface]
    private readonly IMiniLcmWriteApi wrappedWriteApi = api;

    public IMiniLcmApi AsIMiniLcmApi()
    {
        // AutoInterface doesn't know how to handle IMiniLcmApi so we had to specify the component interfaces separately, but this class is intended to function as an IMiniLcmApi implementation
        return (IMiniLcmApi)this;
    }

    public void NotifyEntryChanged(Entry entry)
    {
        bus.PublishEntryChangedEvent(project, entry);
    }

    public async Task NotifyEntryChangedAsync(Guid entryId)
    {
        // TODO: Passing in an entry lookup method allows this wrapper to remain ignorant... but should I just call the wrapped read API instead?
        var entry = await entryLookup(entryId);
        // var maybeThisIsSimpler = await wrappedReadApi.GetEntry(entryId);
        if (entry is null) return;
        bus.PublishEntryChangedEvent(project, entry);
    }

    // ********** Overrides go here **********

    async Task<ComplexFormComponent> IMiniLcmWriteApi.CreateComplexFormComponent(ComplexFormComponent complexFormComponent, BetweenPosition<ComplexFormComponent>? position)
    {
        var result = await wrappedWriteApi.CreateComplexFormComponent(complexFormComponent, position);
        await NotifyEntryChangedAsync(result.ComplexFormEntryId);
        await NotifyEntryChangedAsync(result.ComponentEntryId);
        return result;
    }

    async Task IMiniLcmWriteApi.DeleteComplexFormComponent(ComplexFormComponent complexFormComponent)
    {
        await wrappedWriteApi.DeleteComplexFormComponent(complexFormComponent);
        await NotifyEntryChangedAsync(complexFormComponent.ComplexFormEntryId);
        await NotifyEntryChangedAsync(complexFormComponent.ComponentEntryId);
    }

    public void Dispose()
    {
    }
}
