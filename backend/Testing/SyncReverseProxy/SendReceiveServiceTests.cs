using Chorus.VcsDrivers.Mercurial;
using Microsoft.Extensions.Hosting;
using Shouldly;
using SIL.Progress;
using Testing.Logging;
using Testing.Services;
using Xunit.Abstractions;

namespace Testing.SyncReverseProxy;

[Trait("Category", "Integration")]
public class SendReceiveServiceTests
{
    private readonly ITestOutputHelper _output;
    private string _basePath = Path.Join(Path.GetTempPath(), "SendReceiveTests");

    public SendReceiveServiceTests(ITestOutputHelper output)
    {
        _output = output;
        CleanUpTempDir();
    }

    private void CleanUpTempDir()
    {
        var dirInfo = new DirectoryInfo(_basePath);
        try
        {
            dirInfo.Delete(true);
        }
        catch (DirectoryNotFoundException)
        {
            // It's fine if it didn't exist beforehand
        }
    }

    [Fact]
    public async Task VerifyHgWorking()
    {
        var srService = new SendReceiveService(_progress);
        string version = await srService.GetHgVersion();
        version.ShouldStartWith("Mercurial Distributed SCM");
    }

    private static readonly (string host, string type)[] HostsAndTypes = {
        (host: $"http://{TestingEnvironmentVariables.StandardHgHostname}", type: "normal"),
        (host: $"http://{TestingEnvironmentVariables.ResumableHgHostname}", type: "resumable")
    };

    private static readonly (string user, string pass, bool valid)[] Credentials =
    {
        (user: "manager", pass: "pass", valid: true),
        (user: "manager", pass: "incorrect_pass", valid: false),
        (user: "invalid_user", pass: "pass", valid: false),
        (user: "", pass: "", valid: false),
    };

    public record SendReceiveTestData(string ProjectCode,
        string Host,
        string HostType,
        string Username,
        string Password,
        bool ShouldPass);

    public static IEnumerable<object[]> GetTestDataForSR(string projectCode)
    {
        foreach (var (host, type) in HostsAndTypes)
        {
            foreach (var (user, pass, valid) in Credentials)
            {
                yield return new[]
                {
                    new SendReceiveTestData(projectCode, host, type, user, pass, valid)
                };
            }
        }
    }

    [Fact(
        Skip = "Just for testing, comment out to run"
        )]
    public void CloneForDev()
    {
        CloneProjectAndSendReceive(GetTestDataForSR("sena-3").First().OfType<SendReceiveTestData>().First());
    }

    [Theory]
    [MemberData(nameof(GetTestDataForSR), "sena-3")]
    public void CloneProjectAndSendReceive(SendReceiveTestData data)
    {
        var srService = new SendReceiveService(_output, data.Host);

        string projectDir = Path.Join(_basePath, data.HostType, data.ProjectCode);
        string fwdataFile = Path.Join(projectDir, $"{data.ProjectCode}.fwdata");
        long oldLength = 0;
        var fileInfo = new FileInfo(fwdataFile);
        try
        {
            string result = srService.CloneProject(data.ProjectCode, projectDir, data.Username, data.Password);
            if (data.ShouldPass)
            {
                result.ShouldNotContain("abort");
                result.ShouldNotContain("error");
                fileInfo.Directory?.Exists.ShouldBeTrue("directory " + fileInfo.DirectoryName + " not found. Clone response: " + result);
                fileInfo.Directory!.EnumerateFiles().ShouldContain(child => child.Name == fileInfo.Name);
                fwdataFile.ShouldSatisfyAllConditions(
                    () => fileInfo.Exists.ShouldBeTrue(),
                    () => fileInfo.Length.ShouldBeGreaterThan(0)
                );
                oldLength = fileInfo.Length;
            }
            else
            {
                result.ShouldMatch("abort: authorization failed|Server Response 'Unauthorized'");
            }
        }
        catch (RepositoryAuthorizationException) when (!data.ShouldPass)
        {
            // This is a successful test, because the repo rejected the invalid password as it should
        }
        catch (UnauthorizedAccessException) when (!data.ShouldPass)
        {
            // This is a successful test, because the repo rejected the invalid password as it should
        }

        // Now do a Send/Receive which should get no changes
        // Running in same test because it's dependent on CloneProject happening first
        try
        {
            string result2 = srService.SendReceiveProject(data.ProjectCode, projectDir, data.Username, data.Password);
            if (data.ShouldPass)
            {
                result2.ShouldNotContain("abort");
                result2.ShouldNotContain("error");
                result2.ShouldContain("no changes from others");
                fwdataFile.ShouldSatisfyAllConditions(
                    () => fileInfo.Exists.ShouldBeTrue(),
                    () => fileInfo.Length.ShouldBe(oldLength)
                );
            }
            else
            {
                result2.ShouldMatch("abort: authorization failed|Server Response 'Unauthorized'");
            }
        }
        catch (Chorus.VcsDrivers.Mercurial.RepositoryAuthorizationException)
        {
            if (data.ShouldPass)
            {
                throw;
            }
            else
            {
                // This is a successful test, because the repo rejected the invalid password as it should
            }

            ;
        }
        catch (System.UnauthorizedAccessException)
        {
            if (data.ShouldPass)
            {
                throw;
            }
            else
            {
                // This is a successful test, because the repo rejected the invalid password as it should
            }

            ;
        }
    }

    [Theory]
    [InlineData("non-existent-project", "manager", "pass")]
    [InlineData("non-existent-project", "admin", "pass")]
    // NOTE: resumable failing because can't read sena-3 repo, because owned by UID 82 (Alpine www-data) instead of 33 (Debian www-data)
    public void TestInvalidProject(string projectCode, string username, string password)
    {
        foreach (var (host, type) in HostsAndTypes)
        {
            if (type == "resumable")
            {
                continue; // Skip resumable as Chorus just retries 4xx errors constantly (because it assumes they're caused by a flaky net connection)
            }

            var _srService = new SendReceiveService(_output, host);
            string projectDir = Path.Join(_basePath, type, projectCode);
            string fwdataFile = Path.Join(projectDir, $"{projectCode}.fwdata");
            try
            {
                string result = _srService.CloneProject(projectCode, projectDir, username, password);
                throw new Exception(
                    "Clone should have thrown an exception but didn't; if we reach this point, this is a failed test");
            }
            catch (Chorus.VcsDrivers.Mercurial.ProjectLabelErrorException)
            {
                // Expected failure - this is what admin sees
            }
            catch (Chorus.VcsDrivers.Mercurial.RepositoryAuthorizationException)
            {
                // Expected failure - this is what manager user sees
            }

            fwdataFile.ShouldSatisfyAllConditions(
                () => new FileInfo(fwdataFile).Exists.ShouldBeFalse()
            );
        }
    }
}
