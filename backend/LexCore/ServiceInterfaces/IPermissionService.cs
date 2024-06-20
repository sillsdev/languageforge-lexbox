using LexCore.Entities;

namespace LexCore.ServiceInterfaces;

public interface IPermissionService
{
    ValueTask<bool> CanSyncProject(string projectCode);
    bool CanSyncProject(Guid projectId);
    ValueTask AssertCanSyncProject(string projectCode);
    void AssertCanSyncProject(Guid projectId);
    bool CanManageProject(Guid projectId);
    void AssertCanManageProject(Guid projectId);
    void AssertCanManageProjectMemberRole(Guid projectId, Guid userId);
    void AssertIsAdmin();
    void AssertCanDeleteAccount(Guid userId);
    bool HasProjectCreatePermission();
    void AssertHasProjectCreatePermission();
    bool HasProjectRequestPermission();
    void AssertHasProjectRequestPermission();
    void AssertCanLockOrUnlockUser(Guid userId);
    void AssertCanCreateOrg();
    void AssertCanEditOrg(Organization org);
    void AssertCanAddProjectToOrg(Organization org);
}
