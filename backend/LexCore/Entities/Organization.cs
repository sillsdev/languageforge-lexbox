using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace LexCore.Entities;

public class Organization : EntityBase
{
    public required string Name { get; set; }
    public required List<OrgMember> Members { get; set; }
    public required List<Project> Projects { get; set; }

    [NotMapped]
    public int MemberCount { get; init; }

    [NotMapped]
    public int ProjectCount { get; init; }
}

public class OrgMember : EntityBase
{
    public Guid UserId { get; set; }
    public Guid OrgId { get; set; }
    public required OrgRole Role { get; set; }
    public User? User { get; set; }
    public Organization? Organization { get; set; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OrgRole
{
    Unknown = 0,
    Admin = 1,
    User = 2,
}
