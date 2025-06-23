using System.Text.Json.Serialization;

namespace FwHeadless.Models;

public record FileUploadRequest(
    string Filename,
    Guid ProjectId
);

public record PostFileResult(Guid guid);

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum FileUploadErrorMessage
{
    ProjectIdRequiredForNewFiles,
    UploadedFilesCannotBeMovedToNewProjects,
    ProjectFolderNotFoundInFwHeadless,
}

public record FileListing(string[] Files);
