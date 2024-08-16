using FwDataMiniLcmBridge.LcmUtils;
using Microsoft.Extensions.Options;
using SIL.LCModel;

namespace FwDataMiniLcmBridge.Tests.Fixtures;

public class MockFwProjectLoader(IOptions<FwDataBridgeConfig> config)
    : ProjectLoader(config), IDisposable, IProjectLoader
{
    public Dictionary<string, LcmCache> Projects { get; } = new();

    public new LcmCache LoadCache(string fileName)
    {
        if (!Projects.TryGetValue(Path.GetFileNameWithoutExtension(fileName), out var cache))
        {
            throw new InvalidOperationException($"No cache found for {fileName}");
        }

        return cache;
    }

    public new LcmCache NewProject(string fileName, string analysisWs, string vernacularWs)
    {
        Init();
        var lcmDirectories = new LcmDirectories(ProjectFolder, TemplatesFolder);
        var progress = new LcmThreadedProgress();
        var cache = LcmCache.CreateCacheWithNewBlankLangProj(
            new SimpleProjectId(BackendProviderType.kMemoryOnly, fileName),
            analysisWs,
            vernacularWs,
            null,
            new LfLcmUi(progress.SynchronizeInvoke),
            lcmDirectories,
            new LcmSettings());

        Projects.Add(Path.GetFileNameWithoutExtension(fileName), cache);
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
