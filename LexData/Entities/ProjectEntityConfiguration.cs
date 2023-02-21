using LexCore.Entities;
using LexData.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LexData.Entities;

public class ProjectEntityConfiguration : EntityBaseConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.HasMany(project => project.Users)
            .WithOne(projectUser => projectUser.Project)
            .HasForeignKey(projectUser => projectUser.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}