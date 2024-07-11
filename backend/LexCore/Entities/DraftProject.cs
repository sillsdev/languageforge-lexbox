namespace LexCore.Entities;

public class DraftProject : EntityBase
{
    public required string Name { get; set; }
    public required string Description { get; set; }
    public required string Code { get; set; }
    public required ProjectType Type { get; set; }
    public required RetentionPolicy RetentionPolicy { get; set; }
    public User? ProjectManager { get; set; }
    public Guid? ProjectManagerId { get; set; }
    public Guid? OrgId { get; set; }
    public required bool? IsConfidential { get; set; }
}
