using LcmCrdt.Changes.Entries;
using MiniLcm.SyncHelpers;

namespace LcmCrdt.Tests.MiniLcmTests;

public class ComplexFormComponentTests : ComplexFormComponentTestsBase
{
    private readonly MiniLcmApiFixture _fixture = new();

    protected override async Task<IMiniLcmApi> NewApi()
    {
        await _fixture.InitializeAsync();
        var api = _fixture.Api;
        return api;
    }

    public override async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await _fixture.DisposeAsync();
    }

    // -- Helpers --

    private async Task<Entry> CreateEntry(string headword)
    {
        return await Api.CreateEntry(new() { LexemeForm = { { "en", headword } } });
    }

    private async Task<Entry> GetEntry(Guid id)
    {
        return (await Api.GetEntry(id))!;
    }

    /// <summary>
    /// Creates a ComplexFormComponent with no CRDT entity ID set (MaybeId == null),
    /// as would come from LibLCM / FwData.
    /// </summary>
    private static ComplexFormComponent ComponentWithoutId(Guid complexFormEntryId, Guid componentEntryId)
    {
        return new ComplexFormComponent
        {
            ComplexFormEntryId = complexFormEntryId,
            ComponentEntryId = componentEntryId,
        };
    }

    // -- CRDT-specific tests documenting ComplexFormComponent ID quirks --
    //
    // ComplexFormComponent identity is unusual:
    //   - The CRDT entity ID is internal ([MiniLcmInternal]) and auto-generated.
    //   - Lookup is by properties (ComplexFormEntryId, ComponentEntryId, ComponentSenseId), not by ID.
    //   - The sync code must always call `component.Id = Guid.NewGuid()` before creating,
    //     because a property change in the diff causes a remove+add that would reuse the old ID.

    [Fact]
    public async Task ReusingEntityId_ForDifferentComponent_Throws()
    {
        // Harmony entity IDs must be unique across entities. Reusing one for a
        // different component relationship is invalid. The sync code prevents
        // this by calling `component.Id = Guid.NewGuid()` before every create.
        var entry2 = await CreateEntry("Component 2");

        var entityId = Guid.NewGuid();
        await _fixture.DataModel.AddChange(Guid.NewGuid(),
            new AddEntryComponentChange(entityId, 1, _complexFormEntryId, _componentEntryId));

        var act = () => _fixture.DataModel.AddChange(Guid.NewGuid(),
            new AddEntryComponentChange(entityId, 2, _complexFormEntryId, entry2.Id));
        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task ChangingPropertyId_ProducesNewEntityId()
    {
        // Components are identified by their properties, not entity ID.
        // Changing a property (e.g. ComponentEntryId) creates a distinct entity.
        var entry2 = await CreateEntry("Component 2");
        var complexForm = await GetEntry(_complexFormEntryId);
        var component = await GetEntry(_componentEntryId);

        var created1 = await Api.CreateComplexFormComponent(
            ComplexFormComponent.FromEntries(complexForm, component));
        var created2 = await Api.CreateComplexFormComponent(
            ComplexFormComponent.FromEntries(complexForm, entry2));

        created2.Id.Should().NotBe(created1.Id);
    }

    [Fact]
    public async Task IdempotentCreate_ReturnsExistingId_NotNewlyProvidedId()
    {
        // FromEntries() generates a new Guid each time. When a matching component
        // already exists, Create returns the existing entity — the caller's
        // newly generated ID is silently discarded.
        var complexForm = await GetEntry(_complexFormEntryId);
        var component = await GetEntry(_componentEntryId);

        var firstInput = ComplexFormComponent.FromEntries(complexForm, component);
        var created = await Api.CreateComplexFormComponent(firstInput);

        var secondInput = ComplexFormComponent.FromEntries(complexForm, component);
        secondInput.Id.Should().NotBe(firstInput.Id, "FromEntries generates a fresh Guid each time");

        var returnedExisting = await Api.CreateComplexFormComponent(secondInput);
        returnedExisting.Id.Should().Be(created.Id,
            "the existing entity's ID is returned, not the newly provided one");
    }

    [Fact]
    public async Task Create_WithBetweenComponentsLackingIds_PositionsCorrectly()
    {
        // Components from LibLCM don't carry CRDT entity IDs (MaybeId == null).
        // BetweenPosition items are resolved by property lookup, so positioning
        // works even when the between items have no IDs set.
        var complexForm = await GetEntry(_complexFormEntryId);
        var component = await GetEntry(_componentEntryId);
        var entry3 = await CreateEntry("Entry 3");
        var entry4 = await CreateEntry("Entry 4");

        var comp1 = await Api.CreateComplexFormComponent(
            ComplexFormComponent.FromEntries(complexForm, component));
        var comp2 = await Api.CreateComplexFormComponent(
            ComplexFormComponent.FromEntries(complexForm, entry3));

        // Build BetweenPosition items WITHOUT IDs — as LibLCM would provide
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
