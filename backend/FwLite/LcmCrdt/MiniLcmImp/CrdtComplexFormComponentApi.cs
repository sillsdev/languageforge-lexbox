using LcmCrdt.Data;
using LcmCrdt.Harmony;
using MiniLcm.Exceptions;
using MiniLcm.SyncHelpers;
using SIL.Harmony.Changes;

namespace LcmCrdt.MiniLcmImp;

public class CrdtComplexFormComponentApi(MiniLcmRepositoryFactory repoFactory, HarmonyChangeWriter harmonyChangeWriter)
{
    public async Task SubmitCreateComplexFormComponent(ComplexFormComponent complexFormComponent, BetweenPosition<ComplexFormComponent>? between = null)
    {
        await using var repo = await repoFactory.CreateRepoAsync();
        var existing = await repo.FindComplexFormComponent(complexFormComponent);
        if (existing is null)
        {
            var betweenIds = between is null ? null : await between.MapAsync(async c => (await repo.FindComplexFormComponent(c))?.Id);
            // Always generate a new entity ID — the caller's ID is never used.
            // This aligns with FwData (which ignores the ID entirely) and prevents
            // Harmony duplicate-ID pitfalls during sync.
            complexFormComponent.Id = Guid.NewGuid();
            var addEntryComponentChange = await repo.CreateComplexFormComponentChange(complexFormComponent, betweenIds);
            await harmonyChangeWriter.AddChange(addEntryComponentChange);
            return;
        }

        // The orderable diff sends (null, null) for singletons; skip the move so
        // revisits in one sync don't bump Order via PickOrder.
        if (between is { Previous: not null } or { Next: not null })
        {
            await MoveComplexFormComponent(existing, between);
        }
    }

    public async Task<ComplexFormComponent> CreateComplexFormComponent(ComplexFormComponent complexFormComponent, BetweenPosition<ComplexFormComponent>? between = null)
    {
        await SubmitCreateComplexFormComponent(complexFormComponent, between);
        await using var repo = await repoFactory.CreateRepoAsync();
        return await repo.FindComplexFormComponent(complexFormComponent) ?? throw NotFoundException.ForType<ComplexFormComponent>(complexFormComponent.ComplexFormEntryId);
    }

    public async Task MoveComplexFormComponent(ComplexFormComponent component, BetweenPosition<ComplexFormComponent> between)
    {
        await MoveComplexFormComponent(component, between, tolerateMissing: false);
    }

    public async Task SubmitMoveComplexFormComponent(ComplexFormComponent component, BetweenPosition<ComplexFormComponent> between)
    {
        await MoveComplexFormComponent(component, between, tolerateMissing: true);
    }

    private async Task MoveComplexFormComponent(ComplexFormComponent component, BetweenPosition<ComplexFormComponent> between, bool tolerateMissing)
    {
        await using var repo = await repoFactory.CreateRepoAsync();
        // FwData components carry no stable Id, so the move target is resolved by its references rather than MaybeId.
        var id = component.MaybeId ?? (await repo.FindComplexFormComponent(component))?.Id;
        if (id is null)
        {
            if (tolerateMissing) return; // we can't submit the change, because we don't have an ID to refer to
            throw NotFoundException.ForType<ComplexFormComponent>("missing ID");
        }
        var betweenIds = await between.MapAsync(async c => (await repo.FindComplexFormComponent(c))?.Id);
        var order = await OrderPicker.PickOrder(repo.ComplexFormComponents.Where(s => s.ComplexFormEntryId == component.ComplexFormEntryId), betweenIds);
        await harmonyChangeWriter.AddChange(new Changes.SetOrderChange<ComplexFormComponent>(id.Value, order));
    }

    public async Task DeleteComplexFormComponent(ComplexFormComponent complexFormComponent)
    {
        await using var repo = await repoFactory.CreateRepoAsync();
        var existing = await repo.FindComplexFormComponent(complexFormComponent);
        if (existing is null) return;
        await harmonyChangeWriter.AddChange(new DeleteChange<ComplexFormComponent>(existing.Id));
    }
}
