using FwDataMiniLcmBridge.Tests.Fixtures;
using MiniLcm;
using MiniLcm.Tests;

namespace FwDataMiniLcmBridge.Tests;

[Collection(ProjectLoaderFixture.Name)]
public class WritingSystemTests(ProjectLoaderFixture fixture) : WritingSystemTestsBase
{
    protected override Task<IMiniLcmApi> NewApi()
    {
        return Task.FromResult<IMiniLcmApi>(fixture.NewProjectApi("ws-test", "en", "en"));
    }
}
