using FwDataMiniLcmBridge.Api;
using FwDataMiniLcmBridge.Tests.Fixtures;
using MiniLcm;
using MiniLcm.Models;

namespace FwDataMiniLcmBridge.Tests;

[Collection(ProjectLoaderFixture.Name)]
public class PartOfSpeechTests(ProjectLoaderFixture fixture) : PartOfSpeechTestsBase
{
    protected override Task<IMiniLcmApi> NewApi()
    {
        return Task.FromResult<IMiniLcmApi>(fixture.NewProjectApi("part-of-speech-test", "en", "en"));
    }
}
