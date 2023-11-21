namespace LexCore.ServiceInterfaces;

public interface IPermissionService
{
    ValueTask<bool> CanAccessProject(string projectCode);
    bool CanAccessProject(Guid projectId);
    ValueTask AssertCanAccessProject(string projectCode);
    bool CanManageProject(Guid projectId);
    void AssertCanManageProject(Guid projectId);
    void AssertCanManageProjectMemberRole(Guid projectId, Guid userId);
    void AssertIsAdmin();
    void AssertCanDeleteAccount(Guid userId);
    bool HasProjectCreatePermission();
}
