namespace LexCore.Entities;

// Note that we do *not* derive from EntityBase since we don't need Guid IDs or created/modified dates
public class FlexProjectMetadata
{
    public int Id { get; init; }

    public Guid ProjectId { get; set; }
    public Project Project { get; set; }
    public int? LexEntryCount { get; set; }
}
