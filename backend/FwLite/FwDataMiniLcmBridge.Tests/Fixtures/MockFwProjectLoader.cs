using FwDataMiniLcmBridge.LcmUtils;
using Microsoft.Extensions.Options;
using SIL.LCModel;

namespace FwDataMiniLcmBridge.Tests.Fixtures;

public class MockFwProjectLoader(IOptions<FwDataBridgeConfig> config)
    : ProjectLoader(config), IDisposable
{
    public Dictionary<string, LcmCache> Projects { get; } = new();

    public override LcmCache LoadCache(FwDataProject project)
    {
        if (!Projects.TryGetValue(project.Name, out var cache))
        {
            throw new InvalidOperationException($"No cache found for {project.Name}");
        }

        return cache;
    }

    public override LcmCache NewProject(FwDataProject project, string analysisWs, string vernacularWs)
    {
        Init();
        var lcmDirectories = new LcmDirectories(project.ProjectsPath, TemplatesFolder);
        var progress = new LcmThreadedProgress();
        var cache = LcmCache.CreateCacheWithNewBlankLangProj(
            new SimpleProjectId(BackendProviderType.kMemoryOnly, Path.GetFullPath(project.FilePath)),
            analysisWs,
            vernacularWs,
            null,
            new LfLcmUi(progress.SynchronizeInvoke),
            lcmDirectories,
            new LcmSettings());

        Projects.Add(project.Name, cache);
        return cache;
    }

    public void Dispose()
    {
        foreach (var cache in Projects.Values)
        {
            cache.Dispose();
        }

        Projects.Clear();
    }
}
