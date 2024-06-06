using LexCore.Entities;
using LexData.Configuration;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LexData.Entities;

public class OrgProjectsEntityConfiguration : EntityBaseConfiguration<OrgProjects>
{
    public override void Configure(EntityTypeBuilder<OrgProjects> builder)
    {
        base.Configure(builder);
        builder.HasIndex(pu => new { pu.OrgId, pu.ProjectId }).IsUnique();
        builder.HasQueryFilter(pu => pu.Project!.DeletedDate == null);
    }
}
