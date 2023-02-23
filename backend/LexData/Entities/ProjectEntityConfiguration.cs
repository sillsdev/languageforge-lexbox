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
        builder.HasMany(project => project.Users)
            .WithOne(projectUser => projectUser.Project)
            .HasForeignKey(projectUser => projectUser.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}