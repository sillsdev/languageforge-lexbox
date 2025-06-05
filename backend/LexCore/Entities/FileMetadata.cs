namespace LexCore.Entities;

// Note that we do *not* derive from EntityBase since we don't need Guid IDs or created/modified dates
public class FileMetadata
{
    public Guid FileId { get; set; }
    public required string Filename { get; set; }
    // public int? Size { get; set; } // TODO: Should we include size? We could always look it up on the filesystem if we need it...
    public Guid ProjectId { get; set; } // required; all files must belong to a project
    public string? Metadata { get; set; }
    // TODO: Decide whether we want MIME type, something simplified like "audio"/"video"/"image", or nothing here
    public string? MimeType { get; set; } // Extracted from metadata because useful for processing
}
