namespace LcmCrdt.Tests;

public class CurrentProjectServiceTests(MiniLcmApiFixture fixture) : IClassFixture<MiniLcmApiFixture>
{
    private CurrentProjectService ProjectService => fixture.GetService<CurrentProjectService>();

    [Fact]
    public async Task UpdateOriginUser_WithNullUser_KeepsPersistedValue()
    {
        // Open-time resolution falls back to this persisted value when auth is null, so a null update mustn't wipe it.
        await ProjectService.UpdateOriginUser("Tim Haasdyk", "tim-id");

        await ProjectService.UpdateOriginUser(null, null);

        var projectData = await ProjectService.GetProjectData();
        projectData.OriginUserName.Should().Be("Tim Haasdyk");
        projectData.OriginUserId.Should().Be("tim-id");
    }

    [Fact]
    public async Task UpdateOriginUser_ReplacesPreviousUser()
    {
        await ProjectService.UpdateOriginUser("first", "first-id");

        await ProjectService.UpdateOriginUser("second", "second-id");

        var projectData = await ProjectService.GetProjectData();
        projectData.OriginUserName.Should().Be("second");
        projectData.OriginUserId.Should().Be("second-id");
    }
}
