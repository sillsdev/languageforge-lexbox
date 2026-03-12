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

    private async Task<Entry> GetEntry(Guid id)
    {
        return (await Api.GetEntry(id))!;
    }

    [Fact]
    public async Task Create_WithExistingEntityId_Throws()
    {
        // ComplexFormComponent.Id is internal — callers should never provide one that
        // matches an existing entity. If it does, it means they're reusing an already-created
        // object, which would silently no-op in Harmony (duplicate entity IDs are ignored).
        var created = await Api.CreateComplexFormComponent(
            ComplexFormComponent.FromEntries(
                await GetEntry(_complexFormEntryId),
                await GetEntry(_componentEntryId)));

        var act = () => Api.CreateComplexFormComponent(created);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Create_ChangingProperty_ProducesNewEntityId()
    {
        // When the sync diff detects a property change (e.g. ComponentEntryId), it does
        // remove + add. The "add" reuses the same input object with the old entity ID.
        // CreateComplexFormComponent always generates a new entity ID, so both components
        // get distinct Harmony entities and nothing is silently lost.
        var newComponentEntry = await Api.CreateEntry(new()
        {
            LexemeForm = { { "en", "New Component" } }
        });

        var input = ComplexFormComponent.FromEntries(
            await GetEntry(_complexFormEntryId),
            await GetEntry(_componentEntryId));
        var first = await Api.CreateComplexFormComponent(input);

        input.ComponentEntryId = newComponentEntry.Id;
        var second = await Api.CreateComplexFormComponent(input);

        second.Id.Should().NotBe(first.Id);
    }

    [Fact]
    public async Task Create_AlwaysAssignsNewEntityId()
    {
        // A "normal" create also replaces the provided ID — the caller's entity ID is
        // never used. This matches FwData behavior (which ignores the ID entirely) and
        // prevents Harmony duplicate-ID pitfalls during sync.
        var input = ComplexFormComponent.FromEntries(
            await GetEntry(_complexFormEntryId),
            await GetEntry(_componentEntryId));
        var providedId = input.Id;

        var created = await Api.CreateComplexFormComponent(input);

        created.MaybeId.Should().NotBeNull();
        created.Id.Should().NotBe(providedId);
    }

    [Fact]
    public async Task Create_DuplicateByProperties_WithDifferentEntityId_ReturnsExisting()
    {
        // Two calls with the same properties but different entity IDs (e.g. from separate
        // FromEntries calls) are idempotent — the second returns the existing component.
        // This is the normal case: callers don't control entity IDs.
        var first = await Api.CreateComplexFormComponent(
            ComplexFormComponent.FromEntries(
                await GetEntry(_complexFormEntryId),
                await GetEntry(_componentEntryId)));

        var second = await Api.CreateComplexFormComponent(
            ComplexFormComponent.FromEntries(
                await GetEntry(_complexFormEntryId),
                await GetEntry(_componentEntryId)));

        second.Id.Should().Be(first.Id);
    }
}
