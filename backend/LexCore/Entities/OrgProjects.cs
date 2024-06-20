namespace LexCore.Entities;

public class OrgProjects : EntityBase
{
    public Guid OrgId { get; set; }
    public Guid ProjectId { get; set; }
    public Organization? Org { get; set; }
    public Project? Project { get; set; }
}
