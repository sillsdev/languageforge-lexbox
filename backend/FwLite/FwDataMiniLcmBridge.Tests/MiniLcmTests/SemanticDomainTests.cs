using FwDataMiniLcmBridge.Tests.Fixtures;

namespace FwDataMiniLcmBridge.Tests.MiniLcmTests;

[Collection(ProjectLoaderFixture.Name)]
public class SemanticDomainTests(ProjectLoaderFixture fixture) : SemanticDomainTestsBase
{
    protected override Task<IMiniLcmApi> NewApi()
    {
        return Task.FromResult<IMiniLcmApi>(fixture.NewProjectApi("semantic-domain-test", "en", "en"));
    }
}
