using LexBoxApi.Auth;
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

    public async ValueTask<bool> CanAccessProject(string projectCode)
    {
        if (User is null) return false;
        if (User.Role == UserRole.admin) return true;
        return CanAccessProject(await projectService.LookupProjectId(projectCode));
    }

    public bool CanAccessProject(Guid projectId)
    {
        if (User is null) return false;
        if (User.Role == UserRole.admin) return true;
        return User.Projects.Any(p => p.ProjectId == projectId);
    }

    public async ValueTask AssertCanAccessProject(string projectCode)
    {
        if (!await CanAccessProject(projectCode)) throw new UnauthorizedAccessException();
    }

    public void AssertCanAccessProject(Guid projectId)
    {
        if (!CanAccessProject(projectId)) throw new UnauthorizedAccessException();
    }

    public bool CanManageProject(Guid projectId)
    {
        if (User is null) return false;
        if (User.Role == UserRole.admin) return true;
        return User.Projects.Any(p => p.ProjectId == projectId && p.Role == ProjectRole.Manager);
    }

    public void AssertCanManageProject(Guid projectId)
    {
        if (!CanManageProject(projectId)) throw new UnauthorizedAccessException();
    }

    public void AssertCanManageProjectMemberRole(Guid projectId, Guid userId)
    {
        if (User is null) throw new UnauthorizedAccessException();
        AssertCanManageProject(projectId);
        if (User.Role != UserRole.admin && userId == User.Id)
            throw new UnauthorizedAccessException("Not allowed to change own project role.");
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

    public void AssertCanCreateOrg()
    {
        if (!HasProjectCreatePermission()) throw new UnauthorizedAccessException();
    }
}
