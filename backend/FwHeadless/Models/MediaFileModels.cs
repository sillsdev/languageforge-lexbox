using System.Text.Json.Serialization;

namespace FwHeadless.Models;

public record PostFileResult(Guid guid);

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum FileUploadErrorMessage
{
    UploadedFilesCannotBeMovedToNewProjects,
    UploadedFilesCannotBeMovedToDifferentLinkedFilesSubfolders,
    ProjectFolderNotFoundInFwHeadless,
}

public record FileListing(string[] Files);
