using FwDataMiniLcmBridge.Api;
using FwDataMiniLcmBridge.LcmUtils;
using FwDataMiniLcmBridge.Media;
using LexCore.Utils;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SIL.LCModel;

namespace FwDataMiniLcmBridge;

public class FwDataFactory(
    ILogger<FwDataMiniLcmApi> fwdataLogger,
    IMemoryCache cache,
    ILogger<FwDataFactory> logger,
    IProjectLoader projectLoader,
    IMediaAdapter mediaAdapter,
    IOptions<FwDataBridgeConfig> config) : IDisposable, IHostedService
{
    private bool _shuttingDown = false;

    // Sliding window before an idle LcmCache is evicted and disposed. PreventEviction refreshes inside this window.
    private static readonly TimeSpan CacheSlidingExpiration = TimeSpan.FromMinutes(30);

    public FwDataFactory(ILogger<FwDataMiniLcmApi> fwdataLogger,
        IMemoryCache cache,
        ILogger<FwDataFactory> logger,
        IProjectLoader projectLoader,
        IHostApplicationLifetime lifetime,
        IMediaAdapter mediaAdapter,
        IOptions<FwDataBridgeConfig> config) : this(fwdataLogger, cache, logger, projectLoader, mediaAdapter, config)
    {
        lifetime.ApplicationStopping.Register(() =>
        {
            //this gets called immediately after the shutdown is triggered, we need this so we can ignore project disconnects during shutdown.
            //and delegate those to the disposal of this class.
            _shuttingDown = true;
        });
    }

    internal static string CacheKey(FwDataProject project) => $"{nameof(FwDataFactory)}|{project.FilePath}";

    public FwDataMiniLcmApi GetFwDataMiniLcmApi(FwDataProject project, bool saveOnDispose)
    {
        return new FwDataMiniLcmApi(new(() => GetProjectServiceCached(project)), saveOnDispose, fwdataLogger, project, mediaAdapter, config);
    }

    private readonly Lock _cacheEntryLock = new();
    // Entries rather than keys, so a stale eviction callback can't untrack a newer entry for the same project.
    private HashSet<ProjectCacheEntry> _projectCacheEntries = [];
    private LcmCache GetProjectServiceCached(FwDataProject project)
    {
        var key = CacheKey(project);
        // IMemoryCache.GetOrCreate isn't atomic: concurrent callers would each load the project, and the later Set
        // would evict (and dispose) the earlier LcmCache as Replaced. So create the entry under a lock, and let the
        // entry make every caller share the one slow load without holding the lock during it.
        ProjectCacheEntry projectEntry;
        lock (_cacheEntryLock)
        {
            projectEntry = cache.GetOrCreate(key,
                entry =>
                {
                    entry.SlidingExpiration = CacheSlidingExpiration;
                    entry.RegisterPostEvictionCallback(OnLcmProjectCacheEviction);
                    var newEntry = new ProjectCacheEntry(key, project.FilePath, () =>
                    {
                        logger.LogInformation("Loading project {ProjectFileName}", project.FileName);
                        var projectService = projectLoader.LoadCache(project);
                        logger.LogInformation("Project {ProjectFileName} loaded", project.FileName);
                        return projectService;
                    }, logger);
                    _projectCacheEntries.Add(newEntry);
                    return newEntry;
                }) ?? throw new InvalidOperationException("Project service is null");
        }

        LcmCache projectService;
        try
        {
            projectService = projectEntry.LcmCache;
        }
        catch
        {
            // The entry caches its load exception, so drop it to let the next call retry the load.
            RemoveIfCurrent(projectEntry);
            throw;
        }

        if (projectService.IsDisposed)
        {
            throw new InvalidOperationException("Project service is disposed");
        }

        return projectService;
    }

    private void RemoveIfCurrent(ProjectCacheEntry projectEntry)
    {
        lock (_cacheEntryLock)
        {
            if (cache.TryGetValue(projectEntry.Key, out ProjectCacheEntry? current) && ReferenceEquals(current, projectEntry))
                cache.Remove(projectEntry.Key);
        }
    }

    private void OnLcmProjectCacheEviction(object key, object? value, EvictionReason reason, object? state)
    {
        if (value is not ProjectCacheEntry projectEntry) return;
        // todo this could trigger when the service is still referenced elsewhere, for example in a long running task.
        // disposing of the service while it's still in use would be bad.
        // one way around this would be to return a lease object, only after a timeout and no more references to the lease object would the service be disposed.
        logger.LogInformation("Evicting project {ProjectFileName} from cache ({EvictionReason})", projectEntry.FilePath, reason);
        lock (_cacheEntryLock)
        {
            _projectCacheEntries.Remove(projectEntry);
        }
        _ = projectEntry.DisposeLoaded().ContinueWith(disposal =>
        {
            if (disposal.IsFaulted)
                logger.LogError(disposal.Exception, "Failed to dispose project {ProjectFileName}", projectEntry.FilePath);
            GC.Collect();
        }, TaskScheduler.Default);
    }

    public void Dispose()
    {
        logger.LogInformation("Closing all projects");
        HashSet<ProjectCacheEntry> projectEntries;
        lock (_cacheEntryLock)
        {
            projectEntries = _projectCacheEntries;
            _projectCacheEntries = [];
        }
        foreach (var projectEntry in projectEntries)
        {
            //need to explicitly dispose as that blocks, just removing from the cache does not block, meaning it will not finish disposing before the program exits.
            var disposal = projectEntry.DisposeLoaded();
            // Not waited for: a load that hasn't finished has no changes to save, and exiting releases its file locks.
            if (!disposal.IsCompleted)
                logger.LogWarning("Project {ProjectFileName} is still loading at shutdown, not waiting for it", projectEntry.FilePath);
            else if (disposal.IsFaulted)
                logger.LogError(disposal.Exception, "Failed to dispose project {ProjectFileName}", projectEntry.FilePath);
            RemoveIfCurrent(projectEntry);
        }
    }

    public async Task CloseProjectAsync(FwDataProject project)
    {
        // if we are shutting down, don't do anything because we want project dispose to be called as part of the shutdown process.
        if (_shuttingDown) return;
        logger.LogInformation("Explicitly Closing project {ProjectFileName}", project.FilePath);
        var projectEntry = TakeEntry(CacheKey(project));
        if (projectEntry is null) return;
        // Dispose immediately (waiting for a load still in flight) so file locks are released before we return.
        // The caller assumes the project is ready to be opened somewhere else (e.g. in FieldWorks)
        await Task.Run(projectEntry.DisposeLoaded);
    }

    private ProjectCacheEntry? TakeEntry(string key)
    {
        lock (_cacheEntryLock)
        {
            if (!cache.TryGetValue(key, out ProjectCacheEntry? projectEntry)) return null;
            cache.Remove(key);
            return projectEntry;
        }
    }

    public IAsyncDisposable DeferCloseAsync(FwDataProject project)
    {
        return Defer.Async(() => CloseProjectAsync(project));
    }

    /// <summary>
    /// Keeps the project's LcmCache from being evicted until disposed, by periodically resetting its sliding expiration.
    /// Use around long-running work that holds one api instance past the expiration window (e.g. a sync paused in a debugger).
    /// </summary>
    public IDisposable PreventEviction(FwDataProject project)
    {
        var key = CacheKey(project);
        // Refresh now in case little of the window remains, then every half-window so a tick can't be missed.
        var period = CacheSlidingExpiration / 2;
        return new Timer(_ =>
        {
            // Best-effort: the shared cache can be disposed during shutdown while a sync is still finishing,
            // and an unhandled throw on this timer thread would take the process down.
            try { cache.TryGetValue(key, out _); }
            catch (ObjectDisposedException) { }
        }, null, TimeSpan.Zero, period);
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    //Services in MAUI apps aren't disposed when the app is shut down, we have a workaround to shutdown HostedServices on shutdown, so we made this IHostedService to close projects on shutdown
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _shuttingDown = true;
        Dispose();
        return Task.CompletedTask;
    }

    private sealed class ProjectCacheEntry
    {
        // Lets disposal wait for the load without blocking a thread on the Lazy.
        private readonly TaskCompletionSource _loaded = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private LcmCache? _loadedCache;
        private readonly Lazy<LcmCache> _lcmCache;
        private readonly TaskCompletionSource _disposed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _disposeRequested;
        private readonly ILogger _logger;

        public ProjectCacheEntry(string key, string filePath, Func<LcmCache> load, ILogger logger)
        {
            Key = key;
            FilePath = filePath;
            _logger = logger;
            _lcmCache = new(() =>
            {
                try
                {
                    var lcmCache = load();
                    _loadedCache = lcmCache;
                    _loaded.SetResult();
                    return lcmCache;
                }
                catch (Exception e)
                {
                    _loaded.SetException(e);
                    throw;
                }
            }, LazyThreadSafetyMode.ExecutionAndPublication);
        }

        public string Key { get; }
        public string FilePath { get; }
        public LcmCache LcmCache => _lcmCache.Value;

        /// <summary>
        /// Disposes the LcmCache once its load finishes, synchronously if it already has.
        /// Runs once however many of eviction, close and shutdown call it; every caller gets the same task.
        /// </summary>
        public Task DisposeLoaded()
        {
            if (Interlocked.Exchange(ref _disposeRequested, 1) == 0)
            {
                if (_loaded.Task.IsCompleted) DisposeAfterLoad(_loaded.Task);
                else _ = _loaded.Task.ContinueWith(DisposeAfterLoad, TaskScheduler.Default);
            }
#pragma warning disable VSTHRD003 // Avoid awaiting foreign Tasks: this entry owns the TaskCompletionSource
            return _disposed.Task;
#pragma warning restore VSTHRD003
        }

        private void DisposeAfterLoad(Task load)
        {
            try
            {
                if (!load.IsCompletedSuccessfully)
                {
                    _logger.LogWarning(load.Exception, "FW Data Project {ProjectFileName} failed to load, nothing to dispose", FilePath);
                }
                else if (_loadedCache is { IsDisposed: false } lcmCache)
                {
                    lcmCache.Dispose();
                    _logger.LogInformation("FW Data Project {ProjectFileName} disposed", FilePath);
                }
                _disposed.SetResult();
            }
            catch (Exception e)
            {
                _disposed.SetException(e);
            }
        }
    }
}
