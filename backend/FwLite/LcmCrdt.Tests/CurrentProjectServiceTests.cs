namespace LcmCrdt.Tests;

public class CurrentProjectServiceTests(MiniLcmApiFixture fixture) : IClassFixture<MiniLcmApiFixture>
{
    private CurrentProjectService ProjectService => fixture.GetService<CurrentProjectService>();

    [Fact]
    public async Task UpdateLastUser_WithNullUser_KeepsPersistedValue()
    {
        // Open-time resolution falls back to this persisted value when auth is null, so a null update mustn't wipe it.
        await ProjectService.UpdateLastUser("Tim Haasdyk", "tim-id");

        await ProjectService.UpdateLastUser(null, null);

        var projectData = await ProjectService.GetProjectData();
        projectData.LastUserName.Should().Be("Tim Haasdyk");
        projectData.LastUserId.Should().Be("tim-id");
    }

    [Fact]
    public async Task UpdateLastUser_ReplacesPreviousUser()
    {
        await ProjectService.UpdateLastUser("first", "first-id");

        await ProjectService.UpdateLastUser("second", "second-id");

        var projectData = await ProjectService.GetProjectData();
        projectData.LastUserName.Should().Be("second");
        projectData.LastUserId.Should().Be("second-id");
    }
}
