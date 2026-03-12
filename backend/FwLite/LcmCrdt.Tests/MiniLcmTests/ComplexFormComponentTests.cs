using MiniLcm.SyncHelpers;

namespace LcmCrdt.Tests.MiniLcmTests;

public class ComplexFormComponentTests : ComplexFormComponentTestsBase
{
    private readonly MiniLcmApiFixture _fixture = new();

    protected override async Task<IMiniLcmApi> NewApi()
    {
        await _fixture.InitializeAsync();
        return _fixture.Api;
    }

    public override async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await _fixture.DisposeAsync();
    }

    private async Task<Entry> CreateEntry(string headword)
    {
        return await Api.CreateEntry(new() { LexemeForm = { { "en", headword } } });
    }

    private async Task<Entry> GetEntry(Guid id)
    {
        return (await Api.GetEntry(id))!;
    }

    [Fact]
    public async Task ReusingEntityId_AfterPropertyChange_SilentlyReturnsOriginalEntity()
    {
        // When the sync diff detects a property change (e.g. ComponentEntryId), it treats
        // it as remove + add. The "add" still carries the original entity ID.
        // The sync code guards against this with `after.Id = Guid.NewGuid()` before creating
        // (see EntrySync.ComplexFormsDiffApi.Add / ComplexFormComponentsDiffApi.Add).
        //
        // Without that guard, Harmony silently no-ops the second create (entity ID already
        // exists), and FindComplexFormComponent returns the ORIGINAL entity — not the one
        // with updated properties.
        var complexFormEntry = await GetEntry(_complexFormEntryId);
        var originalComponentEntry = await GetEntry(_componentEntryId);
        var newComponentEntry = await CreateEntry("New Component");

        var input = ComplexFormComponent.FromEntries(complexFormEntry, originalComponentEntry);
        var firstResult = await Api.CreateComplexFormComponent(input);

        // Mutate the property without resetting the entity ID — the bug the sync code prevents.
        input.ComponentEntryId = newComponentEntry.Id;
        var secondResult = await Api.CreateComplexFormComponent(input);

        // Both calls return the same entity — the second create was a no-op.
        secondResult.Id.Should().Be(firstResult.Id);
        secondResult.ComponentEntryId.Should().Be(originalComponentEntry.Id,
            "Harmony ignored the second create; the original entity was returned unchanged");
    }

    [Fact]
    public async Task Create_WithoutPresetId_AssignsNewEntityId()
    {
        // Components from LibLCM/FwData have no CRDT entity ID (MaybeId == null).
        // CreateComplexFormComponent generates one via AddEntryComponentChange.
        var input = new ComplexFormComponent
        {
            ComplexFormEntryId = _complexFormEntryId,
            ComponentEntryId = _componentEntryId,
        };
        input.MaybeId.Should().BeNull();

        var created = await Api.CreateComplexFormComponent(input);

        created.MaybeId.Should().NotBeNull();
        created.ComplexFormEntryId.Should().Be(_complexFormEntryId);
        created.ComponentEntryId.Should().Be(_componentEntryId);
    }

    [Fact]
    public async Task Create_WithBetweenComponentsLackingIds_PositionsCorrectly()
    {
        // Components from LibLCM don't carry CRDT entity IDs.
        // BetweenPosition items are resolved via property lookup
        // (see CrdtMiniLcmApi.CreateComplexFormComponent / MoveComplexFormComponent).
        var complexFormEntry = await GetEntry(_complexFormEntryId);
        var componentA = await GetEntry(_componentEntryId);
        var componentB = await CreateEntry("Component B");
        var componentC = await CreateEntry("Component C");

        var createdA = await Api.CreateComplexFormComponent(
            ComplexFormComponent.FromEntries(complexFormEntry, componentA));
        var createdB = await Api.CreateComplexFormComponent(
            ComplexFormComponent.FromEntries(complexFormEntry, componentB));

        // BetweenPosition anchors without IDs — as they'd arrive from LibLCM.
        var anchorBefore = new ComplexFormComponent
        {
            ComplexFormEntryId = _complexFormEntryId,
            ComponentEntryId = componentA.Id,
        };
        var anchorAfter = new ComplexFormComponent
        {
            ComplexFormEntryId = _complexFormEntryId,
            ComponentEntryId = componentB.Id,
        };
        anchorBefore.MaybeId.Should().BeNull();

        var insertedBetween = await Api.CreateComplexFormComponent(
            ComplexFormComponent.FromEntries(complexFormEntry, componentC),
            new BetweenPosition<ComplexFormComponent>(anchorBefore, anchorAfter));

        insertedBetween.Order.Should().BeGreaterThan(createdA.Order)
            .And.BeLessThan(createdB.Order);
    }
}
