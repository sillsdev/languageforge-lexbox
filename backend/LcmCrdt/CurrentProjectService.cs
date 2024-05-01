using CrdtLib.Db;
using Microsoft.EntityFrameworkCore;

namespace LcmCrdt;

public class CurrentProjectService(CrdtDbContext dbContext, ProjectContext projectContext)
{
    public CrdtProject Project =>
        projectContext.Project ?? throw new NullReferenceException("Not in the context of a project");

    public Task<ProjectData> GetProjectData()
    {
        return dbContext.Set<ProjectData>().FirstAsync();
    }
}
