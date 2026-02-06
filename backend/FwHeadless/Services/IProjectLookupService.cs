namespace FwHeadless.Services;

public interface IProjectLookupService
{
    ValueTask<string?> GetProjectCode(Guid projectId);
    Task<Guid?> GetProjectId(string projectCode);
    Task<bool> ProjectExists(Guid projectId);
    Task<bool> IsCrdtProject(Guid projectId);
}
