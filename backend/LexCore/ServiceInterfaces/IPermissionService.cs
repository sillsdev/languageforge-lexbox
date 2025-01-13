using LexCore.Auth;
using LexCore.Entities;

namespace LexCore.ServiceInterfaces;

public interface IPermissionService
{
    ValueTask<bool> CanSyncProject(string projectCode);
    bool IsProjectMember(Guid projectId, LexAuthUser? overrideUser = null);
    ValueTask<bool> CanSyncProject(Guid projectId);
    ValueTask AssertCanSyncProject(string projectCode);
    ValueTask AssertCanSyncProject(Guid projectId);
    ValueTask<bool> CanViewProject(Guid projectId, LexAuthUser? overrideUser = null);
    ValueTask AssertCanViewProject(Guid projectId);
    ValueTask<bool> CanViewProject(string projectCode, LexAuthUser? overrideUser = null);
    ValueTask AssertCanViewProject(string projectCode, LexAuthUser? overrideUser = null);
    ValueTask<bool> CanViewProjectMembers(Guid projectId);
    ValueTask<bool> CanManageProject(Guid projectId);
    ValueTask<bool> CanManageProject(string projectCode);
    ValueTask AssertCanManageProject(Guid projectId);
    ValueTask AssertCanManageProject(string projectCode);
    ValueTask AssertCanManageProjectMemberRole(Guid projectId, Guid userId);
    ValueTask<bool> CanCreateGuestUserInProject(Guid projectId);
    ValueTask AssertCanCreateGuestUserInProject(Guid projectId);
    bool CanCreateGuestUserInAnyProject();
    void AssertCanCreateGuestUserInAnyProject();
    ValueTask<bool> CanAskToJoinProject(Guid projectId);
    ValueTask<bool> CanAskToJoinProject(string projectCode);
    ValueTask AssertCanAskToJoinProject(Guid projectId);
    ValueTask AssertCanAskToJoinProject(string projectCode);
    void AssertIsAdmin();
    void AssertCanDeleteAccount(Guid userId);
    bool HasProjectCreatePermission();
    void AssertHasProjectCreatePermission();
    bool HasProjectRequestPermission();
    void AssertHasProjectRequestPermission();
    void AssertCanLockOrUnlockUser(Guid userId);
    void AssertCanCreateOrg();
    bool IsOrgMember(Guid orgId, LexAuthUser? overrideUser = null);
    bool CanEditOrg(Guid orgId, LexAuthUser? overrideUser = null);
    void AssertCanEditOrg(Organization org);
    void AssertCanEditOrg(Guid orgId);
    void AssertCanAddProjectToOrg(Organization org);
    ValueTask AssertCanRemoveProjectFromOrg(Organization org, Guid projectId);
}
