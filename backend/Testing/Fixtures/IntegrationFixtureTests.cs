using Moq;
using Shouldly;
using Testing.ApiTests;
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
        await fixture.InitializeAsync(Mock.Of<ApiTestBase>());
        IntegrationFixture.TemplateRepo.EnumerateFiles()
            .Select(f => f.Name)
            .ShouldContain("kevin-test-01.fwdata");
    }

    [Fact]
    public async Task CanFindTheProjectZipFile()
    {
        var fixture = new IntegrationFixture();
        await fixture.InitializeAsync(Mock.Of<ApiTestBase>());
        IntegrationFixture.TemplateRepoZip
            .Directory!.EnumerateFiles().Select(f => f.Name)
            .ShouldContain(IntegrationFixture.TemplateRepoZip.Name);
    }

    [Fact]
    public async Task CanInitFlexProjectRepo()
    {
        var fixture = new IntegrationFixture();
        await fixture.InitializeAsync(Mock.Of<ApiTestBase>());
        var projectConfig = fixture.InitLocalFlexProjectWithRepo();
        Directory.EnumerateFiles(projectConfig.Dir)
            .ShouldContain(projectConfig.FwDataFile);
    }

    [Fact]
    public async Task InitCleansUpPreviousRun()
    {
        var fixture = new IntegrationFixture();
        await fixture.InitializeAsync(Mock.Of<ApiTestBase>());
        var filePath = Path.Join(Constants.BasePath, "test.txt");
        await File.WriteAllTextAsync(filePath, "test");
        Directory.EnumerateFiles(Constants.BasePath).ShouldContain(filePath);

        await fixture.InitializeAsync();
        Directory.EnumerateFiles(Constants.BasePath).ShouldNotContain(filePath);
    }
}
