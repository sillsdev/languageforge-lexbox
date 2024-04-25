using Shouldly;
using Testing.Services;

namespace Testing.Fixtures;

/// <summary>
/// our test code is getting complex enough that we need to test it on it's own
/// </summary>
public class IntegrationFixtureTests
{
    [Fact]
    public async Task InitCreatesARepoWithTheProject()
    {
        var fixture = new IntegrationFixture();
        await fixture.InitializeAsync();
        fixture.TemplateRepo.EnumerateFiles()
            .Select(f => f.Name)
            .ShouldContain("kevin-test-01.fwdata");
    }

    [Fact]
    public async Task CanFindTheProjectZipFile()
    {
        var fixture = new IntegrationFixture();
        await fixture.InitializeAsync();
        fixture.TemplateRepoZip
            .Directory!.EnumerateFiles().Select(f => f.Name)
            .ShouldContain(fixture.TemplateRepoZip.Name);
    }

    [Fact]
    public async Task CanInitFlexProjectRepo()
    {
        var fixture = new IntegrationFixture();
        await fixture.InitializeAsync();
        var projectConfig = fixture.InitLocalFlexProjectWithRepo();
        Directory.EnumerateFiles(projectConfig.Dir)
            .ShouldContain(projectConfig.FwDataFile);
    }

    [Fact]
    public async Task InitCleansUpPreviousRun()
    {
        var fixture = new IntegrationFixture();
        await fixture.InitializeAsync();
        var filePath = Path.Join(Constants.BasePath, "test.txt");
        await File.WriteAllTextAsync(filePath, "test");
        Directory.EnumerateFiles(Constants.BasePath).ShouldContain(filePath);

        await fixture.InitializeAsync();
        Directory.EnumerateFiles(Constants.BasePath).ShouldNotContain(filePath);
    }
}
