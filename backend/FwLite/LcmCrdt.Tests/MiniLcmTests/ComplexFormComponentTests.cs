using LcmCrdt.Changes.Entries;
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

    // Simulates a component from LibLCM/FwData which has no CRDT entity ID (MaybeId == null).
    private static ComplexFormComponent ComponentWithoutId(Guid complexFormEntryId, Guid componentEntryId)
    {
        return new ComplexFormComponent
        {
            ComplexFormEntryId = complexFormEntryId,
            ComponentEntryId = componentEntryId,
        };
    }

    [Fact]
    public async Task ReusingEntityId_ForDifferentComponent_Throws()
    {
        // The sync code guards against this by calling `component.Id = Guid.NewGuid()`
        // before every create (see EntrySync.ComplexFormComponentsDiffApi.Add).
        var entry2 = await CreateEntry("Component 2");

        var entityId = Guid.NewGuid();
        await _fixture.DataModel.AddChange(Guid.NewGuid(),
            new AddEntryComponentChange(entityId, 1, _complexFormEntryId, _componentEntryId));

        var act = () => _fixture.DataModel.AddChange(Guid.NewGuid(),
            new AddEntryComponentChange(entityId, 2, _complexFormEntryId, entry2.Id));
        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task IdempotentCreate_ReturnsExistingId_NotNewlyProvidedId()
    {
        // ComplexFormComponent lookup is by properties, not entity ID.
        // When a matching component already exists, the caller's ID is discarded.
        var complexForm = await GetEntry(_complexFormEntryId);
        var component = await GetEntry(_componentEntryId);

        var firstInput = ComplexFormComponent.FromEntries(complexForm, component);
        var created = await Api.CreateComplexFormComponent(firstInput);

        var secondInput = ComplexFormComponent.FromEntries(complexForm, component);
        secondInput.Id.Should().NotBe(firstInput.Id, "FromEntries generates a fresh Guid each time");

        var returnedExisting = await Api.CreateComplexFormComponent(secondInput);
        returnedExisting.Id.Should().Be(created.Id);
    }

    [Fact]
    public async Task Create_WithBetweenComponentsLackingIds_PositionsCorrectly()
    {
        // Components from LibLCM don't carry CRDT entity IDs.
        // BetweenPosition items are resolved by property lookup instead.
        var complexForm = await GetEntry(_complexFormEntryId);
        var component = await GetEntry(_componentEntryId);
        var entry3 = await CreateEntry("Entry 3");
        var entry4 = await CreateEntry("Entry 4");

        var comp1 = await Api.CreateComplexFormComponent(
            ComplexFormComponent.FromEntries(complexForm, component));
        var comp2 = await Api.CreateComplexFormComponent(
            ComplexFormComponent.FromEntries(complexForm, entry3));

        var previous = ComponentWithoutId(_complexFormEntryId, _componentEntryId);
        var next = ComponentWithoutId(_complexFormEntryId, entry3.Id);
        previous.MaybeId.Should().BeNull("simulates a component from LibLCM with no CRDT ID");

        var comp3 = await Api.CreateComplexFormComponent(
            ComplexFormComponent.FromEntries(complexForm, entry4),
            new BetweenPosition<ComplexFormComponent>(previous, next));

        comp3.Order.Should().BeGreaterThan(comp1.Order)
            .And.BeLessThan(comp2.Order);
    }
}
