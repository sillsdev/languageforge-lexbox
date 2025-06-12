using LexCore.Entities;
using LexData.Configuration;
using LinqToDB;
using LinqToDB.Mapping;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LexData.Entities;

public class OrganizationEntityConfiguration: EntityBaseConfiguration<Organization>, ILinq2DbEntityConfiguration<Organization>
{
    public override void Configure(EntityTypeBuilder<Organization> builder)
    {
        base.Configure(builder);
        builder.ToTable("Orgs");
        builder.HasIndex(o => o.Name).IsUnique();
        builder.Property(u => u.Name).UseCollation(LexBoxDbContext.CaseInsensitiveCollation);
        builder.HasMany(o => o.Members)
            .WithOne(m => m.Organization)
            .HasForeignKey(m => m.OrgId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(o => o.Projects)
            .WithMany(p => p.Organizations)
            .UsingEntity<OrgProjects>(
                op => op.HasOne(op => op.Project).WithMany().HasForeignKey(op => op.ProjectId),
                op => op.HasOne(op => op.Org).WithMany().HasForeignKey(op => op.OrgId)
            );
    }

    public static void ConfigureLinq2Db(EntityMappingBuilder<Organization> entity)
    {
        entity.Property(o => o.MemberCount).IsExpression(o => o.Members.Count);
        entity.Property(o => o.ProjectCount).IsExpression(o => o.OrgProjects().Count());
        entity.Association(o => o.OrgProjects(), o => o.Id, op => op.OrgId);
        entity.Association(o => o.Projects, (organization, dbContext) => dbContext
            .GetTable<OrgProjects>()
            .Where(op => op.OrgId == organization.Id)
            .Select(op => op.Project));
    }
}

