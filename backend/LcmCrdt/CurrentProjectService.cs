﻿using Crdt.Db;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace LcmCrdt;

public class CurrentProjectService(CrdtDbContext dbContext, ProjectContext projectContext, IMemoryCache memoryCache)
{
    public CrdtProject Project =>
        projectContext.Project ?? throw new NullReferenceException("Not in the context of a project");

    public async ValueTask<ProjectData> GetProjectData()
    {
        var key = Project.Name + "|ProjectData";
        if (!memoryCache.TryGetValue(key, out object? result))
        {
            using var entry = memoryCache.CreateEntry(key);
            entry.SlidingExpiration = TimeSpan.FromMinutes(10);
            result = await dbContext.Set<ProjectData>().AsNoTracking().FirstAsync();
            entry.Value = result;
        }
        if (result is null) throw new InvalidOperationException("Project data not found");

        return (ProjectData)result;
    }
}
