using FwDataMiniLcmBridge.LcmUtils;
using FwDataMiniLcmBridge.Tests.Fixtures;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SIL.LCModel;

namespace FwDataMiniLcmBridge.Tests;

public class FwDataFactoryTests : IDisposable
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);
    private readonly ServiceProvider _services;
    private readonly FwDataFactory _factory;
    private readonly MockFwProjectLoader _mockLoader;
    private readonly GatedProjectLoader _gatedLoader;
    private readonly IMemoryCache _memoryCache;
    private readonly string _projectsFolder;

    public FwDataFactoryTests()
    {
        _services = new ServiceCollection()
            .AddTestFwDataBridge()
            .AddSingleton<GatedProjectLoader>()
            .AddSingleton<IProjectLoader>(sp => sp.GetRequiredService<GatedProjectLoader>())
            .BuildServiceProvider();
        _factory = _services.GetRequiredService<FwDataFactory>();
        _mockLoader = _services.GetRequiredService<MockFwProjectLoader>();
        _gatedLoader = _services.GetRequiredService<GatedProjectLoader>();
        _memoryCache = _services.GetRequiredService<IMemoryCache>();
        _projectsFolder = _services.GetRequiredService<IOptions<FwDataBridgeConfig>>().Value.ProjectsFolder;
    }

    public void Dispose()
    {
        _gatedLoader.Release.Set();
        _services.Dispose();
    }

    private FwDataProject NewProject(string name) => new($"{name}_{Guid.NewGuid()}", _projectsFolder);

    private LcmCache GetCache(FwDataProject project) => _factory.GetFwDataMiniLcmApi(project, false).Cache;

    // Starts a load and returns once it's blocked inside LoadCache.
    private Task<LcmCache> StartBlockedLoad(FwDataProject project)
    {
        var load = Task.Run(() => GetCache(project));
        _gatedLoader.Entered.Wait(Timeout).Should().BeTrue();
        return load;
    }

    private static async Task WaitUntil(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow + Timeout;
        while (!condition())
        {
            if (DateTime.UtcNow > deadline) throw new TimeoutException("Condition not met in time");
            await Task.Delay(20);
        }
    }

    [Fact]
    public async Task ConcurrentRequestsForTheSameProjectShareOneLoad()
    {
        var project = NewProject("concurrent-load");
        _mockLoader.NewProject(project, "en", "en");

        var first = StartBlockedLoad(project);
        Thread? secondThread = null;
        var second = Task.Run(() =>
        {
            secondThread = Thread.CurrentThread;
            return GetCache(project);
        });
        // The second caller blocks either joining the first load or (if loads aren't shared) inside its own LoadCache.
        await WaitUntil(() => secondThread?.ThreadState.HasFlag(ThreadState.WaitSleepJoin) == true);
        _gatedLoader.Release.Set();
        var caches = await Task.WhenAll(first, second);

        _gatedLoader.LoadCount.Should().Be(1);
        caches[1].Should().BeSameAs(caches[0]);
        caches[0].IsDisposed.Should().BeFalse();
    }

    [Fact]
    public void FailedLoadIsRetriedOnTheNextRequest()
    {
        _gatedLoader.Release.Set();
        var project = NewProject("failed-load");

        var getCache = () => GetCache(project);
        getCache.Should().Throw<InvalidOperationException>();
        _mockLoader.NewProject(project, "en", "en");

        getCache().IsDisposed.Should().BeFalse();
        _gatedLoader.LoadCount.Should().Be(2);
    }

    [Fact]
    public async Task CloseWaitsForAnInFlightLoadAndDisposesIt()
    {
        var project = NewProject("close-during-load");
        var lcmCache = _mockLoader.NewProject(project, "en", "en");
        var load = StartBlockedLoad(project);

        var close = _factory.CloseProjectAsync(project);
        await Task.WhenAny(close, Task.Delay(200));
        close.IsCompleted.Should().BeFalse();
        _gatedLoader.Release.Set();
        await close.WaitAsync(Timeout);

        lcmCache.IsDisposed.Should().BeTrue();
        await IgnoreFailure(load);
    }

    [Fact]
    public async Task RemovingTheCacheKeyDuringALoadDoesNotLoseTheLoad()
    {
        var project = NewProject("remove-during-load");
        _mockLoader.NewProject(project, "en", "en");
        var load = StartBlockedLoad(project);

        _memoryCache.Remove(FwDataFactory.CacheKey(project));
        _gatedLoader.Release.Set();
        var lcmCache = await load.WaitAsync(Timeout);

        lcmCache.IsDisposed.Should().BeFalse();
        GetCache(project).Should().BeSameAs(lcmCache);
        _gatedLoader.LoadCount.Should().Be(1);
    }

    [Fact]
    public void ShutdownDisposesAProjectRetriedAfterAFailedLoad()
    {
        _gatedLoader.Release.Set();
        var project = NewProject("retry-then-shutdown");
        var lcmCache = _mockLoader.NewProject(project, "en", "en");
        _gatedLoader.FailNext = true;
        var getCache = () => GetCache(project);
        getCache.Should().Throw<InvalidOperationException>();
        getCache().Should().BeSameAs(lcmCache);

        _factory.Dispose();

        lcmCache.IsDisposed.Should().BeTrue();
    }

    [Fact]
    public async Task RequestDuringACloseGetsANewCacheAfterIt()
    {
        _gatedLoader.Release.Set();
        var project = NewProject("request-during-close");
        var firstCache = _mockLoader.NewProject(project, "en", "en");
        GetCache(project).Should().BeSameAs(firstCache);
        var secondCache = _mockLoader.NewProject(NewProject("request-during-close-reload"), "en", "en");
        _mockLoader.Projects[project.Name] = secondCache;

        var close = _factory.CloseProjectAsync(project);
        // Close takes the entry out of the cache before disposing it; request once it's gone.
        await WaitUntil(() => !_memoryCache.TryGetValue(FwDataFactory.CacheKey(project), out _));
        var reloaded = GetCache(project);
        await close.WaitAsync(Timeout);

        firstCache.IsDisposed.Should().BeTrue();
        reloaded.Should().BeSameAs(secondCache);
        reloaded.IsDisposed.Should().BeFalse();
        _gatedLoader.LoadCount.Should().Be(2);
    }

    [Fact]
    public async Task CloseRightAfterRemovalDisposesTheOldCache()
    {
        _gatedLoader.Release.Set();
        for (var i = 0; i < 5; i++)
        {
            var project = NewProject("close-after-removal");
            var lcmCache = _mockLoader.NewProject(project, "en", "en");
            GetCache(project).Should().BeSameAs(lcmCache);

            // Queues the eviction callback, as expiry does, so close races it for the key lock.
            _memoryCache.Remove(FwDataFactory.CacheKey(project));
            await _factory.CloseProjectAsync(project);

            lcmCache.IsDisposed.Should().BeTrue($"iteration {i}");
        }
    }

    [Fact]
    public void ReloadAfterRemovalDisposesTheOldCacheBeforeLoading()
    {
        _gatedLoader.Release.Set();
        var project = NewProject("reload-after-removal");
        var firstCache = _mockLoader.NewProject(project, "en", "en");
        GetCache(project).Should().BeSameAs(firstCache);
        var secondCache = _mockLoader.NewProject(NewProject("reload-after-removal-second"), "en", "en");
        _mockLoader.Projects[project.Name] = secondCache;
        bool? firstDisposedWhenLoading = null;
        _gatedLoader.OnLoadCache = () => firstDisposedWhenLoading = firstCache.IsDisposed;

        _memoryCache.Remove(FwDataFactory.CacheKey(project));
        var reloaded = GetCache(project);

        firstDisposedWhenLoading.Should().BeTrue();
        reloaded.Should().BeSameAs(secondCache);
        reloaded.IsDisposed.Should().BeFalse();
    }

    // The requester may get the cache or an "already disposed" error, depending on how it races the disposal.
    private static async Task IgnoreFailure(Task load)
    {
        try { await load.WaitAsync(Timeout); }
        catch (InvalidOperationException) { }
    }

    private class GatedProjectLoader(MockFwProjectLoader inner) : IProjectLoader
    {
        private int _loadCount;
        public int LoadCount => _loadCount;
        public ManualResetEventSlim Entered { get; } = new();
        public ManualResetEventSlim Release { get; } = new();
        public bool FailNext { get; set; }
        public Action? OnLoadCache { get; set; }

        public LcmCache LoadCache(FwDataProject project)
        {
            Interlocked.Increment(ref _loadCount);
            OnLoadCache?.Invoke();
            if (FailNext)
            {
                FailNext = false;
                throw new InvalidOperationException("Simulated load failure");
            }
            Entered.Set();
            Release.Wait(Timeout);
            return inner.LoadCache(project);
        }

        public LcmCache NewProject(FwDataProject project, string analysisWs, string vernacularWs) =>
            inner.NewProject(project, analysisWs, vernacularWs);
    }
}
