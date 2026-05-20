namespace LcmCrdt.Tests.MiniLcmTests;

// CRDT's homograph handling: an explicit non-zero HomographNumber is taken as-is on both
// create and update. When none is supplied, CreateEntry assigns one based on the entries
// that share the headword — the tests below pin down the observable progression.
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
    public async Task CreateEntry_NoHomographNumberSpecified_NumbersAreAssignedAsHomographsAppear()
    {
        // A lone entry has no homographs and stays at 0.
        var a = await Api.CreateEntry(new() { LexemeForm = { { "en", "hgr" } } });
        (await Api.GetEntry(a.Id))!.HomographNumber.Should().Be(0);

        // Adding a second matching entry promotes the original to 1; the newcomer becomes 2.
        var b = await Api.CreateEntry(new() { LexemeForm = { { "en", "hgr" } } });
        (await Api.GetEntry(a.Id))!.HomographNumber.Should().Be(1);
        (await Api.GetEntry(b.Id))!.HomographNumber.Should().Be(2);

        // Subsequent entries pick up where the current max leaves off.
        var c = await Api.CreateEntry(new() { LexemeForm = { { "en", "hgr" } } });
        (await Api.GetEntry(c.Id))!.HomographNumber.Should().Be(3);
    }

    [Fact]
    public async Task UpdateEntry_RespectsExplicitHomographNumber()
    {
        var before = await Api.CreateEntry(new() { LexemeForm = { { "en", "u" } }, HomographNumber = 1 });
        var updated = await Api.UpdateEntry(before, before with { HomographNumber = 7 });
        updated.HomographNumber.Should().Be(7);
    }
}
