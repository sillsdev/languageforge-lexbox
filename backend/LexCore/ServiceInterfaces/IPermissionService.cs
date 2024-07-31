using LexCore.Entities;

namespace LexCore.ServiceInterfaces;

public interface IPermissionService
{
    ValueTask<bool> CanSyncProject(string projectCode);
    /// <summary>
    /// Does NOT check permissions for org managers, because that requires async DB access.
    /// Use CanSyncProject(projectCode) or CanSyncProjectAsync(projectId) if org manager permissions also need to be checked.
    /// </summary>
    bool CanSyncProject(Guid projectId);
    /// <summary>
    /// Does all the checks from CanSyncProject, plus allows org managers to access the org as well.
    /// </summary>
    ValueTask<bool> CanSyncProjectAsync(Guid projectId);
    ValueTask AssertCanSyncProject(string projectCode);
    ValueTask AssertCanSyncProject(Guid projectId);
    ValueTask<bool> CanViewProject(Guid projectId);
    ValueTask AssertCanViewProject(Guid projectId);
    ValueTask<bool> CanViewProject(string projectCode);
    ValueTask AssertCanViewProject(string projectCode);
    ValueTask<bool> CanManageProject(Guid projectId);
    ValueTask AssertCanManageProject(Guid projectId);
    ValueTask AssertCanManageProjectMemberRole(Guid projectId, Guid userId);
    ValueTask<bool> CanAskToJoinProject(Guid projectId);
    ValueTask AssertCanAskToJoinProject(Guid projectId);
    void AssertIsAdmin();
    void AssertCanDeleteAccount(Guid userId);
    bool HasProjectCreatePermission();
    void AssertHasProjectCreatePermission();
    bool HasProjectRequestPermission();
    void AssertHasProjectRequestPermission();
    void AssertCanLockOrUnlockUser(Guid userId);
    void AssertCanCreateOrg();
    bool IsOrgMember(Guid orgId);
    bool CanEditOrg(Guid orgId);
    void AssertCanEditOrg(Organization org);
    void AssertCanEditOrg(Guid orgId);
    void AssertCanAddProjectToOrg(Organization org);
}
