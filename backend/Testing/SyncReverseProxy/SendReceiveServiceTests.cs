using Chorus.VcsDrivers.Mercurial;
using LexBoxApi.Auth;
using FluentAssertions;
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

[Trait("Category", "FlakyIntegration")]
public class SendReceiveServiceTests : IClassFixture<IntegrationFixture>
{
    private readonly ITestOutputHelper _output;
    private readonly IntegrationFixture _srFixture;
    private readonly ApiTestBase _adminApiTester;

    private readonly SendReceiveService _sendReceiveService;

    public SendReceiveServiceTests(ITestOutputHelper output, IntegrationFixture sendReceiveSrFixture)
    {
        _output = output;
        _sendReceiveService = new SendReceiveService(_output);
        _srFixture = sendReceiveSrFixture;
        _adminApiTester = _srFixture.AdminApiTester;
    }

    [Fact]
    public async Task VerifyHgWorking()
    {
        var version = await _sendReceiveService.GetHgVersion();
        version.Should().StartWith("Mercurial Distributed SCM");
        _output.WriteLine("Hg version: " + version);
        HgRunner.Run("hg version", Environment.CurrentDirectory, 5, new XunitStringBuilderProgress(_output) { ShowVerbose = true });
        HgRepository.GetEnvironmentReadinessMessage("en").Should().BeNull();
    }

    [Theory]
    [InlineData(HgProtocol.Hgweb)]
    [InlineData(HgProtocol.Resumable)]
    public void CloneBigProject(HgProtocol hgProtocol)
    {
        var sendReceiveParams = GetParams(hgProtocol, "elawa-dev-flex");
        _sendReceiveService.RunCloneSendReceive(sendReceiveParams, AdminAuth);
    }

    [Theory]
    [InlineData(HgProtocol.Hgweb)]
    [InlineData(HgProtocol.Resumable)]
    public async Task CloneConfidentialProjectAsOrgManager(HgProtocol protocol)
    {
        // Create a fresh project
        var projectConfig = _srFixture.InitLocalFlexProjectWithRepo(protocol, isConfidential: true, LexData.SeedingData.TestOrgId);
        await using var project = await RegisterProjectInLexBox(projectConfig, _adminApiTester, true);

        // Push the project to the server
        var sendReceiveParams = new SendReceiveParams(protocol, projectConfig);
        _sendReceiveService.SendReceiveProject(sendReceiveParams, ManagerAuth);

        // Verify pushed
        var lastCommitDate = await _adminApiTester.GetProjectLastCommit(projectConfig.Code);
        lastCommitDate.Should().NotBeNull();
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
        var projectConfig = _srFixture.InitLocalFlexProjectWithRepo();
        await using var project = await RegisterProjectInLexBox(projectConfig, _adminApiTester, true);

        // Push the project to the server
        var sendReceiveParams = new SendReceiveParams(protocol, projectConfig);
        _sendReceiveService.SendReceiveProject(sendReceiveParams, AdminAuth);

        // Verify pushed and store last commit
        var lastCommitDate = await _adminApiTester.GetProjectLastCommit(projectConfig.Code);
        lastCommitDate.Should().NotBeNull();

        // Modify
        var fwDataFileInfo = new FileInfo(sendReceiveParams.FwDataFile);
        fwDataFileInfo.Length.Should().BeGreaterThan(0);
        ModifyProjectHelper.ModifyProject(sendReceiveParams.FwDataFile);

        // Push changes
        _sendReceiveService.SendReceiveProject(sendReceiveParams, AdminAuth, "Modify project data automated test");

        // Verify the push updated the last commit date
        var lastCommitDateAfter = await _adminApiTester.GetProjectLastCommit(projectConfig.Code);
        lastCommitDateAfter.Should().BeAfter(lastCommitDate.Value);
    }

    [Theory]
    [InlineData(HgProtocol.Hgweb)]
    [InlineData(HgProtocol.Resumable)]
    public async Task SendReceiveAfterProjectReset(HgProtocol protocol)
    {
        // Create a fresh project
        var projectConfig = _srFixture.InitLocalFlexProjectWithRepo(protocol, isConfidential: false, null, "SR_AfterReset");
        await using var project = await RegisterProjectInLexBox(projectConfig, _adminApiTester, true);

        var sendReceiveParams = new SendReceiveParams(protocol, projectConfig);
        var srResult = _sendReceiveService.SendReceiveProject(sendReceiveParams, AdminAuth);

        // First, save the current value of `hg tip` from the original project
        var tipUri = $"/hg/{projectConfig.Code}/tags?style=json";
        var response = await _adminApiTester.HttpClient.GetAsync(tipUri);
        var jsonResult = await response.Content.ReadFromJsonAsync<JsonObject>();
        var originalTip = jsonResult?["node"]?.AsValue()?.ToString();
        originalTip.Should().NotBeNull();

        // /api/project/resetProject/{code}
        // /api/project/finishResetProject/{code}  // leave project empty
        // /api/project/backupProject/{code}  // download zip file
        // /api/project/upload-zip/{code}  // upload zip file

        // Step 1: reset project
        await _adminApiTester.HttpClient.PostAsync($"{_adminApiTester.BaseUrl}/api/project/resetProject/{projectConfig.Code}", null);
        await _adminApiTester.HttpClient.PostAsync($"{_adminApiTester.BaseUrl}/api/project/finishResetProject/{projectConfig.Code}", null);

        // Step 2: verify project is now empty, i.e. tip is "0000000..."
        response = await _adminApiTester.HttpClient.GetAsync(tipUri);
        jsonResult = await response.Content.ReadFromJsonAsync<JsonObject>();
        var emptyTip = jsonResult?["node"]?.AsValue()?.ToString();
        emptyTip.Should().NotBeNullOrEmpty();
        emptyTip.Replace("0", "").Should().BeEmpty();

        // Step 3: do Send/Receive
        if (protocol == HgProtocol.Resumable)
        {
            // Delete the Chorus revision cache of resumable, otherwise Chorus will send the wrong data during S/R
            var chorusStorageFolder = Path.Join(sendReceiveParams.Dir, "Chorus", "ChorusStorage");
            var revisionCache = new FileInfo(Path.Join(chorusStorageFolder, "revisioncache.json"));
            if (revisionCache.Exists)
            {
                revisionCache.Delete();
            }
        }

        var srResultStep3 = _sendReceiveService.SendReceiveProject(sendReceiveParams, AdminAuth);
        _output.WriteLine(srResultStep3);

        // Step 4: verify project tip is same hash as original project tip
        response = await _adminApiTester.HttpClient.GetAsync(tipUri);
        jsonResult = await response.Content.ReadFromJsonAsync<JsonObject>();
        var postSRTip = jsonResult?["node"]?.AsValue()?.ToString();
        postSRTip.Should().NotBeNull();
        postSRTip.Should().Be(originalTip);
    }

    [Fact]
    public async Task SendNewProject_Big()
    {
        await SendNewProject(180, 10);
    }

    [Fact]
    public async Task SendNewProject_Medium()
    {
        await SendNewProject(90, 5);
    }

    private async Task SendNewProject(int totalSizeMb, int fileCount)
    {
        var projectConfig = _srFixture.InitLocalFlexProjectWithRepo();
        await using var project = await RegisterProjectInLexBox(projectConfig, _adminApiTester, true);

        await WaitForHgRefreshIntervalAsync();

        var sendReceiveParams = new SendReceiveParams(HgProtocol.Hgweb, projectConfig);

        //add a bunch of small files, must be in separate commits otherwise hg runs out of memory. But we want the push to be large
        var progress = new NullProgress();
        for (var i = 1; i <= fileCount; i++)
        {
            var fileName = $"test-file{i}.bin";
            WriteFile(Path.Combine(sendReceiveParams.Dir, fileName), totalSizeMb / fileCount);
            HgRunner.Run($"hg add {fileName}", sendReceiveParams.Dir, 5, progress);
            HgRunner.Run($"""hg commit -m "large file commit {i}" """, sendReceiveParams.Dir, 5, progress).ExitCode.Should().Be(0);
        }

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

        act.Should().Throw<RepositoryAuthorizationException>();
    }

    [Fact]
    public void InvalidPassOnCloneHgResumable()
    {
        var sendReceiveParams = GetParams(HgProtocol.Resumable);
        var act = () => _sendReceiveService.CloneProject(sendReceiveParams, InvalidPass);

        act.Should().Throw<UnauthorizedAccessException>();
    }

    [Fact]
    public void InvalidPassOnSendReceiveHgWeb()
    {
        var sendReceiveParams = GetParams(HgProtocol.Hgweb);
        _sendReceiveService.CloneProject(sendReceiveParams, ManagerAuth);

        var act = () => _sendReceiveService.SendReceiveProject(sendReceiveParams, InvalidPass);
        act.Should().Throw<RepositoryAuthorizationException>();
    }

    [Fact]
    public void InvalidPassOnSendReceiveHgResumable()
    {
        var sendReceiveParams = GetParams(HgProtocol.Resumable);
        _sendReceiveService.CloneProject(sendReceiveParams, ManagerAuth);

        var act = () => _sendReceiveService.SendReceiveProject(sendReceiveParams, InvalidPass);
        act.Should().Throw<UnauthorizedAccessException>();
    }

    [Fact]
    public void InvalidUserCloneHgWeb()
    {
        var sendReceiveParams = GetParams(HgProtocol.Hgweb);
        var act = () => _sendReceiveService.CloneProject(sendReceiveParams, InvalidUser);
        act.Should().Throw<RepositoryAuthorizationException>();
    }

    [Fact]
    public void InvalidUserCloneHgResumable()
    {
        var sendReceiveParams = GetParams(HgProtocol.Resumable);
        var act = () => _sendReceiveService.CloneProject(sendReceiveParams, InvalidUser);
        act.Should().Throw<UnauthorizedAccessException>();
    }

    [Fact]
    public void InvalidProjectAdminLogin()
    {
        var sendReceiveParams = GetParams(HgProtocol.Hgweb, "non-existent-project");
        var act = () => _sendReceiveService.CloneProject(sendReceiveParams, AdminAuth);

        act.Should().Throw<ProjectLabelErrorException>();
        Directory.GetFiles(sendReceiveParams.Dir).Should().BeEmpty();
    }

    [Fact]
    public void InvalidProjectManagerLogin()
    {
        var sendReceiveParams = GetParams(HgProtocol.Hgweb, "non-existent-project");
        var act = () => _sendReceiveService.CloneProject(sendReceiveParams, ManagerAuth);

        act.Should().Throw<RepositoryAuthorizationException>();
        Directory.GetFiles(sendReceiveParams.Dir).Should().BeEmpty();
    }

    [Fact]
    public void UnauthorizedUserCloneHgWeb()
    {
        var sendReceiveParams = GetParams(HgProtocol.Hgweb);

        var act = () => _sendReceiveService.CloneProject(sendReceiveParams, UnauthorizedUser);
        act.Should().Throw<RepositoryAuthorizationException>();
    }

    [Fact]
    public void UnauthorizedUserCloneHgResumable()
    {
        var sendReceiveParams = GetParams(HgProtocol.Resumable);

        var act = () => _sendReceiveService.CloneProject(sendReceiveParams, UnauthorizedUser);
        act.Should().Throw<UnauthorizedAccessException>();
    }
}
