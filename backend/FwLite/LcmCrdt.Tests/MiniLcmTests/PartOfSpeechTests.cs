namespace LcmCrdt.Tests.MiniLcmTests;

public class PartOfSpeechTests : PartOfSpeechTestsBase
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
}
