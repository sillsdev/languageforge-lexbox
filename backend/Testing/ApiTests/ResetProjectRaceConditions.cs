using Shouldly;
using Testing.Fixtures;
using static Testing.Services.Utils;

namespace Testing.ApiTests;

// Issue: https://github.com/sillsdev/languageforge-lexbox/issues/728
// Sadly, this does not reproduce the 404, but I still think it's a decent test
[Trait("Category", "Integration")]
public class ResetPojectRaceCondition : IClassFixture<IntegrationFixture>
{

    private readonly IntegrationFixture _fixture;
    private readonly ApiTestBase _adminApiTester;

    public ResetPojectRaceCondition(IntegrationFixture fixture)
    {
        _fixture = fixture;
        _adminApiTester = _fixture.AdminApiTester;
    }

    [Fact]
    public async Task SimultaneousResetsDontResultIn404s()
    {
        // Create projects on server
        var config1 = GetNewProjectConfig();
        var config2 = GetNewProjectConfig();
        var config3 = GetNewProjectConfig();

        await using var project1 = await RegisterProjectInLexBox(config1, _adminApiTester);
        await using var project2 = await RegisterProjectInLexBox(config2, _adminApiTester);
        await using var project3 = await RegisterProjectInLexBox(config3, _adminApiTester);

        var lastCommitBefore1 = await _adminApiTester.GetProjectLastCommit(config1.Code);
        var lastCommitBefore2 = await _adminApiTester.GetProjectLastCommit(config2.Code);
        var lastCommitBefore3 = await _adminApiTester.GetProjectLastCommit(config3.Code);

        lastCommitBefore1.ShouldBeNullOrWhiteSpace();
        lastCommitBefore2.ShouldBeNullOrWhiteSpace();
        lastCommitBefore3.ShouldBeNullOrWhiteSpace();

        // Reset and fill projects on server
        var newLastCommits = await Task.WhenAll(
            DoFullProjectResetAndVerifyLastCommit(config1.Code),
            DoFullProjectResetAndVerifyLastCommit(config2.Code),
            DoFullProjectResetAndVerifyLastCommit(config3.Code)
        );

        newLastCommits[0].ShouldNotBeNullOrWhiteSpace();
        newLastCommits[0].ShouldBe(newLastCommits[1]);
        newLastCommits[0].ShouldBe(newLastCommits[2]);

        // we need a short delay between resets or we'll get naming collisions on the backups of the reset projects
        await Task.Delay(1000);

        // Reset and fill projects on server
        var templateRepoLastCommit = newLastCommits[0];
        newLastCommits = await Task.WhenAll(
            DoFullProjectResetAndVerifyLastCommit(config1.Code, templateRepoLastCommit),
            DoFullProjectResetAndVerifyLastCommit(config2.Code, templateRepoLastCommit),
            DoFullProjectResetAndVerifyLastCommit(config3.Code, templateRepoLastCommit)
        );
    }

    private async Task<string?> DoFullProjectResetAndVerifyLastCommit(string projectCode, string? expectedLastCommit = null)
    {
        await _adminApiTester.StartLexboxProjectReset(projectCode);
        var lastCommitBefore = await _adminApiTester.GetProjectLastCommit(projectCode);
        lastCommitBefore.ShouldBeNullOrWhiteSpace();
        await _fixture.FinishLexboxProjectResetWithTemplateRepo(projectCode);
        var lastCommit = await _adminApiTester.GetProjectLastCommit(projectCode);
        if (expectedLastCommit is not null) lastCommit.ShouldBe(expectedLastCommit);
        else lastCommit.ShouldNotBeNullOrWhiteSpace();
        return lastCommit;
    }
}
