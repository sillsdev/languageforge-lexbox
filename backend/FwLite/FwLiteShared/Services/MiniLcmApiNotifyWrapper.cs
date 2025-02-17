using FwLiteShared.Events;
using MiniLcm;
using MiniLcm.Models;
using MiniLcm.SyncHelpers;

namespace FwLiteShared.Services;

public class MiniLcmApiNotifyWrapperFactory
{
    public IMiniLcmApi Create(IMiniLcmApi api, ProjectEventBus bus, IProjectIdentifier project)
    {
        return new MiniLcmApiNotifyWrapper(api, bus, project);
    }
}

public partial class MiniLcmApiNotifyWrapper(
    IMiniLcmApi api,
    ProjectEventBus bus,
    IProjectIdentifier project) : IMiniLcmApi, IMiniLcmReadApi, IMiniLcmWriteApi
{
    [BeaKona.AutoInterface]
    private readonly IMiniLcmReadApi _wrappedReadApi = api;

    [BeaKona.AutoInterface]
    private readonly IMiniLcmWriteApi _wrappedWriteApi = api;

    public void NotifyEntryChanged(Entry entry)
    {
        bus.PublishEntryChangedEvent(project, entry);
    }

    public async Task NotifyEntryChangedAsync(Guid entryId)
    {
        var entry = await _wrappedReadApi.GetEntry(entryId);
        if (entry is null) return;
        bus.PublishEntryChangedEvent(project, entry);
    }

    // ********** Overrides go here **********

    async Task<ComplexFormComponent> IMiniLcmWriteApi.CreateComplexFormComponent(ComplexFormComponent complexFormComponent, BetweenPosition<ComplexFormComponent>? position)
    {
        var result = await _wrappedWriteApi.CreateComplexFormComponent(complexFormComponent, position);
        await NotifyEntryChangedAsync(result.ComplexFormEntryId);
        await NotifyEntryChangedAsync(result.ComponentEntryId);
        return result;
    }

    async Task IMiniLcmWriteApi.DeleteComplexFormComponent(ComplexFormComponent complexFormComponent)
    {
        await _wrappedWriteApi.DeleteComplexFormComponent(complexFormComponent);
        await NotifyEntryChangedAsync(complexFormComponent.ComplexFormEntryId);
        await NotifyEntryChangedAsync(complexFormComponent.ComponentEntryId);
    }

    public void Dispose()
    {
    }
}
