namespace LexCore.Entities;

public class ProjectUsers : EntityBase
{
    public required Guid UserId { get; set; }
    public Guid ProjectId { get; set; }
    public required ProjectRole Role { get; set; }
    public User User { get; set; }
    public Project Project { get; set; }
}