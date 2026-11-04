using LcmCrdt.Project;
using LcmCrdt.Tests.MiniLcmTests;

namespace LcmCrdt.Tests;

public class UpdateUserRoleTests(MiniLcmApiFixture fixture) : IClassFixture<MiniLcmApiFixture>
{
    [Fact]
    public async Task UpdateUserRole_ReportsWhetherTheRoleChanged()
    {
        var projectService = fixture.GetService<CurrentProjectService>();
        await projectService.UpdateUserRole(UserProjectRole.Editor);

        (await projectService.UpdateUserRole(UserProjectRole.Editor)).Should().BeFalse("the role is unchanged");
        (await projectService.UpdateUserRole(UserProjectRole.Observer)).Should().BeTrue();
    }

    [Fact]
    public async Task UpdateUserRole_UpdatesTheSharedProjectDataCache()
    {
        // MiniLcmJsInvokable reads write access from this cache, so it must see a role change made in another scope (sync)
        var projectService = fixture.GetService<CurrentProjectService>();
        await projectService.UpdateUserRole(UserProjectRole.Editor);

        await projectService.UpdateUserRole(UserProjectRole.Observer);

        fixture.GetService<ProjectDataCache>().CachedProjectData(projectService.Project)!.Role
            .Should().Be(UserProjectRole.Observer);
    }
}
