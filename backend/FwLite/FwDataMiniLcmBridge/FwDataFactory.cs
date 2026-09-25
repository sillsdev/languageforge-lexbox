using System.Collections.Concurrent;
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

    // One lock per project, held for the whole load, so concurrent requests share a load instead of racing
    // (IMemoryCache.GetOrCreate isn't atomic) and the cache only ever holds a finished LcmCache.
    // Never pruned: it grows by one small Lock per distinct project path this factory opens.
    private readonly ConcurrentDictionary<string, Lock> _keyLocks = new();
    private Lock KeyLock(string key) => _keyLocks.GetOrAdd(key, _ => new());

    private readonly Lock _openCachesLock = new();
    // Whoever removes an instance from here disposes it, so eviction, close and shutdown dispose it exactly once.
    // Keyed by instance so a stale eviction callback can't untrack a newer cache for the same project.
    private Dictionary<LcmCache, string> _openCaches = [];

    private static string FilePathFromCacheKey(string cacheKey) => cacheKey.Split('|')[1];

    private LcmCache GetProjectServiceCached(FwDataProject project)
    {
        var key = CacheKey(project);
        if (cache.TryGetValue(key, out LcmCache? hit) && !hit!.IsDisposed) return hit;
        lock (KeyLock(key))
        {
            if (cache.TryGetValue(key, out hit) && !hit!.IsDisposed) return hit;
            logger.LogInformation("Loading project {ProjectFileName}", project.FileName);
            var lcmCache = projectLoader.LoadCache(project);
            logger.LogInformation("Project {ProjectFileName} loaded", project.FileName);
            lock (_openCachesLock)
            {
                _openCaches.Add(lcmCache, key);
            }
            // The entry is committed to the cache when it's disposed.
            using (var entry = cache.CreateEntry(key))
            {
                entry.SlidingExpiration = CacheSlidingExpiration;
                entry.RegisterPostEvictionCallback(OnLcmProjectCacheEviction);
                entry.Value = lcmCache;
            }
            return lcmCache;
        }
    }

    private bool Untrack(LcmCache lcmCache)
    {
        lock (_openCachesLock)
        {
            return _openCaches.Remove(lcmCache);
        }
    }

    private void OnLcmProjectCacheEviction(object keyObj, object? value, EvictionReason reason, object? state)
    {
        if (value is not LcmCache lcmCache) return;
        // todo this could trigger when the service is still referenced elsewhere, for example in a long running task.
        // disposing of the service while it's still in use would be bad.
        // one way around this would be to return a lease object, only after a timeout and no more references to the lease object would the service be disposed.
        var filePath = FilePathFromCacheKey((string)keyObj);
        logger.LogInformation("Evicting project {ProjectFileName} from cache ({EvictionReason})", filePath, reason);
        if (!Untrack(lcmCache) || lcmCache.IsDisposed) return;
        lcmCache.Dispose();
        logger.LogInformation("FW Data Project {ProjectFileName} disposed", filePath);
        GC.Collect();
    }

    public void Dispose()
    {
        logger.LogInformation("Closing all projects");
        Dictionary<LcmCache, string> openCaches;
        lock (_openCachesLock)
        {
            openCaches = _openCaches;
            _openCaches = [];
        }
        foreach (var (lcmCache, key) in openCaches)
        {
            if (!lcmCache.IsDisposed)
            {
                //need to explicitly call dispose as that blocks, just removing from the cache does not block, meaning it will not finish disposing before the program exits.
                lcmCache.Dispose();
                logger.LogInformation("FW Data Project {ProjectFileName} disposed", FilePathFromCacheKey(key));
            }
            if (cache.TryGetValue(key, out LcmCache? current) && ReferenceEquals(current, lcmCache))
                cache.Remove(key);
        }
    }

    public async Task CloseProjectAsync(FwDataProject project)
    {
        // if we are shutting down, don't do anything because we want project dispose to be called as part of the shutdown process.
        if (_shuttingDown) return;
        logger.LogInformation("Explicitly Closing project {ProjectFileName}", project.FilePath);
        var key = CacheKey(project);
        // Dispose immediately (waiting for a load still in flight) so file locks are released before we return.
        // The caller assumes the project is ready to be opened somewhere else (e.g. in FieldWorks)
        await Task.Run(() =>
        {
            lock (KeyLock(key))
            {
                if (!cache.TryGetValue(key, out LcmCache? lcmCache) || lcmCache is null) return;
                // Untracked first, so the eviction callback that Remove triggers leaves the disposal to us.
                var owned = Untrack(lcmCache);
                cache.Remove(key);
                if (owned && !lcmCache.IsDisposed) lcmCache.Dispose();
            }
        });
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
}
