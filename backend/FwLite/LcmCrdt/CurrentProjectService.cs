using System.Collections.Concurrent;
using LcmCrdt.FullTextSearch;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LcmCrdt;

public class CurrentProjectService(
    IServiceProvider services,
    IMemoryCache memoryCache,
    CrdtProjectsService crdtProjectsService,
    ILogger<CrdtProjectsService> logger)
{
    private CrdtProject? _project;
    //creating a DbContext depends on the CurrentProjectService, so we can't create it in the constructor otherwise we'll create a circular dependency
    private LcmCrdtDbContext DbContext => services.GetRequiredService<LcmCrdtDbContext>();
    private EntrySearchService? EntrySearchService => services.GetService<EntrySearchService>();
    public CrdtProject Project => _project ?? throw new NullReferenceException("Not in the context of a project");
    public CrdtProject? MaybeProject => _project;

    //only works because PopulateProjectDataCache is called first in the request pipeline
    public ProjectData ProjectData => memoryCache.Get<ProjectData>(CacheKey(Project)) ?? throw new InvalidOperationException(
        $"Project data not found for project {MaybeProject?.Name}, call PopulateProjectDataCache first or use GetProjectData");

    public async ValueTask<ProjectData> GetProjectData(bool forceRefresh = false)
    {
        var result = LookupProjectData(memoryCache, Project);
        if (result is null || forceRefresh)
        {
            result = await DbContext.ProjectData.AsNoTracking().FirstAsync();
            memoryCache.Set(CacheKey(Project), result);
        }
        if (result is null) throw new InvalidOperationException($"Project data not found for project {MaybeProject?.Name}");

        return result;
    }

    public void ValidateProjectScope()
    {
        if (Project is null)
            throw new InvalidOperationException($"Project is null for project {MaybeProject?.Name}, there's a bug and {nameof(SetupProjectContext)} was not called");
    }

    private static string CacheKey(CrdtProject project)
    {
        return project.DbPath + "|ProjectData";
    }

    public static ProjectData? LookupProjectData(IMemoryCache memoryCache, CrdtProject project)
    {
        return memoryCache.Get<ProjectData>(CacheKey(project));
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
        using var activity = LcmCrdtActivitySource.Value.StartActivity();
        activity?.SetTag("app.project_code", project.Name);
        if (_project != null && project != _project)
            throw new InvalidOperationException($"Can't setup project context for {project.Name} when already in context of project {_project.Name}");
        _project = project;
        //the first time this is called ProjectData will be null, after that it will be populated, so we can skip migration
        if (LookupProjectData(memoryCache, project) is null) await MigrateDb();
        return await RefreshProjectData();
    }

    public async ValueTask<ProjectData> SetupProjectContext(string projectName)
    {
        return await SetupProjectContext(crdtProjectsService.GetProject(projectName) ?? throw new InvalidOperationException($"Crdt Project {projectName} not found"));
    }

    public async ValueTask<ProjectData> RefreshProjectData()
    {
        var projectData = await GetProjectData(true);
        return projectData;
    }

    private static readonly ConcurrentDictionary<string, Lazy<Task>> MigrationTasks = [];
    private Task MigrateDb()
    {
        //ensure we only execute once, otherwise we'll have a conflict as Migrate is not thread safe.
        //design based on https://andrewlock.net/making-getoradd-on-concurrentdictionary-thread-safe-using-lazy/
#pragma warning disable VSTHRD011
        return MigrationTasks.GetOrAdd(Project.DbPath, _ => new Lazy<Task>(Execute)).Value;
#pragma warning restore VSTHRD011
        async Task Execute()
        {
            try
            {
                await DbContext.Database.MigrateAsync();
                await (EntrySearchService?.RegenerateIfMissing() ?? Task.CompletedTask);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Failed to migrate database for project '{Project}'", Project.Name);
                throw;
            }
        }

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

    public async Task UpdateLastUser(string? userName, string? userId)
    {
        if (userName is null && userId is null) return;
        if (userName != ProjectData.LastUserName || userId != ProjectData.LastUserId)
        {
            await DbContext.ProjectData.ExecuteUpdateAsync(calls => calls
                .SetProperty(p => p.LastUserName, userName)
                .SetProperty(p => p.LastUserId, userId));
            await RefreshProjectData();
        }
    }

    public async Task UpdateUserRole(UserProjectRole role)
    {
        if (ProjectData.Role == role) return;
        await DbContext.ProjectData.ExecuteUpdateAsync(calls => calls
            .SetProperty(p => p.Role, role));
        await RefreshProjectData();
    }
}
