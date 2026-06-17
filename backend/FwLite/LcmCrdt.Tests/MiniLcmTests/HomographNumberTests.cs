namespace LcmCrdt.Tests.MiniLcmTests;

// CRDT homograph numbering is best-effort.
// Any explicit/non-zero value is taken as-is,
// so we respect numbers synced in from FwData. FwData is the authority.
// "Broken" numbers are corrected after 2 syncs.
public class HomographNumberTests : HomographNumberTestsBase
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
    public async Task CreateEntry_RespectsExplicitHomographNumber()
    {
        var entry = await Api.CreateEntry(new() { LexemeForm = { { "en", "explicit" } }, HomographNumber = 5 });
        entry.HomographNumber.Should().Be(5);
    }

    [Fact]
    public async Task UpdateEntry_RespectsExplicitHomographNumber()
    {
        var before = await Api.CreateEntry(new() { LexemeForm = { { "en", "u" } }, HomographNumber = 1 });
        var updated = await Api.UpdateEntry(before, before with { HomographNumber = 7 });
        updated.HomographNumber.Should().Be(7);
    }
}
