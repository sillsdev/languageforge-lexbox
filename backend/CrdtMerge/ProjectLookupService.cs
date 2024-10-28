using LexData;
using Microsoft.EntityFrameworkCore;

namespace CrdtMerge;

public class ProjectLookupService(LexBoxDbContext dbContext)
{
    public async ValueTask<Guid?> GetProjectId(string projectCode)
    {
        var projectId = await dbContext.Projects
            .Where(p => p.Code == projectCode)
            .Select(p => p.Id)
            .FirstOrDefaultAsync();
        return projectId;
    }
}
