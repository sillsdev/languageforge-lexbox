using System.ComponentModel.DataAnnotations.Schema;
using System.Linq.Expressions;
using System.Text.Json.Serialization;
using NeinLinq;

namespace LexCore.Entities;

public class Organization : EntityBase
{
    public required string Name { get; set; }
    public required List<OrgMember> Members { get; set; }
    public required List<Project> Projects { get; set; }

    [NotMapped]
    [InjectLambda(nameof(SqlMemberCount))]
    public int MemberCount { get; set; }
    private static Expression<Func<Organization, int>> SqlMemberCount => org => org.Members.Count;

    [NotMapped]
    [InjectLambda(nameof(SqlProjectCount))]
    public int ProjectCount { get; set; }
    private static Expression<Func<Organization, int>> SqlProjectCount => org => org.Projects.Count;
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
