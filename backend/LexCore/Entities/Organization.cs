namespace LexCore.Entities;

public class Organization : EntityBase
{
    public required string Name { get; set; }
    public required List<OrgMember> Members { get; set; }
}

public class OrgMember : EntityBase
{
    public Guid UserId { get; set; }
    public Guid OrgId { get; set; }
    public required OrgRole Role { get; set; }
    public User? User { get; set; }
    public Organization? Organization { get; set; }
}

public enum OrgRole
{
    Unknown = 0,
    Admin = 1,
    User = 2,
}
