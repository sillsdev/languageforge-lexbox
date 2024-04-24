namespace LexCore.Entities;

// Note that we do *not* derive from EntityBase since we don't need Guid IDs or created/modified dates
public class FlexProjectMetadata
{
    public Guid ProjectId { get; set; }
    public int? LexEntryCount { get; set; }
}
