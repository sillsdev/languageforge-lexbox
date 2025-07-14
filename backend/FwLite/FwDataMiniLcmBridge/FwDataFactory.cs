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

    private string CacheKey(FwDataProject project) => $"{nameof(FwDataFactory)}|{project.FilePath}";

    public FwDataMiniLcmApi GetFwDataMiniLcmApi(FwDataProject project, bool saveOnDispose)
    {
        return new FwDataMiniLcmApi(new (() => GetProjectServiceCached(project)), saveOnDispose, fwdataLogger, project, mediaAdapter, config);
    }

    private HashSet<string> _projects = [];
    private LcmCache GetProjectServiceCached(FwDataProject project)
    {
        var key = CacheKey(project);
        var projectService = cache.GetOrCreate(key,
                entry =>
                {
                    entry.SlidingExpiration = TimeSpan.FromMinutes(30);
                    entry.RegisterPostEvictionCallback(OnLcmProjectCacheEviction, (logger, _projects));
                    logger.LogInformation("Loading project {ProjectFileName}", project.FileName);
                    var projectService = projectLoader.LoadCache(project);
                    logger.LogInformation("Project {ProjectFileName} loaded", project.FileName);
                    _projects.Add((string)entry.Key);
                    return projectService;
                });
        if (projectService is null)
        {
            throw new InvalidOperationException("Project service is null");
        }
        if (projectService.IsDisposed)
        {
            throw new InvalidOperationException("Project service is disposed");
        }

        return projectService;
    }

    private static void OnLcmProjectCacheEviction(object key, object? value, EvictionReason reason, object? state)
    {
        if (value is null) return;
        // todo this could trigger when the service is still referenced elsewhere, for example in a long running task.
        // disposing of the service while it's still in use would be bad.
        // one way around this would be to return a lease object, only after a timeout and no more references to the lease object would the service be disposed.
        var lcmCache = (LcmCache)value;
        var (logger, projects) = ((ILogger<FwDataFactory>, HashSet<string>))state!;
        var name = key.ToString()?.Split('|')[1] ?? "Unknown";
        logger.LogInformation("Evicting project {ProjectFileName} from cache", name);
        projects.Remove((string)key);
        if (!lcmCache.IsDisposed)
        {
            lcmCache.Dispose();
            logger.LogInformation("FW Data Project {ProjectFileName} disposed", name);
            GC.Collect();
        }
    }

    public void Dispose()
    {
        logger.LogInformation("Closing all projects");
        //ensure a race condition doesn't cause us to dispose of a project that's already been disposed
        var projects = Interlocked.Exchange(ref _projects, []);
        foreach (var project in projects)
        {
            var lcmCache = cache.Get<LcmCache>(project);
            if (lcmCache is null || lcmCache.IsDisposed) continue;
            var name = lcmCache.ProjectId.Name;
            lcmCache.Dispose(); //need to explicitly call dispose as that blocks, just removing from the cache does not block, meaning it will not finish disposing before the program exits.
            logger.LogInformation("FW Data Project {ProjectFileName} disposed", name);
            cache.Remove(project);
        }
    }

    public void CloseProject(FwDataProject project)
    {
        // if we are shutting down, don't do anything because we want project dispose to be called as part of the shutdown process.
        if (_shuttingDown) return;
        logger.LogInformation("Explicitly Closing project {ProjectFileName}", project.Name);
        var cacheKey = CacheKey(project);
        var lcmCache = cache.Get<LcmCache>(cacheKey);
        if (lcmCache is null) return;
        cache.Remove(cacheKey);
    }

    public IDisposable DeferClose(FwDataProject project)
    {
        return Defer.Action(() => CloseProject(project));
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    //Services in MAUI apps aren't disposed when the app is shut down, we have a workaround to shutdown HostedServices on shutdown, so we made this IHostedService to close projects on shutdown
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _shuttingDown = true;
        return Task.Run(Dispose, cancellationToken);
    }
}
