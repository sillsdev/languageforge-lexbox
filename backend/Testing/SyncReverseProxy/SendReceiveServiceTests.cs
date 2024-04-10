using Chorus.VcsDrivers.Mercurial;
using LexBoxApi.Auth;
using LexCore.Utils;
using Shouldly;
using SIL.Progress;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Testing.ApiTests;
using Testing.Fixtures;
using Testing.Logging;
using Testing.Services;
using Xunit.Abstractions;
using static Testing.Services.Constants;
using static Testing.Services.Utils;

namespace Testing.SyncReverseProxy;

[Trait("Category", "Integration")]
public class SendReceiveServiceTests : IClassFixture<SendReceiveFixture>
{
    private readonly ITestOutputHelper _output;
    private readonly SendReceiveFixture _fixtureOutput;
    private readonly ApiTestBase _adminApiTester;

    private readonly SendReceiveService _sendReceiveService;

    public SendReceiveServiceTests(ITestOutputHelper output, SendReceiveFixture sendReceiveFixture)
    {
        _output = output;
        _sendReceiveService = new SendReceiveService(_output);
        _fixtureOutput = sendReceiveFixture;
        _adminApiTester = _fixtureOutput.AdminApiTester;
    }

    [Fact]
    public async Task VerifyHgWorking()
    {
        var version = await _sendReceiveService.GetHgVersion();
        version.ShouldStartWith("Mercurial Distributed SCM");
        _output.WriteLine("Hg version: " + version);
        HgRunner.Run("hg version", Environment.CurrentDirectory, 5, new XunitStringBuilderProgress(_output) { ShowVerbose = true });
        HgRepository.GetEnvironmentReadinessMessage("en").ShouldBeNull();
    }

    [Fact]
    public void CloneBigProject()
    {
        var sendReceiveParams = GetParams(HgProtocol.Hgweb, "elawa-dev-flex");
        _sendReceiveService.RunCloneSendReceive(sendReceiveParams, AdminAuth);
    }

    [Theory]
    [InlineData(HgProtocol.Hgweb, "manager")]
    [InlineData(HgProtocol.Resumable, "manager")]
    public void CanCloneSendReceive(HgProtocol hgProtocol, string user)
    {
        var sendReceiveParams = GetParams(hgProtocol);
        _sendReceiveService.RunCloneSendReceive(sendReceiveParams,
            new SendReceiveAuth(user, TestingEnvironmentVariables.DefaultPassword));
    }

    [Theory]
    [InlineData(HgProtocol.Hgweb, "manager")]
    [InlineData(HgProtocol.Resumable, "manager")]
    public async Task CanCloneSendReceiveWithJwtOverBasicAuth(HgProtocol hgProtocol, string user)
    {
        var projectCode = TestingEnvironmentVariables.ProjectCode;
        var jwt = await JwtHelper.GetProjectJwtForUser(new SendReceiveAuth(user, TestingEnvironmentVariables.DefaultPassword), projectCode);
        var sendReceiveParams = GetParams(hgProtocol, projectCode);
        _sendReceiveService.RunCloneSendReceive(sendReceiveParams,
            new SendReceiveAuth(AuthKernel.JwtOverBasicAuthUsername, jwt));
    }

    [Theory]
    [InlineData(HgProtocol.Hgweb)]
    [InlineData(HgProtocol.Resumable)]
    public async Task ModifyProjectData(HgProtocol protocol)
    {
        // Create a fresh project
        var projectConfig = InitLocalFlexProjectWithRepo();
        await using var project = await RegisterProjectInLexBox(projectConfig, _adminApiTester);

        // Push the project to the server
        var sendReceiveParams = new SendReceiveParams(protocol, projectConfig);
        _sendReceiveService.SendReceiveProject(sendReceiveParams, AdminAuth);

        // Wait for Lexbox to finish updating the project metadata
        await Task.Delay(5000);

        // Verify pushed and store last commit
        var gqlQuery =
$$"""
query projectLastCommit {
    projectByCode(code: "{{projectConfig.Code}}") {
        lastCommit
    }
}
""";
        var jsonResult = await _adminApiTester.ExecuteGql(gqlQuery);
        var lastCommitDate = jsonResult?["data"]?["projectByCode"]?["lastCommit"]?.ToString();
        lastCommitDate.ShouldNotBeNullOrEmpty();

        // Modify
        var fwDataFileInfo = new FileInfo(sendReceiveParams.FwDataFile);
        fwDataFileInfo.Length.ShouldBeGreaterThan(0);
        ModifyProjectHelper.ModifyProject(sendReceiveParams.FwDataFile);

        // Push changes
        _sendReceiveService.SendReceiveProject(sendReceiveParams, AdminAuth, "Modify project data automated test");

        // Wait for Lexbox to finish updating the project metadata
        await Task.Delay(5000);

        // Verify the push updated the last commit date
        jsonResult = await _adminApiTester.ExecuteGql(gqlQuery);
        var lastCommitDateAfter = jsonResult?["data"]?["projectByCode"]?["lastCommit"]?.ToString();
        lastCommitDateAfter.ShouldBeGreaterThan(lastCommitDate);
    }

    [Theory]
    [InlineData(HgProtocol.Hgweb)]
    [InlineData(HgProtocol.Resumable)]
    public async Task SendReceiveAfterProjectReset(HgProtocol protocol)
    {
        // Create new project on server so we don't reset our master test project
        var id = Guid.NewGuid();
        var newProjectCode = $"{(protocol == HgProtocol.Hgweb ? "web" : "res")}-sr-reset-{id:N}";
        await _adminApiTester.ExecuteGql($$"""
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
        await using var deleteProject = Defer.Async(() => _adminApiTester.HttpClient.DeleteAsync($"{_adminApiTester.BaseUrl}/api/project/{id}"));

        // Sleep 5 seconds to ensure hgweb picks up newly-created test project
        await Task.Delay(TimeSpan.FromSeconds(5));

        // Populate new project from original so we're not resetting original test project in E2E tests
        // Note that this time we're cloning via hg clone rather than Chorus, because we don't want a .fwdata file yet
        var progress = new NullProgress();
        var origProjectCode = TestingEnvironmentVariables.ProjectCode;
        var newProjectDir = GetNewProjectConfig().Dir;
        Directory.CreateDirectory(newProjectDir);
        var hgwebUrl = new UriBuilder
        {
            Scheme = TestingEnvironmentVariables.HttpScheme,
            Host = HgProtocol.Hgweb.GetTestHostName(),
            UserName = AdminAuth.Username,
            Password = AdminAuth.Password
        };
        HgRunner.Run($"hg clone {hgwebUrl}{origProjectCode} {newProjectDir}", "", 15, progress);
        HgRunner.Run($"hg push {hgwebUrl}{newProjectCode}", newProjectDir, 15, progress);

        // Sleep 5 seconds to ensure hgweb picks up newly-pushed commits
        await Task.Delay(TimeSpan.FromSeconds(5));

        // Now clone again via Chorus so that we'll hvae a .fwdata file
        var sendReceiveParams = GetParams(protocol, newProjectCode);
        Directory.CreateDirectory(sendReceiveParams.Dir);
        var srResult = _sendReceiveService.CloneProject(sendReceiveParams, AdminAuth);
        _output.WriteLine(srResult);

        // Delete the Chorus revision cache if resumable, otherwise Chorus will send the wrong data during S/R
        // Note that HgWeb protocol does *not* have this issue
        var chorusStorageFolder = Path.Join(sendReceiveParams.Dir, "Chorus", "ChorusStorage");
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
        var response = await _adminApiTester.HttpClient.GetAsync(tipUri.Uri);
        var jsonResult = await response.Content.ReadFromJsonAsync<JsonObject>();
        var originalTip = jsonResult?["node"]?.AsValue()?.ToString();
        originalTip.ShouldNotBeNull();

        // /api/project/resetProject/{code}
        // /api/project/finishResetProject/{code}  // leave project empty
        // /api/project/backupProject/{code}  // download zip file
        // /api/project/upload-zip/{code}  // upload zip file

        // Step 1: reset project
        await _adminApiTester.HttpClient.PostAsync($"{_adminApiTester.BaseUrl}/api/project/resetProject/{newProjectCode}", null);
        await _adminApiTester.HttpClient.PostAsync($"{_adminApiTester.BaseUrl}/api/project/finishResetProject/{newProjectCode}", null);

        // Sleep 5 seconds to ensure hgweb picks up newly-reset project
        await Task.Delay(TimeSpan.FromSeconds(5));

        // Step 2: verify project is now empty, i.e. tip is "0000000..."
        response = await _adminApiTester.HttpClient.GetAsync(tipUri.Uri);
        jsonResult = await response.Content.ReadFromJsonAsync<JsonObject>();
        var emptyTip = jsonResult?["node"]?.AsValue()?.ToString();
        emptyTip.ShouldNotBeNull();
        emptyTip.ShouldNotBeEmpty();
        emptyTip.Replace("0", "").ShouldBeEmpty();

        // Step 3: do Send/Receive
        var srResultStep3 = _sendReceiveService.SendReceiveProject(sendReceiveParams, AdminAuth);
        _output.WriteLine(srResultStep3);

        // Step 4: verify project tip is same hash as original project tip
        response = await _adminApiTester.HttpClient.GetAsync(tipUri.Uri);
        jsonResult = await response.Content.ReadFromJsonAsync<JsonObject>();
        var postSRTip = jsonResult?["node"]?.AsValue()?.ToString();
        postSRTip.ShouldNotBeNull();
        postSRTip.ShouldBe(originalTip);
    }

    [Theory]
    [InlineData(180, 10)]
    [InlineData(10, 1)]
    public async Task SendNewProject(int totalSizeMb, int fileCount)
    {
        var projectConfig = InitLocalFlexProjectWithRepo();
        await using var project = await RegisterProjectInLexBox(projectConfig, _adminApiTester);
        var sendReceiveParams = new SendReceiveParams(HgProtocol.Hgweb, projectConfig);

        //add a bunch of small files, must be in separate commits otherwise hg runs out of memory. But we want the push to be large
        var progress = new NullProgress();
        for (var i = 1; i <= fileCount; i++)
        {
            var fileName = $"test-file{i}.bin";
            WriteFile(Path.Combine(sendReceiveParams.Dir, fileName), totalSizeMb / fileCount);
            HgRunner.Run($"hg add {fileName}", sendReceiveParams.Dir, 1, progress);
            HgRunner.Run($"""hg commit -m "large file commit {i}" """, sendReceiveParams.Dir, 5, progress).ExitCode.ShouldBe(0);
        }

        //attempt to prevent issue where project isn't found yet.
        await Task.Delay(TimeSpan.FromSeconds(5));
        var srResult = _sendReceiveService.SendReceiveProject(sendReceiveParams, AdminAuth);
        _output.WriteLine(srResult);
    }

    private static void WriteFile(string path, int sizeMb)
    {
        var random = new Random();
        using var file = File.Open(path, FileMode.Create);

        Span<byte> buffer = stackalloc byte[1024 * 1024];
        for (var i = 0; i < (sizeMb * 1024 * 1024 / buffer.Length); i++)
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
        Directory.GetFiles(sendReceiveParams.Dir).ShouldBeEmpty();
    }

    [Fact]
    public void InvalidProjectManagerLogin()
    {
        var sendReceiveParams = GetParams(HgProtocol.Hgweb, "non-existent-project");
        var act = () => _sendReceiveService.CloneProject(sendReceiveParams, ManagerAuth);

        act.ShouldThrow<RepositoryAuthorizationException>();
        Directory.GetFiles(sendReceiveParams.Dir).ShouldBeEmpty();
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
