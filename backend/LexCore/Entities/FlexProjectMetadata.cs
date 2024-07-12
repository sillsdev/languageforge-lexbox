namespace LexCore.Entities;

// Note that we do *not* derive from EntityBase since we don't need Guid IDs or created/modified dates
public class FlexProjectMetadata
{
    public Guid ProjectId { get; set; }
    public int? LexEntryCount { get; set; }
    public ProjectWritingSystems? WritingSystems { get; set; }
}

public class ProjectWritingSystems
{
    public required FLExWsId[] VernacularWss { get; set; }
    public required FLExWsId[] AnalysisWss { get; set; }
}

public class FLExWsId
{
    public required string Tag { get; set; }
    public bool IsActive { get; set; }
}
