using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace LcmCrdt.Project;

public class ProjectDataCache(IOptions<LcmCrdtConfig> config)
{
    private record ProjectCacheEntry(ProjectData Data);
    private static readonly Lock EnsureCacheLock = new();
    private ConcurrentDictionary<string, ProjectCacheEntry>? _cache;

    [MemberNotNull(nameof(_cache))]
    private void EnsureCache()
    {
        var previousCache = Interlocked.CompareExchange(ref _cache, new ConcurrentDictionary<string, ProjectCacheEntry>(), null);
        if (previousCache is not null) return;
        if (!File.Exists(config.Value.ProjectCachePath()) || !config.Value.EnableProjectDataFileCache) return;
        Dictionary<string, ProjectCacheEntry>? dict;
        lock (EnsureCacheLock)
        {
            using var fileStream = File.OpenRead(config.Value.ProjectCachePath());
            dict = JsonSerializer.Deserialize<Dictionary<string, ProjectCacheEntry>>(fileStream);
        }
        if (dict is null) return;
        foreach (var kv in dict)
        {
            _cache.TryAdd(kv.Key, kv.Value);
        }
    }

    public ProjectData? CachedProjectData(CrdtProject project)
    {
        EnsureCache();
        return _cache.GetValueOrDefault(project.DbPath)?.Data;
    }

    public void SetProjectData(CrdtProject project, ProjectData data)
    {
        EnsureCache();
        _cache.AddOrUpdate(project.DbPath, new ProjectCacheEntry(data), (_, existing) => existing with {Data = data});
        if (!config.Value.EnableProjectDataFileCache) return;
        lock (EnsureCacheLock)
        {
            Directory.CreateDirectory(config.Value.ProjectPath);
            using var fileStream = File.Open(config.Value.ProjectCachePath(), FileMode.Create);
            JsonSerializer.Serialize(fileStream, _cache);
        }
    }
}
