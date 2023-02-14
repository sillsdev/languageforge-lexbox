using LexData.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LexData.Entities;

public class Project : EntityBase
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public required RetentionPolicy RetentionPolicy { get; set; }
    public required ProjectType Type { get; set; }
    public required List<ProjectUsers> Users { get; set; }
}

public enum ProjectType
{
    Unknown = 0,
    FLEx = 1,
}

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