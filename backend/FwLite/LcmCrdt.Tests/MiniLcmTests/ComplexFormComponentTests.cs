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
    public async Task Create_AlwaysAssignsNewEntityId()
    {
        // The provided entity ID is never used — a new one is always generated.
        // This matches FwData behavior and prevents Harmony duplicate-ID pitfalls
        // (see EntrySync.ComplexFormsDiffApi.Add for context).
        var input = ComplexFormComponent.FromEntries(
            await GetEntry(_complexFormEntryId),
            await GetEntry(_componentEntryId));
        var providedId = input.Id;

        var created = await Api.CreateComplexFormComponent(input);

        created.MaybeId.Should().NotBeNull();
        created.Id.Should().NotBe(providedId);
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
