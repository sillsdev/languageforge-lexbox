using LexBoxApi.Config;
using Microsoft.Extensions.Options;
using Moq;
using Shouldly;
using SIL.Progress;
using Testing.Fixtures;
using Testing.Logging;
using Xunit.Abstractions;

namespace Testing.Services;

public class SendReceiveServiceTests
{
    private string _basePath = Path.Join(Path.GetTempPath(), "SendReceiveTests");
    private SendReceiveService _srService;
    private IProgress _progress;

    public SendReceiveServiceTests(ITestOutputHelper output)
    {
        _progress = new XunitStringBuilderProgress(output) { ProgressIndicator = new NullProgressIndicator() };
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

    private static IEnumerable<string[]> hostsAndTypes => new[] { new[] { "http://hg.localhost", "normal" }, new[] { "http://resumable.localhost", "resumable" } };
    private static string[] goodCredentials = new[] { "manager", "pass" };
    private static IEnumerable<string[]> badCredentials = new[] { new[] { "manager", "incorrect_pass" }, new[] { "invalid_user", "pass" }, new[] { "", "" } };

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
        long oldLength = 0;
        try {
            string result = _srService.CloneProject(data.ProjectCode, projectDir, data.Username, data.Password);
            if (data.ShouldPass) {
                result.ShouldNotContain("abort");
                result.ShouldNotContain("error");
                fwdataFile.ShouldSatisfyAllConditions(
                    () => new FileInfo(fwdataFile).Exists.ShouldBeTrue(),
                    () => new FileInfo(fwdataFile).Length.ShouldBeGreaterThan(0)
                );
                oldLength = new FileInfo(fwdataFile).Length;
            } else {
                result.ShouldMatch("abort: authorization failed|Server Response 'Unauthorized'");
            }
        } catch (Chorus.VcsDrivers.Mercurial.RepositoryAuthorizationException) {
            if (data.ShouldPass) {
                throw;
            } else {
                // This is a successful test, because the repo rejected the invalid password as it should
            };
        } catch (System.UnauthorizedAccessException) {
            if (data.ShouldPass) {
                throw;
            } else {
                // This is a successful test, because the repo rejected the invalid password as it should
            };
        }

        // Now do a Send/Receive which should get no changes
        // Running in same test because it's dependent on CloneProject happening first
        try {
            string result2 = _srService.SendReceiveProject(data.ProjectCode, projectDir, data.Username, data.Password);
            if (data.ShouldPass) {
                result2.ShouldNotContain("abort");
                result2.ShouldNotContain("error");
                result2.ShouldContain("no changes from others");
                fwdataFile.ShouldSatisfyAllConditions(
                    () => new FileInfo(fwdataFile).Exists.ShouldBeTrue(),
                    () => new FileInfo(fwdataFile).Length.ShouldBe(oldLength)
                );
            } else {
                result2.ShouldMatch("abort: authorization failed|Server Response 'Unauthorized'");
            }
        } catch (Chorus.VcsDrivers.Mercurial.RepositoryAuthorizationException) {
            if (data.ShouldPass) {
                throw;
            } else {
                // This is a successful test, because the repo rejected the invalid password as it should
            };
        } catch (System.UnauthorizedAccessException) {
            if (data.ShouldPass) {
                throw;
            } else {
                // This is a successful test, because the repo rejected the invalid password as it should
            };
        }
    }

    [Theory]
    [InlineData("non-existent-project", "manager", "pass")]
    [InlineData("non-existent-project", "admin", "pass")]
    // NOTE: resumable failing because can't read sena-3 repo, because owned by UID 82 (Alpine www-data) instead of 33 (Debian www-data)
    public void TestInvalidProject(string projectCode, string username, string password)
    {
        foreach (string[] data in hostsAndTypes)
        {
            string host = data[0];
            string type = data[1];
            if (type == "resumable") {
                continue;  // Skip resumable as Chorus just retries 4xx errors constantly (because it assumes they're caused by a flaky net connection)
            }
            var _srService = new SendReceiveService(_progress, host);
            string projectDir = Path.Join(_basePath, type, projectCode);
            string fwdataFile = Path.Join(projectDir, $"{projectCode}.fwdata");
            try {
                string result = _srService.CloneProject(projectCode, projectDir, username, password);
                throw new Exception("Clone should have thrown an exception but didn't; if we reach this point, this is a failed test");
            } catch (Chorus.VcsDrivers.Mercurial.ProjectLabelErrorException) {
                // Expected failure - this is what admin sees
            } catch (Chorus.VcsDrivers.Mercurial.RepositoryAuthorizationException) {
                // Expected failure - this is what manager user sees
            }
            fwdataFile.ShouldSatisfyAllConditions(
                () => new FileInfo(fwdataFile).Exists.ShouldBeFalse()
            );
        }
    }
}
