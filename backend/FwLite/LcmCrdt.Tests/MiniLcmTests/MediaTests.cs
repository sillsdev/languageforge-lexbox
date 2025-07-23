using Xunit.Abstractions;

namespace LcmCrdt.Tests.MiniLcmTests;

public class MediaTests(ITestOutputHelper output) : MediaTestsBase(output)
{
    private readonly MiniLcmApiFixture _fixture = new();

    protected override async Task<IMiniLcmApi> NewApi()
    {
        await _fixture.InitializeAsync("media-test");
        return _fixture.Api;
    }

    public override async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await _fixture.DisposeAsync();
    }
}
