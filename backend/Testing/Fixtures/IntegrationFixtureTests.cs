using Moq;
using FluentAssertions;
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
            .Should().Contain("kevin-test-01.fwdata");
    }

    [Fact]
    public async Task CanFindTheProjectZipFile()
    {
        await fixture.InitializeAsync(Mock.Of<ApiTestBase>());
        IntegrationFixture.TemplateRepoZip
            .Directory!.EnumerateFiles().Select(f => f.Name)
            .Should().Contain(IntegrationFixture.TemplateRepoZip.Name);
    }

    [Fact]
    public async Task CanInitFlexProjectRepo()
    {
        await fixture.InitializeAsync(Mock.Of<ApiTestBase>());
        var projectConfig = fixture.InitLocalFlexProjectWithRepo();
        Directory.EnumerateFiles(projectConfig.Dir)
            .Should().Contain(projectConfig.FwDataFile);
    }
}
