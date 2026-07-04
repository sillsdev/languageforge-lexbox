namespace LcmCrdt.Tests.MiniLcmTests;

public class VariantTests : VariantTestsBase
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

    [Fact]
    public async Task Create_AlwaysAssignsNewEntityId()
    {
        // The caller's entity ID is never used. This matches FwData behavior (which ignores
        // the ID entirely) and prevents Harmony duplicate-ID pitfalls during sync.
        var input = Variant.FromEntries(
            (await Api.GetEntry(_variantEntryId))!,
            (await Api.GetEntry(_mainEntryId))!);
        var providedId = input.Id;

        var created = await Api.CreateVariant(input);

        created.MaybeId.Should().NotBeNull();
        created.Id.Should().NotBe(providedId);
    }

    [Fact]
    public async Task Create_ChangingProperty_ProducesNewEntityId()
    {
        // When the sync diff detects a property change (e.g. MainEntryId), it does
        // remove + add. The "add" reuses the same input object with the old entity ID.
        var newMainEntry = await Api.CreateEntry(new()
        {
            LexemeForm = { { "en", "New Main" } }
        });

        var input = Variant.FromEntries(
            (await Api.GetEntry(_variantEntryId))!,
            (await Api.GetEntry(_mainEntryId))!);
        var first = await Api.CreateVariant(input);

        input.MainEntryId = newMainEntry.Id;
        var second = await Api.CreateVariant(input);

        second.Id.Should().NotBe(first.Id);
    }
}
