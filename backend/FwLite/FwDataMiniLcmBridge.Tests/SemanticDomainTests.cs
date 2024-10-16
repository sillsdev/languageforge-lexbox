using FwDataMiniLcmBridge.Tests.Fixtures;
using MiniLcm;
using MiniLcm.Tests;

namespace FwDataMiniLcmBridge.Tests;

[Collection(ProjectLoaderFixture.Name)]
public class SemanticDomainTests(ProjectLoaderFixture fixture) : SemanticDomainTestsBase
{
    protected override Task<IMiniLcmApi> NewApi()
    {
        return Task.FromResult<IMiniLcmApi>(fixture.NewProjectApi("semantic-domain-test", "en", "en"));
    }
}
