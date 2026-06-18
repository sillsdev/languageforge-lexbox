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

    // CRDT-only: a never-synced project can lack a main publication; a real FwData project always has one.
    [Fact]
    public async Task CreateEntry_DoesNothingWhenNoMainPublicationExists()
    {
        await Api.CreatePublication(new Publication { Id = Guid.NewGuid(), Name = { { "en", "Not main" } } });

        var entry = await Api.CreateEntry(new Entry { LexemeForm = { { "en", "test" } }, PublishIn = [] }, CreateEntryOptions.WithMainPublication);

        entry.PublishIn.Should().BeEmpty();
    }
}
