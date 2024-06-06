using System.ComponentModel.DataAnnotations.Schema;
using System.Linq.Expressions;
using EntityFrameworkCore.Projectables;

namespace LexCore.Entities;

public class Organization : EntityBase
{
    public required string Name { get; set; }
    public required List<OrgMember> Members { get; set; }

    // This doesn't seem to work:
    // [NotMapped]
    // [Projectable(UseMemberBody = nameof(SqlMemberCount))]
    // public int MemberCount { get; set; }
    // private static Expression<Func<Organization, int>> SqlMemberCount => org => org.Members.Count;

    // We'll use this simpler method until we can debug the SqlMemberCount method
    [Projectable]
    public int MemberCount => Members.Count;
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
