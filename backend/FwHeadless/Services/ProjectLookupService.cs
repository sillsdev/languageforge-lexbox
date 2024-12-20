using LexData;
using Microsoft.EntityFrameworkCore;
using SIL.Harmony.Core;

namespace FwHeadless.Services;

public class ProjectLookupService(LexBoxDbContext dbContext)
{
    public async ValueTask<string?> GetProjectCode(Guid projectId)
    {
        var projectCode = await dbContext.Projects
            .Where(p => p.Id == projectId)
            .Select(p => p.Code)
            .FirstOrDefaultAsync();
        return projectCode;
    }

    public async Task<bool> ProjectExists(Guid projectId)
    {
        return await dbContext.Projects.AnyAsync(p => p.Id == projectId);
    }

    public async Task<bool> IsCrdtProject(Guid projectId)
    {
        return await dbContext.Set<ServerCommit>().AnyAsync(c => c.ProjectId == projectId);
    }
}
