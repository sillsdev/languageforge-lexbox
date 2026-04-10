using FwDataMiniLcmBridge.Tests.Fixtures;

namespace FwDataMiniLcmBridge.Tests.MiniLcmTests;

[Collection(ProjectLoaderFixture.Name)]
public class MorphTypeTests(ProjectLoaderFixture fixture) : MorphTypeTestsBase
{
    protected override Task<IMiniLcmApi> NewApi()
    {
        return Task.FromResult<IMiniLcmApi>(fixture.NewProjectApi("morph-type-test", "en", "en"));
    }
}
