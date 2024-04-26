using LexCore.Entities;
using LexData.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LexData.Entities;

public class OrgMemberEntityConfiguration: EntityBaseConfiguration<OrgMember>
{
    public override void Configure(EntityTypeBuilder<OrgMember> builder)
    {
        base.Configure(builder);
        builder.ToTable("OrgMembers");
        builder.HasIndex(o => new { o.UserId, o.OrgId }).IsUnique();
    }
}
