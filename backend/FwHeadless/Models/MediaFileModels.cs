using System.Text.Json.Serialization;
using LexCore.Entities;

namespace FwHeadless.Models;

public record PostFileResult(Guid guid, FileMetadata? metadata);

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum FileUploadErrorMessage
{
    UploadedFilesCannotBeMovedToNewProjects,
    UploadedFilesCannotBeMovedToDifferentLinkedFilesSubfolders,
    ProjectFolderNotFoundInFwHeadless,
}

public record FileListing(string[] Files);
