using LexBoxApi.Config;
using Shouldly;
using SIL.Progress;
using Testing.Fixtures;

namespace Testing.Services;

public class SendReceiveServiceTests : IClassFixture<TestingServicesFixture>
{
    private HgConfig mockHgConfig = new HgConfig {  // For when we put an IOptions<HgConfig> back into SendReceiveService
        RepoPath = "../../hgweb/repos",
        HgWebUrl = "http://localhost:8088"
    };
    private SendReceiveService _srService;
    private IProgress _progress;

    public SendReceiveServiceTests()
    {
        _progress = new StringBuilderProgress();
        _srService = new SendReceiveService(_progress);
    }

    [Theory]
    [InlineData("3.0.1")]
    public async Task VerifyHgVersion(string expected)
    {
        string version = await _srService.VerifyHgVersion();
        version.ShouldContain(expected);
    }
}
