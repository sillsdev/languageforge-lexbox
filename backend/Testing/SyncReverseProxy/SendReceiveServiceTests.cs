using LexBoxApi.Config;
using Microsoft.Extensions.Options;
using Moq;
using Shouldly;
using SIL.Progress;
using Testing.Fixtures;

namespace Testing.Services;

public class SendReceiveServiceTests : IClassFixture<TestingServicesFixture>
{
    private string _basePath = Path.Join(Path.GetTempPath(), "SendReceiveTests");
    private SendReceiveService _srService;
    private IProgress _progress;

    public SendReceiveServiceTests()
    {
        _progress = new StringBuilderProgress();
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

    [Fact]
    public async Task VerifyHgWorking()
    {
        _srService = new SendReceiveService(_progress);
        string version = await _srService.GetHgVersion();
        version.ShouldStartWith("Mercurial Distributed SCM");
    }

    [Theory]
    [InlineData("sena-3", "http://localhost:8088/hg", "normal")]
    [InlineData("sena-3", "http://hgresumable:8034/", "resumable")]
    // NOTE: resumable failing because can't read sena-3 repo, because owned by UID 82 (Alpine www-data) instead of 33 (Debian www-data)
    public void CloneProjectAndSendReceive(string projectCode, string repoBaseUrl, string testName)
    {
        _srService = new SendReceiveService(_progress, repoBaseUrl);

        string projectDir = Path.Join(_basePath, testName, projectCode);
        string fwdataFile = Path.Join(projectDir, $"{projectCode}.fwdata");
        string result = _srService.CloneProject(projectCode, projectDir);
        result.ShouldNotContain("abort");
        result.ShouldNotContain("error");
        fwdataFile.ShouldSatisfyAllConditions(
            () => new FileInfo(fwdataFile).Exists.ShouldBeTrue(),
            () => new FileInfo(fwdataFile).Length.ShouldBeGreaterThan(0)
        );
        long oldLength = new FileInfo(fwdataFile).Length;
        // Now do a Send/Receive which should get no changes
        // Running in same test because it's dependent on CloneProject happening first
        string result2 = _srService.SendReceiveProject(projectCode, projectDir);
        result.ShouldNotContain("abort");
        result.ShouldNotContain("error");
        fwdataFile.ShouldSatisfyAllConditions(
            () => new FileInfo(fwdataFile).Exists.ShouldBeTrue(),
            () => new FileInfo(fwdataFile).Length.ShouldBe(oldLength)
        );
    }
}
