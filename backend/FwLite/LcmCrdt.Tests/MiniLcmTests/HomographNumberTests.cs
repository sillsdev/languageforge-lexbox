namespace LcmCrdt.Tests.MiniLcmTests;

// CRDT homograph numbering is best-effort: any explicit non-zero value is taken as-is on
// both create and update, even if it leaves the cluster in a bad shape. FwData is the
// authority on numbering, so a sync round-trip is what eventually heals broken numbers on
// the CRDT side. The shared HN=0 auto-assignment scenario lives in HomographNumberTestsBase.
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
