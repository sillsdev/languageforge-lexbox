using LexData;
using Microsoft.EntityFrameworkCore;
using SIL.Harmony.Core;

namespace FwHeadless.Services;

public class ProjectLookupService(LexBoxDbContext dbContext)
{
    public virtual async ValueTask<string?> GetProjectCode(Guid projectId)
    {
        var projectCode = await dbContext.Projects
            .Where(p => p.Id == projectId)
            .Select(p => p.Code)
            .FirstOrDefaultAsync();
        return projectCode;
    }

    public virtual async Task<bool> ProjectExists(Guid projectId)
    {
        return await dbContext.Projects.AnyAsync(p => p.Id == projectId);
    }

    public virtual async Task<bool> IsCrdtProject(Guid projectId)
    {
        return await dbContext.Set<ServerCommit>().AnyAsync(c => c.ProjectId == projectId);
    }

    public virtual async Task<Guid?> GetProjectId(string projectCode)
    {
        var projectId = await dbContext.Projects
            .Where(p => p.Code == projectCode)
            .Select(p => p.Id)
            .FirstOrDefaultAsync();
        return projectId == Guid.Empty ? null : projectId;
    }
}
