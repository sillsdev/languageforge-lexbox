using LexBoxApi.Config;
using Microsoft.Extensions.Options;
using Moq;
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
    private string _basePath = Path.Join(Path.GetTempPath(), "SendReceiveTests");
    private SendReceiveService _srService;
    private IProgress _progress;

    public SendReceiveServiceTests()
    {
        _progress = new StringBuilderProgress();
        var _options = new Mock<IOptions<HgConfig>>();
        _options.Setup(opts => opts.Value).Returns(mockHgConfig);
        _srService = new SendReceiveService(_progress, _options.Object);
        CleanUpTempDir();
    }

    private void CleanUpTempDir()
    {
        var dirInfo = new DirectoryInfo(_basePath);
        try {
            dirInfo.Delete(true);
        } catch (DirectoryNotFoundException) {
            // It's fine if it didn't exist beforehand
        }
    }

    [Theory]
    [InlineData("3.0.1")]
    public async Task VerifyHgVersion(string expected)
    {
        string version = await _srService.VerifyHgVersion();
        version.ShouldContain(expected);
    }

    [Theory]
    [InlineData("sena-3")]
    public async Task CloneProjectAndSendReceive(string projectCode)
    {
        string projectDir = Path.Join(_basePath, projectCode);
        string fwdataFile = Path.Join(projectDir, $"{projectCode}.fwdata");
        string result = await _srService.CloneProject(projectCode, projectDir);
        // Console.WriteLine(result);
        result.ShouldNotContain("abort");
        result.ShouldNotContain("error");
        fwdataFile.ShouldSatisfyAllConditions(
            () => new FileInfo(fwdataFile).Exists.ShouldBeTrue(),
            () => new FileInfo(fwdataFile).Length.ShouldBeGreaterThan(0)
        );
        long oldLength = new FileInfo(fwdataFile).Length;
        // Now do a Send/Receive which should get no changes
        // Running in same test because it's dependent on CloneProject happening first
        string result2 = await _srService.SendReceiveProject(projectCode, projectDir);
        Console.WriteLine(result2);
        result.ShouldNotContain("abort");
        result.ShouldNotContain("error");
        fwdataFile.ShouldSatisfyAllConditions(
            () => new FileInfo(fwdataFile).Exists.ShouldBeTrue(),
            () => new FileInfo(fwdataFile).Length.ShouldBe(oldLength)
        );
    }
}
