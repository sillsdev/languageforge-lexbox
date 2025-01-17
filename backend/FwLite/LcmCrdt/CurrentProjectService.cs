using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace LcmCrdt;

public class CurrentProjectService(IServiceProvider services, IMemoryCache memoryCache, CrdtProjectsService crdtProjectsService)
{
    private CrdtProject? _project;
    //creating a DbContext depends on the CurrentProjectService, so we can't create it in the constructor otherwise we'll create a circular dependency
    private LcmCrdtDbContext DbContext => services.GetRequiredService<LcmCrdtDbContext>();
    public CrdtProject Project => _project ?? throw new NullReferenceException("Not in the context of a project");
    public CrdtProject? MaybeProject => _project;

    //only works because PopulateProjectDataCache is called first in the request pipeline
    public ProjectData ProjectData => memoryCache.Get<ProjectData>(CacheKey(Project)) ?? throw new InvalidOperationException("Project data not found, call PopulateProjectDataCache first or use GetProjectData");

    public async ValueTask<ProjectData> GetProjectData()
    {
        var key = CacheKey(Project);
        if (!memoryCache.TryGetValue(key, out object? result))
        {
            result = await DbContext.ProjectData.AsNoTracking().FirstAsync();
            memoryCache.Set(key, result);
            memoryCache.Set(CacheKey(((ProjectData)result).Id), result);
        }
        if (result is null) throw new InvalidOperationException("Project data not found");

        return (ProjectData)result;
    }

    public void ValidateProjectScope()
    {
        if (Project is null) throw new InvalidOperationException($"Project is null, there's a bug and {nameof(SetupProjectContext)} was not called");
    }

    private static string CacheKey(CrdtProject project)
    {
        return project.Name + "|ProjectData";
    }

    private static string CacheKey(Guid projectId)
    {
        return $"ProjectData|{projectId}";
    }

    public static ProjectData? LookupProjectData(IMemoryCache memoryCache, string projectName)
    {
        return memoryCache.Get<ProjectData>(projectName + "|ProjectData");
    }

    public static ProjectData? LookupProjectById(IMemoryCache memoryCache, Guid projectId)
    {
        return memoryCache.Get<ProjectData>(CacheKey(projectId));
    }

    /// <summary>
    /// Setup the project context for a new db, this will not trigger a refresh or setup for ProjectData, you probably want to call SetupProjectContext instead
    /// </summary>
    public void SetupProjectContextForNewDb(CrdtProject project)
    {
        _project = project;
    }

    public void ClearProjectContext()
    {
        _project = null;
    }

    public async ValueTask<ProjectData> SetupProjectContext(CrdtProject project)
    {
        if (_project != null && project != _project) throw new InvalidOperationException("Can't setup project context for a different project");
        _project = project;
        return await RefreshProjectData();
    }

    public async ValueTask<ProjectData> SetupProjectContext(string projectName)
    {
        return await SetupProjectContext(crdtProjectsService.GetProject(projectName) ?? throw new InvalidOperationException($"Crdt Project {projectName} not found"));
    }

    public async ValueTask<ProjectData> RefreshProjectData()
    {
        RemoveProjectDataCache();
        var projectData = await GetProjectData();
        return projectData;
    }

    private void RemoveProjectDataCache()
    {
        memoryCache.Remove(CacheKey(Project));
    }

    public async Task SetProjectSyncOrigin(Uri? domain, Guid? id)
    {
        var originDomain = ProjectData.GetOriginDomain(domain);
        if (id is null)
        {
            await DbContext.Set<ProjectData>()
                .ExecuteUpdateAsync(calls => calls.SetProperty(p => p.OriginDomain, originDomain));
        }
        else
        {
            await DbContext.Set<ProjectData>()
                .ExecuteUpdateAsync(calls => calls.SetProperty(p => p.OriginDomain, originDomain)
                    .SetProperty(p => p.Id, id));
        }

        await RefreshProjectData();
    }
}
