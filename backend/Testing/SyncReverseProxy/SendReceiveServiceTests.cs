using System.Runtime.CompilerServices;
using Chorus.VcsDrivers.Mercurial;
using Shouldly;
using Testing.Services;
using Xunit.Abstractions;

namespace Testing.SyncReverseProxy;

[Trait("Category", "Integration")]
public class SendReceiveServiceTests
{
    public SendReceiveAuth ManagerAuth = new("manager", "pass");
    public SendReceiveAuth AdminAuth = new("admin", "pass");
    public SendReceiveAuth InvalidPass = new("manager", "incorrect_pass");
    public SendReceiveAuth InvalidUser = new("invalid_user", "pass");
    public SendReceiveAuth UnauthorizedUser = new("user", "pass");

    private readonly ITestOutputHelper _output;
    private string _basePath = Path.Join(Path.GetTempPath(), "SendReceiveTests");
    private SendReceiveService _sendReceiveService;

    public SendReceiveServiceTests(ITestOutputHelper output)
    {
        _output = output;
        _sendReceiveService = new SendReceiveService(_output);
        CleanUpTempDir();
        var fileInfo = new FileInfo("Mercurial/hg");
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && fileInfo.Exists)
        {
            fileInfo.Delete();
        }
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

    private static int _folderIndex = 1;

    private (string projectDir, string fwDataFile) GetProjectDir(string projectCode,
        string? identifier = null,
        [CallerMemberName] string testName = "")
    {
        var projectDir = Path.Join(_basePath, testName);
        if (identifier is not null) projectDir = Path.Join(projectDir, identifier);
        //fwdata file containing folder name will be the same as the file name
        projectDir = Path.Join(projectDir, _folderIndex++.ToString(), projectCode);
        var fwDataFile = Path.Join(projectDir, $"{projectCode}.fwdata");
        return (projectDir, fwDataFile);
    }

    private SendReceiveParams GetParams(HgProtocol protocol,
        string projectCode = TestData.ProjectCode,
        [CallerMemberName] string testName = "")
    {
        var (projectDir, _) = GetProjectDir(projectCode, testName: testName);
        var sendReceiveParams = new SendReceiveParams(projectCode, protocol.GetTestHostName(), projectDir);
        return sendReceiveParams;
    }

    [Fact]
    public async Task VerifyHgWorking()
    {
        HgRepository.GetEnvironmentReadinessMessage("en").ShouldBeNull();
        string version = await _sendReceiveService.GetHgVersion();
        version.ShouldStartWith("Mercurial Distributed SCM");
    }

    private static readonly (string host, string type)[] HostsAndTypes =
    {
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
                yield return new[] { new SendReceiveTestData(projectCode, host, type, user, pass, valid) };
            }
        }
    }

    [Fact(
        // Skip = "Just for testing, comment out to run"
    )]
    public void CloneForDev()
    {
        var sendReceiveTestData = GetTestDataForSR("sena-3").ElementAt(1)
            .OfType<SendReceiveTestData>().First();
        _output.WriteLine("Test run: {0}", sendReceiveTestData);
        // CloneProjectAndSendReceive(sendReceiveTestData);
    }

    [Theory]
    [InlineData(HgProtocol.Hgweb, "admin")]
    [InlineData(HgProtocol.Hgweb, "manager")]
    [InlineData(HgProtocol.Resumable, "admin")]
    [InlineData(HgProtocol.Resumable, "manager")]
    public void CanCloneSendReceive(HgProtocol hgProtocol, string user)
    {
        var (projectDir, fwDataFile) = GetProjectDir(TestData.ProjectCode, Path.Join(hgProtocol.ToString(), user));
        var auth = new SendReceiveAuth(user, "pass");
        var sendReceiveParams = new SendReceiveParams(TestData.ProjectCode, hgProtocol.GetTestHostName(), projectDir);

        // Clone
        var cloneResult = _sendReceiveService.CloneProject(sendReceiveParams, auth);
        cloneResult.ShouldNotContain("abort");
        cloneResult.ShouldNotContain("error");
        Directory.Exists(projectDir).ShouldBeTrue($"Directory {projectDir} not found. Clone response: {cloneResult}");
        Directory.EnumerateFiles(projectDir).ShouldContain(fwDataFile);
        var fwDataFileInfo = new FileInfo(fwDataFile);
        fwDataFileInfo.Length.ShouldBeGreaterThan(0);
        var fwDataFileOriginalLength = fwDataFileInfo.Length;

        // SendReceive
        var srResult = _sendReceiveService.SendReceiveProject(sendReceiveParams, auth);
        srResult.ShouldNotContain("abort");
        srResult.ShouldNotContain("error");
        srResult.ShouldContain("no changes from others");
        fwDataFileInfo.Refresh();
        fwDataFileInfo.Exists.ShouldBeTrue();
        fwDataFileInfo.Length.ShouldBe(fwDataFileOriginalLength);
    }

    [Fact]
    public void InvalidPassOnCloneHgWeb()
    {
        var sendReceiveParams = GetParams(HgProtocol.Hgweb);
        var act = () => _sendReceiveService.CloneProject(sendReceiveParams, InvalidPass);

        act.ShouldThrow<RepositoryAuthorizationException>();
    }

    [Fact]
    public void InvalidPassOnCloneHgResumable()
    {
        var sendReceiveParams = GetParams(HgProtocol.Resumable);
        var act = () => _sendReceiveService.CloneProject(sendReceiveParams, InvalidPass);

        act.ShouldThrow<UnauthorizedAccessException>();
    }

    [Fact]
    public void InvalidPassOnSendReceiveHgWeb()
    {
        var sendReceiveParams = GetParams(HgProtocol.Hgweb);
        _sendReceiveService.CloneProject(sendReceiveParams, ManagerAuth);

        var act = () => _sendReceiveService.SendReceiveProject(sendReceiveParams, InvalidPass);
        act.ShouldThrow<RepositoryAuthorizationException>();
    }

    [Fact]
    public void InvalidPassOnSendReceiveHgResumable()
    {
        var sendReceiveParams = GetParams(HgProtocol.Resumable);
        _sendReceiveService.CloneProject(sendReceiveParams, ManagerAuth);

        var act = () => _sendReceiveService.SendReceiveProject(sendReceiveParams, InvalidPass);
        act.ShouldThrow<UnauthorizedAccessException>();
    }

    [Fact]
    public void InvalidUserCloneHgWeb()
    {
        var sendReceiveParams = GetParams(HgProtocol.Hgweb);
        var act = () => _sendReceiveService.CloneProject(sendReceiveParams, InvalidUser);
        act.ShouldThrow<RepositoryAuthorizationException>();
    }

    [Fact]
    public void InvalidUserCloneHgResumable()
    {
        var sendReceiveParams = GetParams(HgProtocol.Resumable);
        var act = () => _sendReceiveService.CloneProject(sendReceiveParams, InvalidUser);
        act.ShouldThrow<UnauthorizedAccessException>();
    }

    [Fact]
    public void InvalidProjectAdminLogin()
    {
        var sendReceiveParams = GetParams(HgProtocol.Hgweb, "non-existent-project");
        var act = () => _sendReceiveService.CloneProject(sendReceiveParams, AdminAuth);

        act.ShouldThrow<ProjectLabelErrorException>();
        Directory.GetFiles(sendReceiveParams.DestDir).ShouldBeEmpty();
    }

    [Fact]
    public void InvalidProjectManagerLogin()
    {
        var sendReceiveParams = GetParams(HgProtocol.Hgweb, "non-existent-project");
        var act = () => _sendReceiveService.CloneProject(sendReceiveParams, ManagerAuth);

        act.ShouldThrow<RepositoryAuthorizationException>();
        Directory.GetFiles(sendReceiveParams.DestDir).ShouldBeEmpty();
    }

    [Fact]
    public void UnauthorizedUserCloneHgWeb()
    {
        var sendReceiveParams = GetParams(HgProtocol.Hgweb);

        var act = () => _sendReceiveService.CloneProject(sendReceiveParams, UnauthorizedUser);
        act.ShouldThrow<RepositoryAuthorizationException>();
    }

    [Fact]
    public void UnauthorizedUserCloneHgResumable()
    {
        var sendReceiveParams = GetParams(HgProtocol.Resumable);

        var act = () => _sendReceiveService.CloneProject(sendReceiveParams, UnauthorizedUser);
        act.ShouldThrow<UnauthorizedAccessException>();
    }
}
