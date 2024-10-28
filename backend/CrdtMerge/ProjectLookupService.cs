using LexData;
using Microsoft.EntityFrameworkCore;

namespace CrdtMerge;

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
}
