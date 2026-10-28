using Gridify;
using MiniLcm.Filtering;

namespace LcmCrdt;

public class LcmCrdtConfig
{
    public string ProjectPath { get; set; } = Path.GetFullPath(".");
    public string ProjectCacheFileName { get; set; } = "project-cache.json";
    public bool EnableProjectDataFileCache { get; set; } = true;

    public string ProjectCachePath()
    {
        return Path.GetFullPath(Path.Combine(ProjectPath, ProjectCacheFileName));
    }
    public string? DefaultAuthorForCommits { get; set; } = null;
    /// <summary>
    /// When false a project still opens if reconciling model versions fails, on the snapshots the previous app version wrote,
    /// and the reconcile is retried on the next app start. FwHeadless sets it so a failed sync job is visible instead.
    /// </summary>
    public bool ThrowOnModelVersionReconcileFailure { get; set; } = false;
    public GridifyMapper<Entry> Mapper { get; set; } = EntryFilter.NewMapper(new EntryFilterMapProvider());
}
