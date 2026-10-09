using FwDataMiniLcmBridge.LcmUtils;
using FwDataMiniLcmBridge.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SIL.LCModel;

namespace FwDataMiniLcmBridge.Tests;

public class FwDataFactoryOnDiskTests : IDisposable
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);
    private readonly ServiceProvider _services;
    private readonly GatedProjectLoader _gatedLoader;
    private readonly FwDataProject _project;

    public FwDataFactoryOnDiskTests()
    {
        _services = new ServiceCollection()
            .AddTestFwDataBridge(mockProjectLoader: false)
            .PostConfigure<FwDataBridgeConfig>(config => config.TemplatesFolder = Path.GetFullPath("Templates"))
            .AddSingleton(sp => new GatedProjectLoader(ActivatorUtilities.CreateInstance<ProjectLoader>(sp)))
            .AddSingleton<IProjectLoader>(sp => sp.GetRequiredService<GatedProjectLoader>())
            .BuildServiceProvider();
        _gatedLoader = _services.GetRequiredService<GatedProjectLoader>();
        var projectsFolder = _services.GetRequiredService<IOptions<FwDataBridgeConfig>>().Value.ProjectsFolder;
        Directory.CreateDirectory(projectsFolder);
        _project = new FwDataProject($"close-releases-lock_{Guid.NewGuid()}", projectsFolder);
        _gatedLoader.NewProject(_project, "en", "en").Dispose();
    }

    public void Dispose()
    {
        _gatedLoader.Release.Set();
        _services.Dispose();
        if (Directory.Exists(_project.ProjectFolder))
            Directory.Delete(_project.ProjectFolder, true);
    }

    [Fact]
    public async Task CloseDuringALoadReleasesTheLockFile()
    {
        var factory = _services.GetRequiredService<FwDataFactory>();
        var load = Task.Run(() => factory.GetFwDataMiniLcmApi(_project, false).Cache);
        _gatedLoader.Entered.Wait(Timeout).Should().BeTrue();
        var close = factory.CloseProjectAsync(_project);
        _gatedLoader.Release.Set();
        await close.WaitAsync(Timeout);
        // The requester may get the disposed cache or the "disposed" error, depending on how it races the close.
        try { await load.WaitAsync(Timeout); }
        catch (InvalidOperationException) { }

        // LCM's XML backend doesn't hold the .fwdata open; its lock is this file, deleted when the cache is disposed.
        _gatedLoader.LockFileWhileLoaded.Should().BeTrue();
        File.Exists(_project.FilePath + ".lock").Should().BeFalse();
    }

    private class GatedProjectLoader(ProjectLoader inner) : IProjectLoader
    {
        public ManualResetEventSlim Entered { get; } = new();
        public ManualResetEventSlim Release { get; } = new();
        public bool LockFileWhileLoaded { get; private set; }

        public LcmCache LoadCache(FwDataProject project)
        {
            Entered.Set();
            Release.Wait(Timeout);
            var lcmCache = inner.LoadCache(project);
            LockFileWhileLoaded = File.Exists(project.FilePath + ".lock");
            return lcmCache;
        }

        public LcmCache NewProject(FwDataProject project, string analysisWs, string vernacularWs) =>
            inner.NewProject(project, analysisWs, vernacularWs);
    }
}
