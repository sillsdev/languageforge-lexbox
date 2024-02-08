using System.ComponentModel.Composition;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using Chorus.VcsDrivers.Mercurial;
using LexBoxApi.Auth;
using LexCore.Utils;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using SIL.Progress;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Testing.ApiTests;
using Testing.Fixtures;
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
        _output.WriteLine("Hg version: " + version);
        HgRunner.Run("hg version", Environment.CurrentDirectory, 5, new XunitStringBuilderProgress(_output) {ShowVerbose = true});
        HgRepository.GetEnvironmentReadinessMessage("en").ShouldBeNull();
    }

    [Fact]
    public void CloneBigProject()
    {
        RunCloneSendReceive(HgProtocol.Hgweb, AdminAuth, "elawa-dev-flex");
    }

    [Theory]
    [InlineData(HgProtocol.Hgweb, "manager")]
    [InlineData(HgProtocol.Resumable, "manager")]
    public void CanCloneSendReceive(HgProtocol hgProtocol, string user)
    {
        RunCloneSendReceive(hgProtocol,
            new SendReceiveAuth(user, TestingEnvironmentVariables.DefaultPassword),
            TestingEnvironmentVariables.ProjectCode);
    }

    [Theory]
    [InlineData(HgProtocol.Hgweb, "manager")]
    [InlineData(HgProtocol.Resumable, "manager")]
    public async Task CanCloneSendReceiveWithJwtOverBasicAuth(HgProtocol hgProtocol, string user)
    {
        var projectCode = TestingEnvironmentVariables.ProjectCode;
        var jwt = await JwtHelper.GetProjectJwtForUser(new SendReceiveAuth(user, TestingEnvironmentVariables.DefaultPassword), projectCode);

        RunCloneSendReceive(hgProtocol,
            new SendReceiveAuth(AuthKernel.JwtOverBasicAuthUsername, jwt),
            projectCode);
    }

    private void RunCloneSendReceive(HgProtocol hgProtocol, SendReceiveAuth auth, string projectCode)
    {
        var sendReceiveParams = new SendReceiveParams(projectCode, hgProtocol.GetTestHostName(),
            GetProjectDir(projectCode, Path.Join(hgProtocol.ToString(), auth.Username)));
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

    [Theory]
    [InlineData(HgProtocol.Hgweb)]
    [InlineData(HgProtocol.Resumable)]
    public async Task ModifyProjectData(HgProtocol protocol)
    {
        var projectCode = TestingEnvironmentVariables.ProjectCode;
        var apiTester = new ApiTestBase();
        var auth = AdminAuth;
        await apiTester.LoginAs(auth.Username, auth.Password);
        string gqlQuery =
$$"""
query projectLastCommit {
    projectByCode(code: "{{projectCode}}") {
        lastCommit
    }
}
""";
        var jsonResult = await apiTester.ExecuteGql(gqlQuery);
        var lastCommitDate = jsonResult["data"]["projectByCode"]["lastCommit"].ToString();

        // Clone
        var sendReceiveParams = GetParams(protocol, projectCode);
        var cloneResult = _sendReceiveService.CloneProject(sendReceiveParams, auth);
        cloneResult.ShouldNotContain("abort");
        cloneResult.ShouldNotContain("error");
        var fwDataFileInfo = new FileInfo(sendReceiveParams.FwDataFile);
        fwDataFileInfo.Length.ShouldBeGreaterThan(0);
        ModifyProjectHelper.ModifyProject(sendReceiveParams.FwDataFile);

        // Send changes
        var srResult = _sendReceiveService.SendReceiveProject(sendReceiveParams, auth, "Modify project data automated test");
        srResult.ShouldNotContain("abort");
        srResult.ShouldNotContain("error");
        await Task.Delay(6000);

        jsonResult = await apiTester.ExecuteGql(gqlQuery);
        var lastCommitDateAfter = jsonResult["data"]["projectByCode"]["lastCommit"].ToString();
        lastCommitDateAfter.ShouldBeGreaterThan(lastCommitDate);
    }


    [Theory]
    [InlineData(HgProtocol.Hgweb)]
    [InlineData(HgProtocol.Resumable)]
    public async Task SendReceiveAfterProjectReset(HgProtocol protocol)
    {
        // Create new project on server so we don't reset our master test project
        var id = Guid.NewGuid();
        var newProjectCode = $"send-receive-{protocol.ToString().ToLowerInvariant()}-after-reset-test-{id:N}";
        var apiTester = new ApiTestBase();
        var auth = AdminAuth;
        await apiTester.LoginAs(auth.Username, auth.Password);
        await apiTester.ExecuteGql($$"""
            mutation {
                createProject(input: {
                    name: "Send new project test",
                    type: FL_EX,
                    id: "{{id}}",
                    code: "{{newProjectCode}}",
                    description: "A project created during a unit test to test Send/Receive operation via {{protocol}} after a project reset",
                    retentionPolicy: DEV
                }) {
                    createProjectResponse {
                        id
                        result
                    }
                    errors {
                        __typename
                        ... on DbError {
                            code
                            message
                        }
                    }
                }
            }
            """);

        // Ensure newly-created project is deleted after test completes
        await using var deleteProject = Defer.Async(() => apiTester.HttpClient.DeleteAsync($"{apiTester.BaseUrl}/api/project/project/{id}"));

        // Populate new project from original so we're not resetting original test project in E2E tests
        // Note that this time we're cloning via hg clone rather than Chorus, because we don't want a .fwdata file yet
        var progress = new NullProgress();
        var origProjectCode = TestingEnvironmentVariables.ProjectCode;
        var sourceProjectDir = GetProjectDir(origProjectCode);
        Directory.CreateDirectory(sourceProjectDir);
        var hgwebUrl = new UriBuilder
        {
            Scheme = TestingEnvironmentVariables.HttpScheme,
            Host = HgProtocol.Hgweb.GetTestHostName(),
            UserName = auth.Username,
            Password = auth.Password
        };
        HgRunner.Run($"hg clone {hgwebUrl}{origProjectCode} {sourceProjectDir}", "", 15, progress);
        HgRunner.Run($"hg push {hgwebUrl}{newProjectCode}", sourceProjectDir, 15, progress);

        // Now clone again via Chorus so that we'll hvae a .fwdata file
        var sendReceiveParams = GetParams(protocol, newProjectCode);
        Directory.CreateDirectory(sendReceiveParams.DestDir);
        var srResult = _sendReceiveService.CloneProject(sendReceiveParams, auth);
        _output.WriteLine(srResult);
        srResult.ShouldNotContain("abort");
        srResult.ShouldNotContain("failure");
        srResult.ShouldNotContain("error");

        // Delete the Chorus revision cache if resumable, otherwise Chorus will send the wrong data during S/R
        // Note that HgWeb protocol does *not* have this issue
        string chorusStorageFolder = Path.Join(sendReceiveParams.DestDir, "Chorus", "ChorusStorage");
        var revisionCache = new FileInfo(Path.Join(chorusStorageFolder, "revisioncache.json"));
        if (revisionCache.Exists)
        {
            revisionCache.Delete();
        }

        // With all that setup out of the way, we can now start the actual test itself

        // First, save the current value of `hg tip` from the original project
        var tipUri = new UriBuilder
        {
            Scheme = TestingEnvironmentVariables.HttpScheme,
            Host = TestingEnvironmentVariables.ServerHostname,
            Path = $"hg/{newProjectCode}/tags",
            Query = "?style=json"
        };
        var response = await apiTester.HttpClient.GetAsync(tipUri.Uri);
        var jsonResult = await response.Content.ReadFromJsonAsync<JsonObject>();
        var originalTip = jsonResult?["node"]?.AsValue()?.ToString();
        originalTip.ShouldNotBeNull();

        // /api/project/resetProject/{code}
        // /api/project/finishResetProject/{code}  // leave project empty
        // /api/project/backupProject/{code}  // download zip file
        // /api/project/upload-zip/{code}  // upload zip file

        // Step 1: reset project
        await apiTester.HttpClient.PostAsync($"{apiTester.BaseUrl}/api/project/resetProject/{newProjectCode}", null);
        await apiTester.HttpClient.PostAsync($"{apiTester.BaseUrl}/api/project/finishResetProject/{newProjectCode}", null);

        // Step 2: verify project is now empty, i.e. tip is "0000000..."
        response = await apiTester.HttpClient.GetAsync(tipUri.Uri);
        jsonResult = await response.Content.ReadFromJsonAsync<JsonObject>();
        var emptyTip = jsonResult?["node"]?.AsValue()?.ToString();
        emptyTip.ShouldNotBeNull();
        emptyTip.ShouldNotBeEmpty();
        emptyTip.Replace("0", "").ShouldBeEmpty();

        // Step 3: do Send/Receive
        var srResultStep3 = _sendReceiveService.SendReceiveProject(sendReceiveParams, auth);
        _output.WriteLine(srResultStep3);
        srResultStep3.ShouldNotContain("abort");
        srResultStep3.ShouldNotContain("failure");
        srResultStep3.ShouldNotContain("error");

        // Step 4: verify project tip is same hash as original project tip
        response = await apiTester.HttpClient.GetAsync(tipUri.Uri);
        jsonResult = await response.Content.ReadFromJsonAsync<JsonObject>();
        var postSRTip = jsonResult?["node"]?.AsValue()?.ToString();
        postSRTip.ShouldNotBeNull();
        postSRTip.ShouldBe(originalTip);
    }

    [Fact]
    public async Task SendNewProject()
    {
        var id = Guid.NewGuid();
        var apiTester = new ApiTestBase();
        var auth = AdminAuth;
        var projectCode = "send-new-project-test-" + id.ToString("N");
        await apiTester.LoginAs(auth.Username, auth.Password);
        await apiTester.ExecuteGql($$"""
            mutation {
                createProject(input: {
                    name: "Send new project test",
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
                    errors {
                        __typename
                        ... on DbError {
                            code
                            message
                        }
                    }
                }
            }
            """);

        await using var deleteProject = Defer.Async(() => apiTester.HttpClient.DeleteAsync($"{apiTester.BaseUrl}/api/project/project/{id}"));

        var sendReceiveParams = GetParams(HgProtocol.Hgweb, projectCode);
        await using (var stream = await apiTester.HttpClient.GetStreamAsync("https://drive.google.com/uc?export=download&id=1w357T1Ti7bDwEof4HPBUZ5gB7WSKA5O2"))
        using(var zip = new ZipArchive(stream))
        {
            zip.ExtractToDirectory(sendReceiveParams.DestDir);
        }
        File.Move(Path.Join(sendReceiveParams.DestDir, "kevin-test-01.fwdata"), sendReceiveParams.FwDataFile);
        Directory.EnumerateFiles(sendReceiveParams.DestDir).ShouldContain(sendReceiveParams.FwDataFile);

        //hack around the fact that our send and receive won't create a repo from scratch.
        var progress = new NullProgress();
        HgRunner.Run("hg init", sendReceiveParams.DestDir, 1, progress);
        HgRunner.Run("hg branch 7500002.7000072", sendReceiveParams.DestDir, 1, progress);
        HgRunner.Run($"hg add Lexicon.fwstub", sendReceiveParams.DestDir, 1, progress);
        HgRunner.Run("""hg commit -m "first commit" """, sendReceiveParams.DestDir, 1, progress);

        //add a bunch of small files, must be in separate commits otherwise hg runs out of memory. But we want the push to be large
        const int totalSizeMb = 180;
        const int fileCount = 10;
        for (int i = 1; i <= fileCount; i++)
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
