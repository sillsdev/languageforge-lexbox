using Crdt.Db;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace LcmCrdt;

public class CurrentProjectService(LcmCrdtDbContext dbContext, ProjectContext projectContext, IMemoryCache memoryCache)
{
    public CrdtProject Project =>
        projectContext.Project ?? throw new NullReferenceException("Not in the context of a project");

    //only works because PopulateProjectDataCache is called first in the request pipeline
    public ProjectData ProjectData => memoryCache.Get<ProjectData>(CacheKey(Project)) ?? throw new InvalidOperationException("Project data not found");

    public async ValueTask<ProjectData> GetProjectData()
    {
        var key = CacheKey(Project);
        if (!memoryCache.TryGetValue(key, out object? result))
        {
            using var entry = memoryCache.CreateEntry(key);
            entry.SlidingExpiration = TimeSpan.FromMinutes(10);
            result = await dbContext.ProjectData.AsNoTracking().FirstAsync();
            entry.Value = result;
        }
        if (result is null) throw new InvalidOperationException("Project data not found");

        return (ProjectData)result;
    }

    private static string CacheKey(CrdtProject project)
    {
        return project.Name + "|ProjectData";
    }

    public async ValueTask<ProjectData> PopulateProjectDataCache()
    {
        var projectData = await GetProjectData();
        return projectData;
    }

    private void RemoveProjectDataCache()
    {
        memoryCache.Remove(CacheKey(Project));
    }

    public async Task SetProjectSyncOrigin(Uri domain, Guid? id)
    {
        var originDomain = ProjectData.GetOriginDomain(domain);
        if (id is null)
        {
            await dbContext.Set<ProjectData>()
                .ExecuteUpdateAsync(calls => calls.SetProperty(p => p.OriginDomain, originDomain));
        }
        else
        {
            await dbContext.Set<ProjectData>()
                .ExecuteUpdateAsync(calls => calls.SetProperty(p => p.OriginDomain, originDomain)
                    .SetProperty(p => p.Id, id));
        }

        RemoveProjectDataCache();
        await PopulateProjectDataCache();
    }
}
