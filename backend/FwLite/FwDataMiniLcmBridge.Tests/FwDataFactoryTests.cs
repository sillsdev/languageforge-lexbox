using FwDataMiniLcmBridge.LcmUtils;
using FwDataMiniLcmBridge.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SIL.LCModel;

namespace FwDataMiniLcmBridge.Tests;

public class FwDataFactoryTests : IDisposable
{
    private readonly ServiceProvider _services;
    private readonly FwDataFactory _factory;
    private readonly MockFwProjectLoader _mockLoader;
    private readonly GatedProjectLoader _gatedLoader;
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
        _projectsFolder = _services.GetRequiredService<IOptions<FwDataBridgeConfig>>().Value.ProjectsFolder;
    }

    public void Dispose()
    {
        _gatedLoader.Release.Set();
        _services.Dispose();
    }

    private FwDataProject NewProject(string name) => new($"{name}_{Guid.NewGuid()}", _projectsFolder);

    [Fact]
    public async Task ConcurrentRequestsForTheSameProjectShareOneLoad()
    {
        var project = NewProject("concurrent-load");
        _mockLoader.NewProject(project, "en", "en");

        var first = Task.Run(() => _factory.GetFwDataMiniLcmApi(project, false).Cache);
        _gatedLoader.Entered.Wait(TimeSpan.FromSeconds(10)).Should().BeTrue();
        var second = Task.Run(() => _factory.GetFwDataMiniLcmApi(project, false).Cache);
        // Give the second caller time to reach the cache while the first load is still running.
        await Task.Delay(200);
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
        var getCache = () => _factory.GetFwDataMiniLcmApi(project, false).Cache;

        getCache.Should().Throw<InvalidOperationException>();
        _mockLoader.NewProject(project, "en", "en");

        getCache().IsDisposed.Should().BeFalse();
        _gatedLoader.LoadCount.Should().Be(2);
    }

    private class GatedProjectLoader(MockFwProjectLoader inner) : IProjectLoader
    {
        private int _loadCount;
        public int LoadCount => _loadCount;
        public ManualResetEventSlim Entered { get; } = new();
        public ManualResetEventSlim Release { get; } = new();

        public LcmCache LoadCache(FwDataProject project)
        {
            Interlocked.Increment(ref _loadCount);
            Entered.Set();
            Release.Wait(TimeSpan.FromSeconds(10));
            return inner.LoadCache(project);
        }

        public LcmCache NewProject(FwDataProject project, string analysisWs, string vernacularWs) =>
            inner.NewProject(project, analysisWs, vernacularWs);
    }
}
