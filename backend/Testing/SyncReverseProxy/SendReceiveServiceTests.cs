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
        _progress = new StringBuilderProgress() { ProgressIndicator = new NullProgressIndicator() };
        _progress.ShowVerbose = true;
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

    private static IEnumerable<string[]> hostsAndTypes => new[] { new[] { "http://localhost", "normal" }, new[] { "http://hgresumable", "resumable" } };
    private static string[] goodCredentials = new[] { "manager", "pass" };
    private static IEnumerable<string[]> badCredentials = new[] { new[] { "manager", "incorrect_pass" }, new[] { "invalid_user", "pass" } };

    public record SendReceiveTestData(string ProjectCode, string Host, string HostType, string Username, string Password, bool ShouldPass);

    public static IEnumerable<object[]> GetTestDataForSR(string projectCode)
    {
        foreach (var data in hostsAndTypes)
        {
            var host = data[0];
            var type = data[1];
            yield return new[] { new SendReceiveTestData(projectCode, host, type, goodCredentials[0], goodCredentials[1], true) };
            foreach (var credentials in badCredentials)
            {
                yield return new[] { new SendReceiveTestData(projectCode, host, type, credentials[0], credentials[1], false) };
            }
        }
    }

    [Theory]
    [MemberData(nameof(GetTestDataForSR), "sena-3")]
    // NOTE: resumable failing because can't read sena-3 repo, because owned by UID 82 (Alpine www-data) instead of 33 (Debian www-data)
    public void CloneProjectAndSendReceive(SendReceiveTestData data)
    {
        _srService = new SendReceiveService(_progress, data.Host);

        string projectDir = Path.Join(_basePath, data.HostType, data.ProjectCode);
        string fwdataFile = Path.Join(projectDir, $"{data.ProjectCode}.fwdata");
        string result = _srService.CloneProject(data.ProjectCode, projectDir, data.Username, data.Password);
        if (data.ShouldPass) {
            result.ShouldNotContain("abort");
            result.ShouldNotContain("error");
        fwdataFile.ShouldSatisfyAllConditions(
            () => new FileInfo(fwdataFile).Exists.ShouldBeTrue(),
            () => new FileInfo(fwdataFile).Length.ShouldBeGreaterThan(0)
        );
        long oldLength = new FileInfo(fwdataFile).Length;
        // Now do a Send/Receive which should get no changes
        // Running in same test because it's dependent on CloneProject happening first
        string result2 = _srService.SendReceiveProject(data.ProjectCode, projectDir, data.Username, data.Password);
        result2.ShouldNotContain("abort");
        result2.ShouldNotContain("error");
        result2.ShouldContain("no changes from others");
        fwdataFile.ShouldSatisfyAllConditions(
            () => new FileInfo(fwdataFile).Exists.ShouldBeTrue(),
            () => new FileInfo(fwdataFile).Length.ShouldBe(oldLength)
        );
        } else {
            result.ShouldContain("error");
        }
    }
}
