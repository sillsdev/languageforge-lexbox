namespace LexCore.Entities;

// Note that we do *not* derive from EntityBase since we don't need Guid IDs or created/modified dates
public class FlexProjectMetadata
{
    public Guid ProjectId { get; set; }
    public int? LexEntryCount { get; set; }
    /// <summary>
    /// GUID from the LangProject element, which is not the same as the ID of the LexBox project
    /// </summary>
    public Guid? LangProjectId { get; set; }
    public ProjectWritingSystems? WritingSystems { get; set; }
}

public class ProjectWritingSystems
{
    public required List<FLExWsId> VernacularWss { get; set; } = [];
    public required List<FLExWsId> AnalysisWss { get; set; } = [];
}

public class FLExWsId
{
    public required string Tag { get; set; }
    public bool IsActive { get; set; }
    public bool IsDefault { get; set; }
}
