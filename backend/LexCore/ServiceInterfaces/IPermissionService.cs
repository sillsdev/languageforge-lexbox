using LexCore.Entities;

namespace LexCore.ServiceInterfaces;

public interface IPermissionService
{
    ValueTask<bool> CanSyncProject(string projectCode);
    bool CanSyncProject(Guid projectId);
    ValueTask AssertCanSyncProject(string projectCode);
    void AssertCanSyncProject(Guid projectId);
    ValueTask<bool> CanViewProject(Guid projectId);
    ValueTask AssertCanViewProject(Guid projectId);
    ValueTask<bool> CanViewProject(string projectCode);
    ValueTask AssertCanViewProject(string projectCode);
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
    bool IsOrgMember(Organization org);
    bool CanEditOrg(Organization org);
    void AssertCanEditOrg(Organization org);
    void AssertCanAddProjectToOrg(Organization org);
}
