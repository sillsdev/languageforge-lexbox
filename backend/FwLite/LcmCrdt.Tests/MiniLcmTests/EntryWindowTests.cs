using Xunit.Abstractions;

namespace LcmCrdt.Tests.MiniLcmTests;

public class EntryWindowTests(ITestOutputHelper output) : EntryWindowTestsBase
{
    private readonly MiniLcmApiFixture _fixture = new();

    public override async Task InitializeAsync()
    {
        _fixture.LogTo(output);
        await _fixture.InitializeAsync();
        await base.InitializeAsync();
    }

    protected override Task<IMiniLcmApi> NewApi()
    {
        return Task.FromResult<IMiniLcmApi>(_fixture.Api);
    }

    public override async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await _fixture.DisposeAsync();
    }
}
