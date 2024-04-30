using Moq;
using Shouldly;
using Testing.ApiTests;

namespace Testing.Fixtures;

/// <summary>
/// our test code is getting complex enough that we need to test it on it's own
/// </summary>
public class IntegrationFixtureTests
{
    // A static shared instance for the whole test class, because that's how xunit uses fixtures
    private static readonly IntegrationFixture fixture = new();

    [Fact]
    public async Task InitCreatesARepoWithTheProject()
    {
        await fixture.InitializeAsync(Mock.Of<ApiTestBase>());
        IntegrationFixture.TemplateRepo.EnumerateFiles()
            .Select(f => f.Name)
            .ShouldContain("kevin-test-01.fwdata");
    }

    [Fact]
    public async Task CanFindTheProjectZipFile()
    {
        await fixture.InitializeAsync(Mock.Of<ApiTestBase>());
        IntegrationFixture.TemplateRepoZip
            .Directory!.EnumerateFiles().Select(f => f.Name)
            .ShouldContain(IntegrationFixture.TemplateRepoZip.Name);
    }

    [Fact]
    public async Task CanInitFlexProjectRepo()
    {
        await fixture.InitializeAsync(Mock.Of<ApiTestBase>());
        var projectConfig = fixture.InitLocalFlexProjectWithRepo();
        Directory.EnumerateFiles(projectConfig.Dir)
            .ShouldContain(projectConfig.FwDataFile);
    }
}
