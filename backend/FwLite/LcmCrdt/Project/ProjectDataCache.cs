using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LcmCrdt.Project;

public class ProjectDataCache(IOptions<LcmCrdtConfig> config, ILogger<ProjectDataCache> logger)
{
    private record ProjectCacheEntry(ProjectData Data);
    private static readonly Lock EnsureCacheLock = new();
    private ConcurrentDictionary<string, ProjectCacheEntry>? _cache;

    [MemberNotNull(nameof(_cache))]
    private void EnsureCache()
    {
        var previousCache = Interlocked.CompareExchange(ref _cache, new ConcurrentDictionary<string, ProjectCacheEntry>(), null);
        if (previousCache is not null) return;

        // If file caching is disabled or file doesn't exist, we're done
        var projectCachePath = config.Value.ProjectCachePath();
        if (!config.Value.EnableProjectDataFileCache || !File.Exists(projectCachePath))
        {
            return;
        }

        try
        {
            Dictionary<string, ProjectCacheEntry>? dict;
            lock (EnsureCacheLock)
            {
                try
                {
                    using var fileStream = File.OpenRead(projectCachePath);
                    dict = JsonSerializer.Deserialize<Dictionary<string, ProjectCacheEntry>>(fileStream);
                }
                catch (UnauthorizedAccessException ex)
                {
                    logger.LogWarning(ex, "Access denied when trying to read project cache file at {CachePath}", projectCachePath);
                    return;
                }
                catch (DirectoryNotFoundException ex)
                {
                    logger.LogWarning(ex, "Directory not found when trying to read project cache file at {CachePath}", projectCachePath);
                    return;
                }
                catch (FileNotFoundException ex)
                {
                    logger.LogWarning(ex, "Project cache file not found at {CachePath}", projectCachePath);
                    return;
                }
                catch (IOException ex)
                {
                    logger.LogWarning(ex, "I/O error when trying to read project cache file at {CachePath}", projectCachePath);
                    return;
                }
                catch (JsonException ex)
                {
                    logger.LogWarning(ex, "Failed to deserialize project cache file at {CachePath}. The cache file may be corrupted", projectCachePath);
                    return;
                }
            }

            if (dict is null)
            {
                logger.LogWarning("Project cache file at {CachePath} deserialized to null", projectCachePath);
                return;
            }

            foreach (var kv in dict)
            {
                _cache.TryAdd(kv.Key, kv.Value);
            }

            logger.LogDebug("Successfully loaded {Count} entries from project cache file at {CachePath}", dict.Count, projectCachePath);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error when trying to load project cache from {CachePath}", projectCachePath);
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

        var projectCachePath = config.Value.ProjectCachePath();
        try
        {
            lock (EnsureCacheLock)
            {
                try
                {
                    Directory.CreateDirectory(config.Value.ProjectPath);
                    using var fileStream = File.Open(projectCachePath, FileMode.Create);
                    JsonSerializer.Serialize(fileStream, _cache);
                    logger.LogDebug("Successfully saved project cache to {CachePath}", projectCachePath);
                }
                catch (UnauthorizedAccessException ex)
                {
                    logger.LogWarning(ex, "Access denied when trying to write project cache file at {CachePath}", projectCachePath);
                }
                catch (DirectoryNotFoundException ex)
                {
                    logger.LogWarning(ex, "Directory not found when trying to write project cache file at {CachePath}", projectCachePath);
                }
                catch (IOException ex)
                {
                    logger.LogWarning(ex, "I/O error when trying to write project cache file at {CachePath}", projectCachePath);
                }
                catch (JsonException ex)
                {
                    logger.LogWarning(ex, "Failed to serialize project cache to file at {CachePath}", projectCachePath);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error when trying to save project cache to {CachePath}", projectCachePath);
        }
    }
}
