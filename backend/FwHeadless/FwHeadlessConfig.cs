using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using FwDataMiniLcmBridge;
using SIL.LCModel;

namespace FwHeadless;

public class FwHeadlessConfig
{
    [Required, Url, RegularExpression(@"^.+/$", ErrorMessage = "Must end with '/'")]
    public required string LexboxUrl { get; set; }
    public string HgWebUrl => $"{LexboxUrl}hg/";
    [Required]
    public required string LexboxUsername { get; set; }
    [Required]
    public required string LexboxPassword { get; set; }
    [Required]
    public required string ProjectStorageRoot { get; set; }
    [Required]
    public required string MediaFileAuthority { get; set; }
    public int MaxUploadFileSizeKb { get; init; } = 10240;
    public long MaxUploadFileSizeBytes => MaxUploadFileSizeKb * 1024;
    public string FdoDataModelVersion { get; init; } = "7000072";

    public string GetProjectFolder(string projectCode, Guid projectId)
    {
        //don't change projectId format, everything will break
        return Path.Join(Path.GetFullPath(ProjectStorageRoot), $"{projectCode}-{projectId:D}");
    }

    public bool TryGetProjectFolder(Guid projectId, [NotNullWhen(true)] out string? projectFolder)
    {
        projectFolder = Directory.EnumerateDirectories(ProjectStorageRoot, $"*-{projectId:D}").FirstOrDefault();
        return projectFolder is not null;
    }

    public string GetProjectFolder(Guid projectId)
    {
        if (TryGetProjectFolder(projectId, out var projectFolder))
            return projectFolder;
        throw new ArgumentException("Unable to find project folder for project id " + projectId);
    }

    private const int GuidLength = 36;
    public static Guid IdFromProjectFolder(ReadOnlySpan<char> projectFolder)
    {
        return Guid.Parse(projectFolder[^GuidLength..]);
    }

    public Guid LexboxProjectId(LcmCache cache)
    {
        var projectFolder = cache.ProjectId.ProjectFolder;
        if (!projectFolder.StartsWith(Path.GetFullPath(ProjectStorageRoot)))
        {
            throw new ArgumentException(
                $"Project folder is not in the project storage root, instead it is: '{projectFolder}'");
        }

        return IdFromProjectFolder(projectFolder.EndsWith($"\\{FwDataSubFolder}") || projectFolder.EndsWith($"/{FwDataSubFolder}")  ? projectFolder.AsSpan()[..^3] : projectFolder);
    }
    private const string FwDataSubFolder = "fw";

    public string GetCrdtFile(string projectCode, Guid projectId)
    {
        return Path.Join(GetProjectFolder(projectCode, projectId), "crdt.sqlite");
    }

    public FwDataProject GetFwDataProject(Guid projectId)
    {
        return new FwDataProject(FwDataSubFolder, GetProjectFolder(projectId));
    }

    public FwDataProject GetFwDataProject(string projectCode, Guid projectId)
    {
        return new FwDataProject(FwDataSubFolder, GetProjectFolder(projectCode, projectId));
    }

    public string GetFwDataFolder(Guid projectId)
    {
        return GetFwDataFolder(GetProjectFolder(projectId));
    }
    public string GetFwDataFolder(string projectRootFolder)
    {
        return Path.Join(projectRootFolder, FwDataSubFolder);
    }
}
