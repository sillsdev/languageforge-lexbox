namespace LexCore.Entities;

public class ProjectUsers : EntityBase
{
    public required Guid UserId { get; set; }
    public required Guid ProjectId { get; set; }
    public required ProjectRole Role { get; set; }
    public required User User { get; set; }
    public required Project Project { get; set; }
}