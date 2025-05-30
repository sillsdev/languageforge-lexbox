using LexCore.Auth;
using LexCore.Entities;

namespace LexBoxApi.GraphQL;

public static class LexAuthUserOutOfSyncExtensions
{
    public static bool IsOutOfSyncWithMyProjects(this LexAuthUser user, IReadOnlyCollection<Project> myProjects)
    {
        if (user.IsAdmin) return false; // admins don't have projects in their token
        if (user.Projects.Length != myProjects.Count) return true; // different number of projects
        return myProjects.Any(proj => user.IsOutOfSyncWithProject(proj, isMyProject: true));
    }

    public static bool IsOutOfSyncWithMyProjects(this LexAuthUser user, ICollection<Guid> projectIds)
    {
        if (user.IsAdmin) return false; // admins don't have projects in their token
        if (user.Projects.Length != projectIds.Count) return true; // different number of projects
        return user.Projects.Select(p => p.ProjectId).Intersect(projectIds).Count() != projectIds.Count;
    }

    public static bool IsOutOfSyncWithMyProjects(this LexAuthUser user, ICollection<FieldWorksLiteProject> projects)
    {
        if (user.IsAdmin) return false; // admins don't have projects in their token
        if (user.Projects.Length != projects.Count) return true; // different number of projects
        return projects.Any(p =>
        {
            var tokenMembership = user.Projects.SingleOrDefault(p2 => p2.ProjectId == p.Id);
            return p.Role != tokenMembership?.Role;
        });
    }

    public static bool IsOutOfSyncWithMyOrgs(this LexAuthUser user, List<Organization> myOrgs)
    {
        if (user.IsAdmin) return false; // admins don't have orgs in their token
        if (user.Orgs.Length != myOrgs.Count) return true; // different number of orgs
        return myOrgs.Any(org => user.IsOutOfSyncWithOrg(org, isMyOrg: true));
    }

    public static bool IsOutOfSyncWithProject(this LexAuthUser user, Project project, bool isMyProject = false)
    {
        if (user.IsAdmin) return false; // admins don't have projects in their token

        var tokenMembership = user.Projects.SingleOrDefault(p => p.ProjectId == project.Id);

        if (project.Users is null)
        {
            if (tokenMembership is null && isMyProject) return true; // we know we're supposed to be a member
            return false; // otherwise, we can't detect differences without users available
        }

        var dbMembership = project.Users.SingleOrDefault(u => u.UserId == user.Id);

        if (tokenMembership is null && dbMembership is null) return false; // both null: they're the same
        if (tokenMembership is null || dbMembership is null) return true; // only 1 is null: they're different

        var projectRolesAvailable = project.Users.Any(u => u.Role is not ProjectRole.Unknown);
        if (!projectRolesAvailable) return false; // we can't detect differences without roles available

        return tokenMembership.Role != dbMembership.Role;
    }

    public static bool IsOutOfSyncWithOrg(this LexAuthUser user, Organization org, bool isMyOrg = false)
    {
        if (user.IsAdmin) return false; // admins don't have orgs in their token
        if (org.Projects?.Any(project => user.IsOutOfSyncWithProject(project)) ?? false) return true;

        var tokenMembership = user.Orgs.SingleOrDefault(o => o.OrgId == org.Id);

        if (org.Members is null)
        {
            if (tokenMembership is null && isMyOrg) return true; // we know we're supposed to be a member
            return false; // otherwise, we can't detect differences without members available
        }

        var dbMembership = org.Members.SingleOrDefault(m => m.UserId == user.Id);

        if (tokenMembership is null && dbMembership is null) return false; // both null: they're the same
        if (tokenMembership is null || dbMembership is null) return true; // only 1 is null: they're different

        var orgRolesAvailable = org.Members.Any(u => u.Role is not OrgRole.Unknown);
        if (!orgRolesAvailable) return false; // we can't detect differences without roles available

        return tokenMembership.Role != dbMembership.Role;
    }
}
