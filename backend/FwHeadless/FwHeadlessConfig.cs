using System.ComponentModel.DataAnnotations;
using System.IO;
using FwDataMiniLcmBridge;
using LexCore.Exceptions;
using SIL.LCModel;

namespace FwHeadless;

public class FwHeadlessConfig
{
    [Required, Url, RegularExpression(@"^.+/$", ErrorMessage = "Must end with '/'")]
    public required string LexboxUrl { get; init; }
    public string HgWebUrl => $"{LexboxUrl}hg/";
    [Required]
    public required string LexboxUsername { get; init; }
    [Required]
    public required string LexboxPassword { get; init; }
    [Required]
    public required string ProjectStorageRoot { get; init; }
    [Required]
    public required string MediaFileAuthority { get; init; }
    public int MaxUploadFileSizeKb { get; init; } = 10240;
    public long MaxUploadFileSizeBytes => MaxUploadFileSizeKb * 1024;
    public string FdoDataModelVersion { get; init; } = "7000072";

    public string GetProjectFolder(string projectCode, Guid projectId)
    {
        //don't change projectId format, everything will break
        return Path.Join(ProjectStorageRoot, $"{projectCode}-{projectId:D}");
    }

    public string GetProjectFolder(Guid projectId)
    {
        return Directory.EnumerateDirectories(ProjectStorageRoot, $"*-{projectId:D}").FirstOrDefault() ??
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

        return IdFromProjectFolder(projectFolder.EndsWith("\\fw") || projectFolder.EndsWith("/fw")  ? projectFolder.AsSpan()[..^3] : projectFolder);
    }

    public string GetCrdtFile(string projectCode, Guid projectId)
    {
        return Path.Join(GetProjectFolder(projectCode, projectId), "crdt.sqlite");
    }

    public FwDataProject GetFwDataProject(string projectCode, Guid projectId)
    {
        return new FwDataProject("fw", GetProjectFolder(projectCode, projectId));
    }
}
