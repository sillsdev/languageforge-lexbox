namespace LexCore.Entities;

// Note that we do *not* derive from EntityBase since we don't need Guid IDs or created/modified dates
public class MediaFile : EntityBase
{
    public Guid FileId { get; set; }
    public required string Filename { get; set; }
    public Guid ProjectId { get; set; } // required; all files must belong to a project
    public DateTimeOffset LastModified { get; set; } // You know what? We *should* derive from EntityBase after all.
    public FileMetadata? Metadata { get; set; }
}

public class FileMetadata
{
    public required string Filename;
    public required int SizeInBytes;
    public string? FileFormat; // TODO: Define strings we might use here, or else switch to MimeType
    public string? MimeType;
    public string? Author;
    public DateTimeOffset UploadDate;
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
