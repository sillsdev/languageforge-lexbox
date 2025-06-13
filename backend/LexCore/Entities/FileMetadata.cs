using System.Diagnostics.CodeAnalysis;

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
                Filename = Filename,
                SizeInBytes = cappedSize
            };
        }
    }
}

public class FileMetadata
{
    public string? Filename;
    public string? Sha256Hash; // Used for EntityTag / ETag headers, among other things
    public int? SizeInBytes;
    public string? FileFormat; // TODO: Define strings we might use here, or else switch to MimeType
    public string? MimeType;
    public string? Author;
    public DateTimeOffset? UploadDate;
    public MediaFileLicense? License;
    // TODO: Add other optional properties like purpose, transcript, etc.

    public void Merge(FileMetadata other)
    {
        if (!string.IsNullOrEmpty(other.Filename)) this.Filename = other.Filename;
        if (other.SizeInBytes > 0) this.SizeInBytes = other.SizeInBytes;
        if (other.FileFormat is not null) this.FileFormat = other.FileFormat;
        if (other.MimeType is not null) this.MimeType = other.MimeType;
        if (other.Author is not null) this.Author = other.Author;
        if (other.UploadDate > DateTimeOffset.MinValue) this.UploadDate = other.UploadDate;
        if (other.License is not null) this.License = other.License;
    }
}

public enum MediaFileLicense
{
    CreativeCommons,
    CreativeCommonsShareAlike,
    Other,
}
