using System.ComponentModel.DataAnnotations;
using System.IO;
using FwDataMiniLcmBridge;

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
        return Path.Join(ProjectStorageRoot, $"{projectCode}-{projectId}");
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
