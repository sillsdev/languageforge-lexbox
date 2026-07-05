using FwDataMiniLcmBridge.Tests.Fixtures;

namespace FwDataMiniLcmBridge.Tests.MiniLcmTests;

[Collection(ProjectLoaderFixture.Name)]
public class EntrySenseRowTests(ProjectLoaderFixture fixture) : EntrySenseRowTestsBase
{
    protected override Task<IMiniLcmApi> NewApi()
    {
        return Task.FromResult<IMiniLcmApi>(fixture.NewProjectApi("entry-sense-row-test", "en", "en"));
    }
}
