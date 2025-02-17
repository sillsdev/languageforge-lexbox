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
    IProjectIdentifier project) : IMiniLcmApi
{
    [BeaKona.AutoInterface(IncludeBaseInterfaces = true)]
    private readonly IMiniLcmApi _api = api;

    public void NotifyEntryChanged(Entry entry)
    {
        bus.PublishEntryChangedEvent(project, entry);
    }

    public async Task NotifyEntryChangedAsync(Guid entryId)
    {
        var entry = await _api.GetEntry(entryId);
        if (entry is null) return;
        bus.PublishEntryChangedEvent(project, entry);
    }

    // ********** Overrides go here **********

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

    void IDisposable.Dispose()
    {
    }
}
