namespace LcmCrdt.Tests.MiniLcmTests;

public class BasicApiTests(MiniLcmApiFixture fixture): BasicApiTestsBase, IClassFixture<MiniLcmApiFixture>
{
    protected override Task<IMiniLcmApi> NewApi()
    {
        return Task.FromResult<IMiniLcmApi>(fixture.Api);
    }
}
