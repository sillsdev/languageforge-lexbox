using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace LexCore.Entities;

public class MediaFile : EntityBase
{
    // Id property comes from EntityBase
    public required string Filename { get; set; }
    public Guid ProjectId { get; set; } // required; all files must belong to a project
    public DateTimeOffset LastModified { get; set; } // You know what? We *should* derive from EntityBase after all.
    public FileMetadata? Metadata { get; set; } = new FileMetadata();

    [MemberNotNull(nameof(Metadata))]
    public void InitializeMetadataIfNeeded(string filePath)
    {
        if (Metadata is null)
        {
            var fileInfo = new FileInfo(filePath);
            int cappedSize = 0;
            if (fileInfo.Exists)
            {
                cappedSize = fileInfo.Length > int.MaxValue ? int.MaxValue : (int)fileInfo.Length;
            }
            Metadata = new FileMetadata
            {
                SizeInBytes = cappedSize
            };
        }
    }
}

public class FileMetadata
{
    public string? Sha256Hash { get; set; } // Used for EntityTag / ETag headers, among other things
    public int? SizeInBytes { get; set; }
    public string? FileFormat { get; set; } // TODO: Define strings we might use here, or else switch to MimeType
    public string? MimeType { get; set; }
    public string? Author { get; set; }
    public DateTimeOffset? UploadDate { get; set; }
    public MediaFileLicense? License { get; set; }
    // TODO: Add other optional properties like purpose, transcript, etc.

    public void Merge(FileMetadata other)
    {
        if (other.SizeInBytes > 0) this.SizeInBytes = other.SizeInBytes;
        if (!string.IsNullOrEmpty(other.FileFormat)) this.FileFormat = other.FileFormat;
        if (!string.IsNullOrEmpty(other.MimeType)) this.MimeType = other.MimeType;
        if (!string.IsNullOrEmpty(other.Author)) this.Author = other.Author;
        if (other.UploadDate > DateTimeOffset.MinValue) this.UploadDate = other.UploadDate;
        if (other.License is not null) this.License = other.License;
    }
}

public class ApiMetadataEndpointResult(): FileMetadata
{
    public ApiMetadataEndpointResult(FileMetadata? other) : this()
    {
        if (other is not null) Merge(other);
    }

    public required string Filename { get; set; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MediaFileLicense
{
    CreativeCommons,
    CreativeCommonsShareAlike,
    Other,
}
