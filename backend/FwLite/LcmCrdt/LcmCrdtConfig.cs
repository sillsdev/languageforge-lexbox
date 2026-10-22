using Gridify;
using MiniLcm.Filtering;

namespace LcmCrdt;

public class LcmCrdtConfig
{
    public string ProjectPath { get; set; } = Path.GetFullPath(".");
    public string ProjectCacheFileName { get; set; } = "project-cache.json";
    public bool EnableProjectDataFileCache { get; set; } = true;

    /// <summary>
    /// When true, new projects are created at <c>{ProjectPath}/projects/{code}/{code}.sqlite</c> instead of
    /// <c>{ProjectPath}/{code}.sqlite</c>. Projects in either layout are always found.
    /// </summary>
    public bool UseProjectFolders { get; set; }

    public const string ProjectFoldersDirName = "projects";

    public static string ProjectFolderDbPath(string projectPath, string code)
    {
        return Path.Combine(projectPath, ProjectFoldersDirName, code, $"{code}.sqlite");
    }

    public string ProjectCachePath()
    {
        return Path.GetFullPath(Path.Combine(ProjectPath, ProjectCacheFileName));
    }
    public string? DefaultAuthorForCommits { get; set; } = null;
    public GridifyMapper<Entry> Mapper { get; set; } = EntryFilter.NewMapper(new EntryFilterMapProvider());
}
