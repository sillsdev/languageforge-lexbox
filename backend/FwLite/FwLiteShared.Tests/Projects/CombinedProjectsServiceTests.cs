using FwLiteShared.Auth;
using FwLiteShared.Projects;
using LcmCrdt;

namespace FwLiteShared.Tests.Projects;

public class CombinedProjectsServiceTests
{
    private static readonly LexboxServer Staging = new(new Uri("https://staging.languagedepot.org"), "Staging");
    private static readonly LexboxServer Dev = new(new Uri("https://lexbox.dev.languagetechnology.org"), "Dev");

    private static ProjectData ProjectFrom(LexboxServer origin) =>
        new("Sena 3", "sena-3", Guid.NewGuid(), ProjectData.GetOriginDomain(origin.Authority), Guid.NewGuid());

    [Fact]
    public void ServerOwnsProject_TrueForOriginServer()
    {
        CombinedProjectsService.ServerOwnsProject(ProjectFrom(Staging), Staging).Should().BeTrue();
    }

    [Fact]
    public void ServerOwnsProject_FalseForOtherServerSharingTheGuid()
    {
        // A project downloaded from staging must not be claimed by dev when both list the same GUID.
        CombinedProjectsService.ServerOwnsProject(ProjectFrom(Staging), Dev).Should().BeFalse();
    }
}
