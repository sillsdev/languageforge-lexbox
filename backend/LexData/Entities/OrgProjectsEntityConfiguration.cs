using LexCore.Entities;
using LexData.Configuration;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LexData.Entities;

public class OrgProjectsEntityConfiguration : EntityBaseConfiguration<OrgProjects>
{
    public override void Configure(EntityTypeBuilder<OrgProjects> builder)
    {
        base.Configure(builder);
        builder.HasIndex(op => new { op.OrgId, op.ProjectId }).IsUnique();
        builder.HasQueryFilter(op => op.Project!.DeletedDate == null);
    }
}
