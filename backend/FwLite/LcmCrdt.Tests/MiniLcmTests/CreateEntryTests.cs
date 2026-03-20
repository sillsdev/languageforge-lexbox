namespace LcmCrdt.Tests.MiniLcmTests;

public class CreateEntryTests : CreateEntryTestsBase
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

    // This is CRDT only, because:
    // - CRDT should respect the homograph numbers synced in from FwData
    // - FwData should NOT respect the homograph numbers from CRDT (they can easily get out of sync and users can't pick them anyway)
    [Fact]
    public async Task CreateEntry_RespectsExplicitHomographNumber()
    {
        var entry1 = await Api.CreateEntry(new() { LexemeForm = { { "en", "explicit" } }, HomographNumber = 5 });
        entry1.HomographNumber.Should().Be(5, "explicit HomographNumber should be preserved");
    }
}
