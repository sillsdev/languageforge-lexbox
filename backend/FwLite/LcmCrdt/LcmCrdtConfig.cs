using Gridify;
using MiniLcm.Filtering;

namespace LcmCrdt;

public class LcmCrdtConfig
{
    public string ProjectPath { get; set; } = Path.GetFullPath(".");
    public const string DefaultProjectCacheFileName = "project-cache.json";
    public string ProjectCacheFileName { get; set; } = DefaultProjectCacheFileName;
    public bool EnableProjectDataFileCache { get; set; } = true;

    /// <summary>
    /// When true, new projects are created at <c>{ProjectPath}/projects/{code}/{code}.sqlite</c> instead of
    /// <c>{ProjectPath}/{code}.sqlite</c>, and media is kept beside the database in
    /// <c>{ProjectPath}/projects/{code}/resources</c> instead of <c>{ProjectPath}/localResourcesCache/{code}</c>.
    /// Projects in either layout are always found.
    /// </summary>
    public bool UseProjectFolders { get; set; }

    public const string ProjectFoldersDirName = "projects";
    public const string ProjectResourcesDirName = "resources";
    public const string LocalResourcesCacheDirName = "localResourcesCache";

    public static string ProjectFolderDbPath(string projectPath, string code)
    {
        return Path.Combine(projectPath, ProjectFoldersDirName, code, $"{code}.sqlite");
    }

    public static string ProjectFolderResourcesPath(string projectPath, string code)
    {
        return Path.Combine(projectPath, ProjectFoldersDirName, code, ProjectResourcesDirName);
    }

    /// <summary>
    /// Harmony stores media paths relative to this one folder, so every project's media has to be under it.
    /// </summary>
    public string LocalResourceCachePath()
    {
        return Path.Combine(ProjectPath, UseProjectFolders ? ProjectFoldersDirName : LocalResourcesCacheDirName);
    }

    public string ProjectResourceCachePath(string code)
    {
        return UseProjectFolders
            ? ProjectFolderResourcesPath(ProjectPath, code)
            : Path.Combine(LocalResourceCachePath(), code);
    }

    public string ProjectCachePath()
    {
        return Path.GetFullPath(Path.Combine(ProjectPath, ProjectCacheFileName));
    }
    /// <summary>
    /// When true a commit with no author (neither name nor id) is refused instead of written. FwHeadless sets it: every writer there
    /// (the chorus harmony merge, lexicon API requests) names its author through <see cref="CommitMetadataInterceptor"/>.
    /// </summary>
    public bool RequireCommitAuthor { get; set; } = false;
    /// <summary>
    /// When false a project still opens if reconciling model versions fails, on the snapshots the previous app version wrote,
    /// and the reconcile is retried on the next app start. FwHeadless sets it so a failed sync job is visible instead.
    /// </summary>
    public bool ThrowOnModelVersionReconcileFailure { get; set; } = false;
    public GridifyMapper<Entry> Mapper { get; set; } = EntryFilter.NewMapper(new EntryFilterMapProvider());
}
