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
    public GridifyMapper<Entry> Mapper { get; set; } = EntryFilter.NewMapper(new EntryFilterMapProvider());
}
