namespace LexCore.Entities;

public class Project : EntityBase
{
    public required string Code { get; init; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public required RetentionPolicy RetentionPolicy { get; set; }
    public required ProjectType Type { get; set; }
    public required List<ProjectUsers> Users { get; set; }
}

public enum ProjectType
{
    Unknown = 0,
    FLEx = 1,
}