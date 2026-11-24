using System.Net;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using LexBoxApi.Services;
using LexCore.Utils;
using Testing.Fixtures;
using Testing.Services;
using static Testing.Services.HgRepoTestHelpers;
using static Testing.Services.Utils;

namespace Testing.ApiTests;

/// <summary>
/// Pins down the observable behavior of LexBox's hg repo operations (create, copy, backup, reset, upload)
/// through its public surface only (GraphQL, REST, TUS and the hgweb proxy), so the implementation behind them can change.
/// Known gap: hasAbandonedTransactions is only ever asserted to be false, a repo with an abandoned transaction
/// can't be produced through the public API.
/// </summary>
[Trait("Category", "Integration")]
public class ProjectRepoTests : IClassFixture<IntegrationFixture>
{
    private readonly IntegrationFixture _fixture;
    private readonly ApiTestBase _adminApiTester;

    public ProjectRepoTests(IntegrationFixture fixture)
    {
        _fixture = fixture;
        _adminApiTester = fixture.AdminApiTester;
    }

    private async Task<LexboxProject> CreateProject(string codePrefix = "", [CallerMemberName] string projectName = "")
    {
        var config = GetNewProjectConfig(projectName: projectName);
        config = new ProjectConfig(config.Id, config.Name, codePrefix + config.Code, config.Dir, config.IsConfidential, config.OwningOrgId);
        return await RegisterProjectInLexBox(config, _adminApiTester, waitForRepoReady: true);
    }

    private static IDisposable DeleteWhenDone(FileInfo file) => Defer.Action(file.Delete);

    [Theory]
    [InlineData("")]
    [InlineData("1")]
    public async Task CreateProject_HasEmptyServableRepo(string codePrefix)
    {
        await using var project = await CreateProject(codePrefix);
        project.Code.Should().StartWith(codePrefix);

        await _adminApiTester.AssertRepoEmpty(project.Code);
        var state = await _adminApiTester.GetProjectRepoState(project.Code);
        state.LastCommit.Should().BeNull();
        state.HasAbandonedTransactions.Should().BeFalse();
        state.ResetStatus.Should().Be("NONE");
    }

    /// <summary>
    /// Many tests compare against these constants, so if the template repo zip changes this is the test to fix first.
    /// </summary>
    [Fact]
    public async Task TemplateRepoConstants_MatchTheTemplateRepoZip()
    {
        await using var project = await CreateProject();
        await _adminApiTester.StartResetAndAssertEmpty(project.Code);
        await _fixture.FinishLexboxProjectResetWithTemplateRepo(project.Code);

        (await _adminApiTester.GetTip(project.Code)).Should().BeEquivalentTo(IntegrationFixture.TemplateRepoTip);
        (await _adminApiTester.GetProjectRepoState(project.Code)).LastCommit.Should().Be(IntegrationFixture.TemplateRepoLastCommit);
    }

    [Fact]
    public async Task CopyToNewProject_CopiesRepo()
    {
        var sourceCode = TestingEnvironmentVariables.ProjectCode;
        var sourceTip = await _adminApiTester.GetTip(sourceCode);
        sourceTip.IsEmpty.Should().BeFalse();

        var copyCode = NewProjectCode();
        var response = await _adminApiTester.HttpClient.PostAsync(
            $"api/Testing/copyToNewProject?newProjectCode={copyCode}&existingProjectCode={sourceCode}", null);
        response.ShouldBeSuccessful();
        var copyId = await response.Content.ReadFromJsonAsync<Guid>();
        await using var cleanup = Defer.Async(async () =>
        {
            // not asserted, so a failure here can't hide the actual test failure
            await _adminApiTester.HttpClient.DeleteAsync($"api/project/{copyId}");
        });
        await WaitForHgRefreshIntervalAsync();

        (await _adminApiTester.GetTip(copyCode)).Should().BeEquivalentTo(sourceTip);
        (await _adminApiTester.GetTip(sourceCode)).Should().BeEquivalentTo(sourceTip, "the source repo should be unchanged");
    }

    [Fact]
    public async Task BackupProject_ZipRestoresTheSameRepo()
    {
        await using var source = await CreateProject();
        await _fixture.PopulateWithTemplateRepo(source.Code);

        var backup = await _adminApiTester.DownloadBackup(source.Code);
        ZipEntryNames(backup).Should().Contain(".hg/store/00changelog.i");
        (await _adminApiTester.GetTip(source.Code)).Should().BeEquivalentTo(IntegrationFixture.TemplateRepoTip,
            "taking a backup should not modify the repo");

        await using var target = await CreateProject();
        await _adminApiTester.StartResetAndAssertEmpty(target.Code);
        var backupFile = await SaveTempZip(backup, source.Code);
        using (DeleteWhenDone(backupFile))
        {
            await _fixture.FinishLexboxProjectResetWithRepo(target.Code, backupFile);
        }

        (await _adminApiTester.GetTip(target.Code)).Should().BeEquivalentTo(IntegrationFixture.TemplateRepoTip);
        await _adminApiTester.AssertRepoContainsOnlyHgFolder(target.Code);
        var state = await _adminApiTester.GetProjectRepoState(target.Code);
        state.LastCommit.Should().Be(IntegrationFixture.TemplateRepoLastCommit);
        state.ResetStatus.Should().Be("NONE");
    }

    [Fact]
    public async Task BackupProject_UnknownProjectCode_ReturnsNotFound()
    {
        var response = await _adminApiTester.HttpClient.GetAsync($"api/project/backupProject/{NewProjectCode()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task BackupProject_OfEmptyRepo_RestoresAnEmptyRepo()
    {
        await using var source = await CreateProject();
        await _fixture.PopulateWithTemplateRepo(source.Code);
        await _adminApiTester.StartResetAndAssertEmpty(source.Code);
        await _adminApiTester.FinishLexboxProjectResetWithoutUpload(source.Code);
        await _adminApiTester.AssertRepoEmpty(source.Code);

        var backup = await _adminApiTester.DownloadBackup(source.Code);
        var entryNames = ZipEntryNames(backup);
        entryNames.Should().Contain(".hg/requires");
        entryNames.Should().NotContain(".hg/store/00changelog.i");

        await using var target = await CreateProject();
        await _fixture.PopulateWithTemplateRepo(target.Code);
        await _adminApiTester.StartResetAndAssertEmpty(target.Code);
        var backupFile = await SaveTempZip(backup, source.Code);
        using (DeleteWhenDone(backupFile))
        {
            await _fixture.FinishLexboxProjectResetWithRepo(target.Code, backupFile);
        }

        await _adminApiTester.AssertRepoEmpty(target.Code);
        var state = await _adminApiTester.GetProjectRepoState(target.Code);
        state.LastCommit.Should().BeNull();
        state.ResetStatus.Should().Be("NONE");
    }

    [Fact]
    public async Task ResetProject_WithoutUpload_LeavesEmptyRepo()
    {
        await using var project = await CreateProject();
        await _fixture.PopulateWithTemplateRepo(project.Code);
        var oldTipUrl = $"hg/{project.Code}/file/{IntegrationFixture.TemplateRepoTip.Node}?style=json-lex";
        (await _adminApiTester.SendWithoutRetry(HttpMethod.Get, oldTipUrl)).Status.Should().Be(HttpStatusCode.OK);

        await _adminApiTester.StartResetAndAssertEmpty(project.Code);
        await _adminApiTester.FinishLexboxProjectResetWithoutUpload(project.Code);

        await _adminApiTester.AssertRepoEmpty(project.Code);
        var state = await _adminApiTester.GetProjectRepoState(project.Code);
        state.LastCommit.Should().BeNull();
        state.RepoSizeInKb.Should().Be(0);
        state.ResetStatus.Should().Be("NONE");
        // the old history must be gone, not just hidden behind a new tip
        var oldTipResponse = await _adminApiTester.SendWithoutRetry(HttpMethod.Get, oldTipUrl);
        oldTipResponse.Status.Should().Be(HttpStatusCode.NotFound, oldTipResponse.Body);
    }

    [Fact]
    public async Task FinishResetProject_WithoutStartingReset_FailsAndLeavesRepoUnchanged()
    {
        await using var project = await CreateProject();
        await _fixture.PopulateWithTemplateRepo(project.Code);

        var response = await _adminApiTester.SendWithoutRetry(HttpMethod.Post, $"api/project/finishResetProject/{project.Code}");

        ((int)response.Status).Should().BeGreaterThanOrEqualTo(400, response.Body);
        response.GetAppErrorCode().Should().Be("RESET_NOT_STARTED", response.Body);
        (await _adminApiTester.GetTip(project.Code)).Should().BeEquivalentTo(IntegrationFixture.TemplateRepoTip);
        (await _adminApiTester.GetProjectRepoState(project.Code)).ResetStatus.Should().Be("NONE");
    }

    [Fact]
    public async Task ResetProject_WhileResetInProgress_FailsAndFirstResetCanStillFinish()
    {
        await using var project = await CreateProject();
        await _fixture.PopulateWithTemplateRepo(project.Code);
        await _adminApiTester.StartResetAndAssertEmpty(project.Code);
        await Task.Delay(1100); // make sure a (wrong) second backup wouldn't fail just because of a name collision

        var response = await _adminApiTester.SendWithoutRetry(HttpMethod.Post, $"api/project/resetProject/{project.Code}");

        // The guard's message ("not ready for reset or already reset") isn't in the response body outside of Development,
        // so the state checks below are what show the reset in progress wasn't disturbed
        ((int)response.Status).Should().BeGreaterThanOrEqualTo(400, response.Body);
        (await _adminApiTester.GetProjectRepoState(project.Code)).ResetStatus.Should().Be("IN_PROGRESS");
        await _adminApiTester.AssertRepoEmpty(project.Code);

        await _fixture.FinishLexboxProjectResetWithTemplateRepo(project.Code);
        (await _adminApiTester.GetTip(project.Code)).Should().BeEquivalentTo(IntegrationFixture.TemplateRepoTip);
        (await _adminApiTester.GetProjectRepoState(project.Code)).ResetStatus.Should().Be("NONE");
    }

    [Fact]
    public async Task ResetProject_UploadZipWithHgAtRoot_ReplacesRepoAndUpdatesMetadata()
    {
        await using var project = await CreateProject();
        await _adminApiTester.StartResetAndAssertEmpty(project.Code);
        var stateDuringReset = await _adminApiTester.GetProjectRepoState(project.Code);
        stateDuringReset.LastCommit.Should().BeNull();
        stateDuringReset.RepoSizeInKb.Should().Be(0);

        await _fixture.FinishLexboxProjectResetWithTemplateRepo(project.Code);

        (await _adminApiTester.GetTip(project.Code)).Should().BeEquivalentTo(IntegrationFixture.TemplateRepoTip);
        // the template zip also contains the working copy files
        await _adminApiTester.AssertRepoContainsOnlyHgFolder(project.Code);
        // FinishReset updates the metadata before the upload request completes
        var state = await _adminApiTester.GetProjectRepoState(project.Code);
        state.LastCommit.Should().Be(IntegrationFixture.TemplateRepoLastCommit);
        state.RepoSizeInKb.Should().BeGreaterThan(0);
        state.ResetStatus.Should().Be("NONE");
        state.HasAbandonedTransactions.Should().BeFalse();
    }

    [Fact]
    public async Task ResetProject_UploadZipWithNestedHgAndWorkingFiles_UsesTheNestedRepo()
    {
        var zip = MakeZipFromTemplateRepo(
            name => $"some-folder/project/{name}",
            new Dictionary<string, string>
            {
                ["readme.txt"] = "a file next to the project folder",
                ["some-folder/notes.txt"] = "a file in the folder above the repo",
                ["some-folder/project/untracked.txt"] = "a working copy file that isn't in the repo",
            });
        using var deleteZip = DeleteWhenDone(zip);
        await using var project = await CreateProject();
        await _adminApiTester.StartResetAndAssertEmpty(project.Code);

        await _fixture.FinishLexboxProjectResetWithRepo(project.Code, zip);

        (await _adminApiTester.GetTip(project.Code)).Should().BeEquivalentTo(IntegrationFixture.TemplateRepoTip);
        await _adminApiTester.AssertRepoContainsOnlyHgFolder(project.Code);
        var state = await _adminApiTester.GetProjectRepoState(project.Code);
        state.LastCommit.Should().Be(IntegrationFixture.TemplateRepoLastCommit);
        state.ResetStatus.Should().Be("NONE");
    }

    [Fact]
    public async Task ResetProject_UploadZipWithoutHg_FailsAndAValidUploadCanFollow()
    {
        var zipWithoutHg = MakeZipFromTemplateRepo(name => name.StartsWith(".hg/") ? null : name);
        using var deleteZip = DeleteWhenDone(zipWithoutHg);
        await using var project = await CreateProject();
        await _adminApiTester.StartResetAndAssertEmpty(project.Code);

        var response = await _adminApiTester.UploadResetZipWithoutRetry(project.Code, zipWithoutHg);

        // the frontend only shows the error code of a 500 response
        response.Status.Should().Be(HttpStatusCode.InternalServerError, response.Body);
        response.GetAppErrorCode().Should().Be("ZIP_MISSING_HG_FOLDER");
        await _adminApiTester.AssertRepoEmpty(project.Code);
        (await _adminApiTester.GetProjectRepoState(project.Code)).ResetStatus.Should().Be("IN_PROGRESS");

        await _fixture.FinishLexboxProjectResetWithTemplateRepo(project.Code);
        (await _adminApiTester.GetTip(project.Code)).Should().BeEquivalentTo(IntegrationFixture.TemplateRepoTip);
        (await _adminApiTester.GetProjectRepoState(project.Code)).ResetStatus.Should().Be("NONE");
    }

    /// <summary>
    /// Only covers the dry run listing: reset backups are kept for at least 5 days,
    /// so a backup old enough to be listed (or deleted) can't be produced through the public API.
    /// </summary>
    [Fact]
    public async Task CleanupResetBackups_DryRun_ReturnsNamesWithoutDeleting()
    {
        await using var project = await CreateProject();
        await _fixture.PopulateWithTemplateRepo(project.Code);
        // this reset backs up the template repo
        await _adminApiTester.StartResetAndAssertEmpty(project.Code);
        await _adminApiTester.FinishLexboxProjectResetWithoutUpload(project.Code);

        var wouldDelete = await DryRunCleanupResetBackups();

        wouldDelete.Should().NotContain(name => name.StartsWith(project.Code + "__"));
        var minimumAge = DateTimeOffset.UtcNow - TimeSpan.FromDays(5);
        wouldDelete.Should().AllSatisfy(name =>
        {
            HgService.ResetProjectsRegex().IsMatch(name).Should().BeTrue($"{name} should be a reset backup");
            HgService.GetResetDate(name).Should().BeBefore(minimumAge);
        });
        // BeSubsetOf rather than Contain, which throws when nothing is old enough to list (always the case in CI)
        wouldDelete.Should().BeSubsetOf(await DryRunCleanupResetBackups(), "a dry run should not delete anything");
    }

    private async Task<string[]> DryRunCleanupResetBackups()
    {
        var response = await _adminApiTester.HttpClient.GetAsync("api/Testing/test-cleanup-reset-backups?dryRun=true");
        response.ShouldBeSuccessful();
        var names = await response.Content.ReadFromJsonAsync<string[]>();
        names.Should().NotBeNull();
        return names;
    }
}
