using System.Collections.Concurrent;
using LcmCrdt.FullTextSearch;
using LcmCrdt.Project;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LcmCrdt;

public class CurrentProjectService(
    IServiceProvider services,
    ProjectDataCache projectDataCache,
    CrdtProjectsService crdtProjectsService,
    ILogger<CrdtProjectsService> logger)
{
    private CrdtProject? _project;
    //creating a DbContext depends on the CurrentProjectService, so we can't create it in the constructor otherwise we'll create a circular dependency
    private IDbContextFactory<LcmCrdtDbContext> DbContextFactory => services.GetRequiredService<IDbContextFactory<LcmCrdtDbContext>>();
    private EntrySearchServiceFactory? EntrySearchServiceFactory => services.GetService<EntrySearchServiceFactory>();
    public CrdtProject Project => _project ?? throw new InvalidOperationException("Not in the context of a project");
    public CrdtProject? MaybeProject => _project;

    //only works because PopulateProjectDataCache is called first in the request pipeline
    public ProjectData ProjectData => projectDataCache.CachedProjectData(Project) ?? throw new InvalidOperationException(
        $"Project data not found for project {MaybeProject?.Name}, call PopulateProjectDataCache first or use GetProjectData");

    public async ValueTask<ProjectData> GetProjectData(bool forceRefresh = false)
    {
        var result = projectDataCache.CachedProjectData(Project);
        if (result is null || forceRefresh)
        {
            await using var dbContext = await DbContextFactory.CreateDbContextAsync();
            result = await dbContext.ProjectData.AsNoTracking().FirstAsync();
            projectDataCache.SetProjectData(Project, result);
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
        //migrate will only execute once with a static which tracks if we've already migrated
        await MigrateDb();
        return project.Data = await RefreshProjectData();
    }

    public async ValueTask<ProjectData> SetupProjectContext(string projectCode)
    {
        return await SetupProjectContext(crdtProjectsService.GetProject(projectCode) ?? throw new InvalidOperationException($"Crdt Project {projectCode} not found"));
    }

    public async ValueTask<ProjectData> RefreshProjectData()
    {
        var projectData = await GetProjectData(true);
        return projectData;
    }

    private static readonly ConcurrentDictionary<string, Lazy<Task>> MigrationTasks = [];
    private Task MigrateDb()
    {
        //ensure we only execute migrations once, this avoids race conditions, as well as doing duplicate work on startup
        //design based on https://andrewlock.net/making-getoradd-on-concurrentdictionary-thread-safe-using-lazy/
#pragma warning disable VSTHRD011
        return MigrationTasks.GetOrAdd(Project.DbPath, _ => new Lazy<Task>(Execute)).Value;
#pragma warning restore VSTHRD011
        async Task Execute()
        {
            try
            {
                await using var dbContext = await DbContextFactory.CreateDbContextAsync();
                await dbContext.Database.MigrateAsync();
                if (EntrySearchServiceFactory is not null)
                {
                    await using var  ess = EntrySearchServiceFactory.CreateSearchService(dbContext);
                    await ess.RegenerateIfMissing();
                }
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
        await using var dbContext = await DbContextFactory.CreateDbContextAsync();
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

        await RefreshProjectData();
    }

    public async Task UpdateLastUser(string? userName, string? userId)
    {
        if (userName is null && userId is null) return;
        if (userName != ProjectData.LastUserName || userId != ProjectData.LastUserId)
        {
            await using var dbContext = await DbContextFactory.CreateDbContextAsync();
            await dbContext.ProjectData.ExecuteUpdateAsync(calls => calls
                .SetProperty(p => p.LastUserName, userName)
                .SetProperty(p => p.LastUserId, userId));
            await RefreshProjectData();
        }
    }

    public async Task UpdateUserRole(UserProjectRole role)
    {
        if (ProjectData.Role == role) return;
        await using var dbContext = await DbContextFactory.CreateDbContextAsync();
        await dbContext.ProjectData.ExecuteUpdateAsync(calls => calls
            .SetProperty(p => p.Role, role));
        await RefreshProjectData();
    }
}
