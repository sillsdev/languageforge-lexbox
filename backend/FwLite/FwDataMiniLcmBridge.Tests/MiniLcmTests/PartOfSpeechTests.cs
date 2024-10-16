using FwDataMiniLcmBridge.Tests.Fixtures;

namespace FwDataMiniLcmBridge.Tests.MiniLcmTests;

[Collection(ProjectLoaderFixture.Name)]
public class PartOfSpeechTests(ProjectLoaderFixture fixture) : PartOfSpeechTestsBase
{
    protected override Task<IMiniLcmApi> NewApi()
    {
        return Task.FromResult<IMiniLcmApi>(fixture.NewProjectApi("part-of-speech-test", "en", "en"));
    }
}
