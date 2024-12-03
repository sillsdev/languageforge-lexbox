using FwDataMiniLcmBridge.Tests.Fixtures;

namespace FwDataMiniLcmBridge.Tests.MiniLcmTests;

[Collection(ProjectLoaderFixture.Name)]
public class SortingTests(ProjectLoaderFixture fixture) : SortingTestsBase
{
    protected override Task<IMiniLcmApi> NewApi()
    {
        return Task.FromResult<IMiniLcmApi>(fixture.NewProjectApi("sorting-test", "en", "en"));
    }
}
