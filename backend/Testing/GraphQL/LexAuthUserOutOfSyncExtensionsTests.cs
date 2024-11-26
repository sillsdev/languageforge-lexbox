using LexBoxApi.GraphQL;
using LexCore.Auth;
using LexCore.Entities;
using FluentAssertions;

namespace Testing.GraphQL;

public class LexAuthUserOutOfSyncExtensionsTests
{
    private static readonly LexAuthUser user = new()
    {
        Id = Guid.NewGuid(),
        Name = "Test User",
        Role = UserRole.user,
        Projects = [],
        Locale = "en",
    };

    [Fact]
    public void DetectsUserAddedToProject()
    {
        var project = NewProject();
        user.IsOutOfSyncWithProject(project).Should().BeFalse();

        project.Users.Add(new() { UserId = user.Id, Role = ProjectRole.Editor });
        user.IsOutOfSyncWithProject(project).Should().BeTrue();
    }

    [Fact]
    public void DetectsUserRemovedFromProject()
    {
        var project = NewProject();
        project.Users.Add(new() { UserId = user.Id, Role = ProjectRole.Editor });
        var editorUser = user with { Projects = [new AuthUserProject(ProjectRole.Editor, project.Id)] };
        editorUser.IsOutOfSyncWithProject(project).Should().BeFalse();

        project.Users.Clear();
        editorUser.IsOutOfSyncWithProject(project).Should().BeTrue();
    }

    [Fact]
    public void DetectsUserProjectRoleChanged()
    {
        var project = NewProject();
        var projectUser = new ProjectUsers { UserId = user.Id, Role = ProjectRole.Editor };
        project.Users.Add(projectUser);
        var editorUser = user with { Projects = [new AuthUserProject(ProjectRole.Editor, project.Id)] };
        editorUser.IsOutOfSyncWithProject(project).Should().BeFalse();

        projectUser.Role = ProjectRole.Manager;
        editorUser.IsOutOfSyncWithProject(project).Should().BeTrue();
    }

    [Fact]
    public void DoesNotDetectsUserProjectRoleChangedIfRolesNotAvailable()
    {
        var project = NewProject();
        var projectUser = new ProjectUsers { UserId = user.Id, Role = ProjectRole.Unknown };
        project.Users.Add(projectUser);
        var editorUser = user with { Projects = [new AuthUserProject(ProjectRole.Editor, project.Id)] };
        editorUser.IsOutOfSyncWithProject(project).Should().BeFalse(); // might be out of sync, but we can't tell
    }

    [Fact]
    public void DoesNotDetectChangesWithoutProjectUsersIfNotMyProject()
    {
        var project = NewProject();
        // simulate Users not projected in GQL query
        project.Users = null!;

        var editorUser = user with { Projects = [new AuthUserProject(ProjectRole.Editor, project.Id)] };
        editorUser.IsOutOfSyncWithProject(project).Should().BeFalse(); // might be out of sync, but we can't tell
    }

    [Fact]
    public void DetectsAddedToMyProjectWithoutProjectUsers()
    {
        var project = NewProject();
        // simulate Users not projected in GQL query
        project.Users = null!;

        user.IsOutOfSyncWithProject(project).Should().BeFalse(); // might be out of sync, but we can't tell
        user.IsOutOfSyncWithProject(project, isMyProject: true).Should().BeTrue();
        user.IsOutOfSyncWithMyProjects([project]).Should().BeTrue();
    }

    [Fact]
    public void DetectsRemovedFromMyProjectWithoutProjectUsers()
    {
        var project = NewProject();
        // simulate Users not projected in GQL query
        project.Users = null!;

        var editorUser = user with { Projects = [new AuthUserProject(ProjectRole.Editor, project.Id)] };
        editorUser.IsOutOfSyncWithProject(project).Should().BeFalse(); // might be out of sync, but we can't tell
        editorUser.IsOutOfSyncWithMyProjects([]).Should().BeTrue();
    }

    [Fact]
    public void DetectsUserAddedToOrg()
    {
        var org = NewOrg();
        user.IsOutOfSyncWithOrg(org).Should().BeFalse();

        org.Members.Add(new() { UserId = user.Id, Role = OrgRole.User });
        user.IsOutOfSyncWithOrg(org).Should().BeTrue();
    }

    [Fact]
    public void DetectsUserRemovedFromOrg()
    {
        var org = NewOrg();
        org.Members.Add(new() { UserId = user.Id, Role = OrgRole.User });
        var editorUser = user with { Orgs = [new AuthUserOrg(OrgRole.User, org.Id)] };
        editorUser.IsOutOfSyncWithOrg(org).Should().BeFalse();

        org.Members.Clear();
        editorUser.IsOutOfSyncWithOrg(org).Should().BeTrue();
    }

    [Fact]
    public void DetectsUserOrgRoleChanged()
    {
        var org = NewOrg();
        var orgUser = new OrgMember { UserId = user.Id, Role = OrgRole.User };
        org.Members.Add(orgUser);
        var editorUser = user with { Orgs = [new AuthUserOrg(OrgRole.User, org.Id)] };
        editorUser.IsOutOfSyncWithOrg(org).Should().BeFalse();

        orgUser.Role = OrgRole.Admin;
        editorUser.IsOutOfSyncWithOrg(org).Should().BeTrue();
    }

    [Fact]
    public void DoesNotDetectsUserOrgRoleChangedIfRolesNotAvailable()
    {
        var org = NewOrg();
        var orgUser = new OrgMember { UserId = user.Id, Role = OrgRole.Unknown };
        org.Members.Add(orgUser);
        var editorUser = user with { Orgs = [new AuthUserOrg(OrgRole.User, org.Id)] };
        editorUser.IsOutOfSyncWithOrg(org).Should().BeFalse(); // might be out of sync, but we can't tell
    }

    [Fact]
    public void DetectsChangesWithOrgProjects()
    {
        var org = NewOrg();
        var project = NewProject();
        org.Projects = [project];
        user.IsOutOfSyncWithOrg(org).Should().BeFalse();

        project.Users.Add(new() { UserId = user.Id, Role = ProjectRole.Editor });
        user.IsOutOfSyncWithOrg(org).Should().BeTrue();
    }

    [Fact]
    public void DoesNotDetectChangesWithoutOrgMembersIfNotMyOrg()
    {
        var org = NewOrg();
        // simulate Members not projected in GQL query
        org.Members = null!;

        var editorUser = user with { Orgs = [new AuthUserOrg(OrgRole.User, org.Id)] };
        editorUser.IsOutOfSyncWithOrg(org).Should().BeFalse(); // might be out of sync, but we can't tell
    }

    [Fact]
    public void DetectsAddedToMyOrgWithoutOrgMembers()
    {
        var org = NewOrg();
        // simulate Members not projected in GQL query
        org.Members = null!;

        user.IsOutOfSyncWithOrg(org).Should().BeFalse(); // might be out of sync, but we can't tell
        user.IsOutOfSyncWithOrg(org, isMyOrg: true).Should().BeTrue();
        user.IsOutOfSyncWithMyOrgs([org]).Should().BeTrue();
    }

    [Fact]
    public void DetectsRemovedFromMyOrgWithoutOrgMembers()
    {
        var org = NewOrg();
        // simulate Members not projected in GQL query
        org.Members = null!;

        var editorUser = user with { Orgs = [new AuthUserOrg(OrgRole.User, org.Id)] };
        editorUser.IsOutOfSyncWithOrg(org).Should().BeFalse(); // might be out of sync, but we can't tell
        editorUser.IsOutOfSyncWithMyOrgs([]).Should().BeTrue();
    }

    private static Project NewProject()
    {
        return new()
        {
            Id = Guid.NewGuid(),
            Name = "Project 1",
            Code = "project1",
            Type = ProjectType.FLEx,
            Users = [],
            Organizations = [],
            IsConfidential = false,
            LastCommit = DateTimeOffset.UtcNow,
            RetentionPolicy = RetentionPolicy.Dev,
        };
    }

    private static Organization NewOrg()
    {
        return new()
        {
            Id = Guid.NewGuid(),
            Name = "Organization 1",
            Members = [],
            Projects = [],
        };
    }
}
