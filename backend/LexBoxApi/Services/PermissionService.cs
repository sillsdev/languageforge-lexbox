﻿using LexBoxApi.Auth;
using LexCore.Auth;
using LexCore.Entities;
using LexCore.ServiceInterfaces;

namespace LexBoxApi.Services;

public class PermissionService(
    LoggedInContext loggedInContext,
    ProjectService projectService)
    : IPermissionService
{
    private LexAuthUser? User => loggedInContext.MaybeUser;

    private async ValueTask<bool> ManagesOrgThatOwnsProject(Guid projectId, LexAuthUser? overrideUser = null)
    {
        var user = overrideUser ?? User;
        if (user is not null && user.Orgs.Any(o => o.Role == OrgRole.Admin))
        {
            // Org admins can view, edit, and sync all projects, even confidential ones
            var managedOrgIds = user.Orgs.Where(o => o.Role == OrgRole.Admin).Select(o => o.OrgId).ToHashSet();
            var projectOrgIds = await projectService.LookupProjectOrgIds(projectId);
            if (projectOrgIds.Any(managedOrgIds.Contains)) return true;
        }
        return false;
    }

    private async ValueTask<bool> IsMemberOfOrgThatOwnsProject(Guid projectId)
    {
        if (User is not null && User.Orgs.Any())
        {
            var memberOfOrgIds = User.Orgs.Select(o => o.OrgId).ToHashSet();
            var projectOrgIds = await projectService.LookupProjectOrgIds(projectId);
            if (projectOrgIds.Any(memberOfOrgIds.Contains)) return true;
        }
        return false;
    }

    public async ValueTask<bool> CanSyncProject(string projectCode)
    {
        if (User is null) return false;
        if (User.Role == UserRole.admin) return true;
        var projectId = await projectService.LookupProjectId(projectCode);
        if (projectId is null) return false;
        return await CanSyncProject(projectId.Value);
    }

    public bool IsProjectMember(Guid projectId, LexAuthUser? overrideUser = null)
    {
        var user = overrideUser ?? User;
        if (user is null) return false;
        return user.IsProjectMember(projectId);
    }

    public async ValueTask<bool> CanSyncProject(Guid projectId)
    {
        if (User is null) return false;
        if (User.Role == UserRole.admin) return true;
        if (User.IsProjectMember(projectId, ProjectRole.Editor, ProjectRole.Manager)) return true;
        // Org managers can sync any project owned by their org(s)
        return await ManagesOrgThatOwnsProject(projectId);
    }

    public async ValueTask<bool> CanDownloadProject(Guid projectId)
    {
        if (User is null) return false;
        if (User.Role == UserRole.admin) return true;
        if (User.IsProjectMember(projectId)) return true;
        // Org managers can sync any project owned by their org(s)
        return await ManagesOrgThatOwnsProject(projectId);
    }

    public async ValueTask AssertCanSyncProject(string projectCode)
    {
        if (!await CanSyncProject(projectCode)) throw new UnauthorizedAccessException();
    }

    public async ValueTask AssertCanSyncProject(Guid projectId)
    {
        if (!await CanSyncProject(projectId)) throw new UnauthorizedAccessException();
    }

    public async ValueTask AssertCanDownloadProject(Guid projectId)
    {
        if (!await CanDownloadProject(projectId)) throw new UnauthorizedAccessException();
    }

    public async ValueTask<bool> CanViewProject(Guid projectId, LexAuthUser? overrideUser = null)
    {
        var user = overrideUser ?? User;
        if (user is not null && user.Role == UserRole.admin) return true;
        if (user is not null && user.IsProjectMember(projectId)) return true;
        // Org admins can view all projects, even confidential ones
        if (await ManagesOrgThatOwnsProject(projectId, overrideUser)) return true;
        var isConfidential = await projectService.LookupProjectConfidentiality(projectId);
        if (isConfidential is null) return false; // Private by default
        return isConfidential == false; // Explicitly set to public
    }

    public async ValueTask AssertCanViewProject(Guid projectId, LexAuthUser? overrideUser = null)
    {
        if (!await CanViewProject(projectId, overrideUser)) throw new UnauthorizedAccessException();
    }

    public async ValueTask<bool> CanViewProject(string projectCode, LexAuthUser? overrideUser = null)
    {
        var user = overrideUser ?? User;
        if (user is not null && user.Role == UserRole.admin) return true;
        var projectId = await projectService.LookupProjectId(projectCode);
        if (projectId is null) return false;
        return await CanViewProject(projectId.Value, overrideUser);
    }

    public async ValueTask AssertCanViewProject(string projectCode, LexAuthUser? overrideUser = null)
    {
        if (!await CanViewProject(projectCode, overrideUser)) throw new UnauthorizedAccessException();
    }

    public async ValueTask<bool> CanViewProjectMembers(Guid projectId)
    {
        if (User is not null && User.Role == UserRole.admin) return true;
        // Project managers can view members of their own projects, even confidential ones
        if (await CanManageProject(projectId)) return true;
        // non members can't view project members
        if (User?.IsProjectMember(projectId) != true) return false;
        var isConfidential = await projectService.LookupProjectConfidentiality(projectId);
        // In this specific case (only), we assume public unless explicitly set to private
        return !(isConfidential ?? false);
    }

    public async ValueTask<bool> CanManageProject(Guid projectId)
    {
        if (User is null) return false;
        if (User.Role == UserRole.admin) return true;
        if (User.IsProjectMember(projectId, ProjectRole.Manager)) return true;
        return await ManagesOrgThatOwnsProject(projectId);
    }

    public async ValueTask AssertCanManageProject(Guid projectId)
    {
        if (!await CanManageProject(projectId)) throw new UnauthorizedAccessException();
    }

    public async ValueTask<bool> CanManageProject(string projectCode)
    {
        var projectId = await projectService.LookupProjectId(projectCode);
        if (projectId is null) return false;
        return await CanManageProject(projectId.Value);
    }

    public async ValueTask AssertCanManageProject(string projectCode)
    {
        if (!await CanManageProject(projectCode)) throw new UnauthorizedAccessException();
    }

    public async ValueTask AssertCanManageProjectMemberRole(Guid projectId, Guid userId)
    {
        if (User is null) throw new UnauthorizedAccessException();
        await AssertCanManageProject(projectId);
        if (User.Role != UserRole.admin && userId == User.Id)
            throw new UnauthorizedAccessException("Not allowed to change own project role.");
    }

    public async ValueTask<bool> CanCreateGuestUserInProject(Guid projectId)
    {
        if (User is null) return false;
        if (User.Role == UserRole.admin) return true;
        return await ManagesOrgThatOwnsProject(projectId);
    }

    public async ValueTask AssertCanCreateGuestUserInProject(Guid projectId)
    {
        if (!await CanCreateGuestUserInProject(projectId)) throw new UnauthorizedAccessException();
    }

    public bool CanCreateGuestUserInOrg(Guid? orgId)
    {
        if (User is null) return false;
        if (User.Role == UserRole.admin) return true;
        // Site admins can create guest users even with no org, anyone else (like org admins) must specify an org ID
        if (orgId is null) return false;
        return CanEditOrg(orgId.Value);
    }

    public void AssertCanCreateGuestUserInOrg(Guid? orgId)
    {
        if (!CanCreateGuestUserInOrg(orgId)) throw new UnauthorizedAccessException();
    }

    public async ValueTask<bool> CanAskToJoinProject(Guid projectId)
    {
        if (User is null) return false;
        if (User.IsAdmin) return true;
        return await IsMemberOfOrgThatOwnsProject(projectId);
    }

    public async ValueTask AssertCanAskToJoinProject(Guid projectId)
    {
        if (!await CanAskToJoinProject(projectId)) throw new UnauthorizedAccessException();
    }

    public async ValueTask<bool> CanAskToJoinProject(string projectCode)
    {
        var projectId = await projectService.LookupProjectId(projectCode);
        if (projectId is null) return false;
        return await CanAskToJoinProject(projectId.Value);
    }

    public async ValueTask AssertCanAskToJoinProject(string projectCode)
    {
        if (!await CanAskToJoinProject(projectCode)) throw new UnauthorizedAccessException();
    }

    public void AssertCanLockOrUnlockUser(Guid userId)
    {
        AssertIsAdmin();
        if (userId == User?.Id)
            throw new UnauthorizedAccessException("Not allowed to lock or unlock own account.");
    }

    public void AssertIsAdmin()
    {
        if (User is not { Role: UserRole.admin }) throw new UnauthorizedAccessException();
    }

    public void AssertCanDeleteAccount(Guid userId)
    {
        if (User is { Role: UserRole.admin } || User?.Id == userId)
            return;
        throw new UnauthorizedAccessException();
    }

    public bool HasProjectCreatePermission()
    {
        return User is { CanCreateProjects: true } or { Role: UserRole.admin };
    }

    public void AssertHasProjectCreatePermission()
    {
        if (!HasProjectCreatePermission()) throw new UnauthorizedAccessException();
    }

    public bool HasProjectRequestPermission()
    {
        return User is not ({ CreatedByAdmin: true } or { Email: null } or { EmailVerificationRequired: true });
    }

    public void AssertHasProjectRequestPermission()
    {
        if (!HasProjectRequestPermission()) throw new UnauthorizedAccessException();
    }

    public bool CanCreateOrg()
    {
        return User is {Role: UserRole.admin};
    }

    public void AssertCanCreateOrg()
    {
        //todo adjust permission
        if (!CanCreateOrg()) throw new UnauthorizedAccessException();
    }

    public bool IsOrgMember(Guid orgId, LexAuthUser? overrideUser = null)
    {
        var user = overrideUser ?? User;
        if (user is null) return false;
        if (user.Orgs.Any(o => o.OrgId == orgId)) return true;
        return false;
    }

    public bool CanEditOrg(Guid orgId, LexAuthUser? overrideUser = null)
    {
        var user = overrideUser ?? User;
        if (user is null) return false;
        if (user.Role == UserRole.admin) return true;
        if (user.Orgs.Any(o => o.OrgId == orgId && o.Role == OrgRole.Admin)) return true;
        return false;
    }

    public void AssertCanEditOrg(Guid orgId)
    {
        if (!CanEditOrg(orgId)) throw new UnauthorizedAccessException();
    }

    public void AssertCanEditOrg(Organization org)
    {
        if (!CanEditOrg(org.Id)) throw new UnauthorizedAccessException();
    }

    public void AssertCanAddProjectToOrg(Organization org)
    {
        if (User is null) throw new UnauthorizedAccessException();
        if (User.Role == UserRole.admin) return;
        if (org.Members.Any(m => m.UserId == User.Id)) return;
        throw new UnauthorizedAccessException();
    }

    public async ValueTask AssertCanRemoveProjectFromOrg(Organization org, Guid projectId)
    {
        // Org managers can kick projects out and project managers can pull projects out
        if (!CanEditOrg(org.Id) && !await CanManageProject(projectId))
        {
            throw new UnauthorizedAccessException();
        }
    }
}
