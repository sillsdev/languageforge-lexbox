namespace FwHeadless.Services;

public interface IProjectLookupService
{
    ValueTask<string?> GetProjectCode(Guid projectId);
    Task<bool> ProjectExists(Guid projectId);
    Task<bool> IsCrdtProject(Guid projectId);
}
