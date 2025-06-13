using LexCore.Entities;
using LexData.Configuration;
using LinqToDB.Mapping;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LexData.Entities;

public class OrgProjectsEntityConfiguration : EntityBaseConfiguration<OrgProjects>,
    ILinq2DbEntityConfiguration<OrgProjects>
{
    public override void Configure(EntityTypeBuilder<OrgProjects> builder)
    {
        base.Configure(builder);
        builder.HasIndex(op => new { op.OrgId, op.ProjectId }).IsUnique();
        builder.HasQueryFilter(op => op.Project!.DeletedDate == null);
    }

    public static void ConfigureLinq2Db(EntityMappingBuilder<OrgProjects> entity)
    {
        entity.Association(op => op.Org, op => op.OrgId, o => o!.Id);
        entity.Association(op => op.Project, op => op.ProjectId, p => p!.Id);
    }
}

internal static class OrgProjectAssociation
{
    public static IEnumerable<OrgProjects> OrgProjects(this Organization org)
    {
        throw new InvalidOperationException("called outside of linq2db context");
    }

    public static IEnumerable<OrgProjects> OrgProjects(this Project org)
    {
        throw new InvalidOperationException("called outside of linq2db context");
    }
}
