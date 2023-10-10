using System.IO.Compression;
using System.Runtime.CompilerServices;
using Chorus.VcsDrivers.Mercurial;
using LexCore.Utils;
using Shouldly;
using SIL.Progress;
using Testing.ApiTests;
using Testing.Logging;
using Testing.Services;
using Xunit.Abstractions;

namespace Testing.SyncReverseProxy;

[Trait("Category", "Integration")]
public class SendReceiveServiceTests
{
    public SendReceiveAuth ManagerAuth = new("manager", TestingEnvironmentVariables.DefaultPassword);
    public SendReceiveAuth AdminAuth = new("admin", TestingEnvironmentVariables.DefaultPassword);
    public SendReceiveAuth InvalidPass = new("manager", "incorrect_pass");
    public SendReceiveAuth InvalidUser = new("invalid_user", TestingEnvironmentVariables.DefaultPassword);
    public SendReceiveAuth UnauthorizedUser = new("user", TestingEnvironmentVariables.DefaultPassword);

    private readonly ITestOutputHelper _output;
    private string _basePath = Path.Join(Path.GetTempPath(), "SendReceiveTests");
    private SendReceiveService _sendReceiveService;

    public SendReceiveServiceTests(ITestOutputHelper output)
    {
        _output = output;
        _sendReceiveService = new SendReceiveService(_output);
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

    private static int _folderIndex = 1;

    private string GetProjectDir(string projectCode,
        string? identifier = null,
        [CallerMemberName] string testName = "")
    {
        var projectDir = Path.Join(_basePath, testName);
        if (identifier is not null) projectDir = Path.Join(projectDir, identifier);
        //fwdata file containing folder name will be the same as the file name
        projectDir = Path.Join(projectDir, _folderIndex++.ToString(), projectCode);
        return projectDir;
    }

    private SendReceiveParams GetParams(HgProtocol protocol,
        string? projectCode = null,
        [CallerMemberName] string testName = "")
    {
        projectCode ??= TestingEnvironmentVariables.ProjectCode;
        var sendReceiveParams = new SendReceiveParams(projectCode, protocol.GetTestHostName(), GetProjectDir(projectCode, testName: testName));
        return sendReceiveParams;
    }

    [Fact]
    public async Task VerifyHgWorking()
    {
        string version = await _sendReceiveService.GetHgVersion();
        version.ShouldStartWith("Mercurial Distributed SCM");
        HgRunner.Run("hg version", Environment.CurrentDirectory, 5, new XunitStringBuilderProgress(_output) {ShowVerbose = true});
        HgRepository.GetEnvironmentReadinessMessage("en").ShouldBeNull();
    }

    [Fact]
    public void CloneBigProject()
    {
        RunCloneSendReceive(HgProtocol.Hgweb, "admin", "elawa-dev-flex");
    }

    [Theory]
    [InlineData(HgProtocol.Hgweb, "admin")]
    [InlineData(HgProtocol.Hgweb, "manager")]
    [InlineData(HgProtocol.Resumable, "admin")]
    [InlineData(HgProtocol.Resumable, "manager")]
    public void CanCloneSendReceive(HgProtocol hgProtocol, string user)
    {
        RunCloneSendReceive(hgProtocol, user, TestingEnvironmentVariables.ProjectCode);
    }
    private void RunCloneSendReceive(HgProtocol hgProtocol, string user, string projectCode)
    {
        var auth = new SendReceiveAuth(user, TestingEnvironmentVariables.DefaultPassword);
        var sendReceiveParams = new SendReceiveParams(projectCode, hgProtocol.GetTestHostName(),
            GetProjectDir(projectCode, Path.Join(hgProtocol.ToString(), user)));
        var projectDir = sendReceiveParams.DestDir;
        var fwDataFile = sendReceiveParams.FwDataFile;

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
    public void ModifyProjectData()
    {
        var projectCode = TestingEnvironmentVariables.ProjectCode;

        // Clone
        var sendReceiveParams = GetParams(HgProtocol.Hgweb, projectCode);
        var cloneResult = _sendReceiveService.CloneProject(sendReceiveParams, AdminAuth);
        cloneResult.ShouldNotContain("abort");
        cloneResult.ShouldNotContain("error");
        var fwDataFileInfo = new FileInfo(sendReceiveParams.FwDataFile);
        fwDataFileInfo.Length.ShouldBeGreaterThan(0);
        ModifyProjectHelper.ModifyProject(sendReceiveParams.FwDataFile);

        // Send changes
        var srResult = _sendReceiveService.SendReceiveProject(sendReceiveParams, AdminAuth, "Modify project data automated test");
        srResult.ShouldNotContain("abort");
        srResult.ShouldNotContain("error");
    }

    [Fact]
    public async Task SendNewProject()
    {
        var id = Guid.NewGuid();
        var apiTester = new ApiTestBase();
        var auth = AdminAuth;
        var projectCode = "kevin-test-01";
        await apiTester.LoginAs(auth.Username, auth.Password);
        await apiTester
            .ExecuteGql($$"""
                          mutation {
                              createProject(input: {
                                  name: "Kevin test 01",
                                  type: FL_EX,
                                  id: "{{id}}",
                                  code: "{{projectCode}}",
                                  description: "this is a new project created during a unit test to verify we can send a new project for the first time",
                                  retentionPolicy: DEV
                              }) {
                                  createProjectResponse {
                                      id
                                      result
                                  }
                              }
                          }
                          """);
        await using var deleteProject = Defer.Async( () => apiTester.HttpClient.DeleteAsync($"{apiTester.BaseUrl}/api/project/project/{id}"));

        var sendReceiveParams = GetParams(HgProtocol.Hgweb, projectCode);
        var stream = await apiTester.HttpClient.GetStreamAsync("https://drive.google.com/uc?export=download&id=1w357T1Ti7bDwEof4HPBUZ5gB7WSKA5O2");
        using var zip = new ZipArchive(stream);
        zip.ExtractToDirectory(sendReceiveParams.DestDir);
        File.Exists(sendReceiveParams.FwDataFile).ShouldBeTrue();

        //hack around the fact that our send and receive won't create a repo from scratch.
        var progress = new NullProgress();
        HgRunner.Run("hg init", sendReceiveParams.DestDir, 1, progress);
        HgRunner.Run("hg branch 7500002.7000072", sendReceiveParams.DestDir, 1, progress);
        HgRunner.Run($"hg add Lexicon.fwstub", sendReceiveParams.DestDir, 1, progress);
        HgRunner.Run("""hg commit -m "first commit" """, sendReceiveParams.DestDir, 1, progress);

        //add a bunch of small files, must be in separate commits otherwise hg runs out of memory. But we want the push to be large
        const int totalSizeMb = 1100;
        const int fileCount = 25;
        for (int i = 1; i < fileCount; i++)
        {
            var bigFileName = $"big-file{i}.bin";
            WriteBigFile(Path.Combine(sendReceiveParams.DestDir, bigFileName), totalSizeMb / fileCount);
            HgRunner.Run($"hg add {bigFileName}", sendReceiveParams.DestDir, 1, progress);
            HgRunner.Run($"""hg commit -m "large file commit {i}" """, sendReceiveParams.DestDir, 5, progress).ExitCode.ShouldBe(0);
        }


        //attempt to prevent issue where project isn't found yet.
        await Task.Delay(TimeSpan.FromSeconds(5));
        var srResult = _sendReceiveService.SendReceiveProject(sendReceiveParams, auth);
        _output.WriteLine(srResult);
        srResult.ShouldNotContain("abort");
        srResult.ShouldNotContain("failure");
        srResult.ShouldNotContain("error");
    }

    private static void WriteBigFile(string path, int sizeMb)
    {
        var random = new Random();
        using var file = File.Open(path, FileMode.Create);

        Span<byte> buffer = stackalloc byte[1024 * 1024];
        for (int i = 0; i < (sizeMb * 1024 * 1024 / buffer.Length); i++)
        {
            random.NextBytes(buffer);
            file.Write(buffer);
        }
        file.Flush(true);
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
