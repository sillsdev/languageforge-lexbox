namespace LcmCrdt.Tests.MiniLcmTests;

// CRDT respects whatever HomographNumber it's given on both create and update — the
// CRDT layer doesn't try to compute a valid sequence. FwData is the authority that
// produces and corrects the numbering; a sync round-trip reconciles CRDT with that.
public class HomographNumberTests : MiniLcmTestBase
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
