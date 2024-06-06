using LexCore.Entities;
using LexData.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LexData.Entities;

public class ProjectEntityConfiguration : EntityBaseConfiguration<Project>
{
    public override void Configure(EntityTypeBuilder<Project> builder)
    {
        base.Configure(builder);
        builder.HasIndex(p => p.Code).IsUnique();
        builder.Property(p => p.ProjectOrigin).HasDefaultValue(ProjectMigrationStatus.Migrated)
            .HasSentinel(ProjectMigrationStatus.Unknown);
        builder.HasOne<Project>()
            .WithMany()
            .HasForeignKey(p => p.ParentId);
        builder.HasOne(p => p.FlexProjectMetadata)
            .WithOne()
            .HasForeignKey<FlexProjectMetadata>(metadata => metadata.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(project => project.Users)
            .WithOne(projectUser => projectUser.Project)
            .HasForeignKey(projectUser => projectUser.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(p => p.Organizations)
            .WithMany(p => p.Projects)
            .UsingEntity<OrgProjects>(
                op => op.HasOne(op => op.Org).WithMany().HasForeignKey(op => op.OrgId),
                op => op.HasOne(op => op.Project).WithMany().HasForeignKey(op => op.ProjectId)
            );
        builder.HasQueryFilter(p => p.DeletedDate == null);
    }
}
