using LexCore.Entities;
using LexData.Configuration;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LexData.Entities;

public class ProjectUsersEntityConfiguration : EntityBaseConfiguration<ProjectUsers>
{
    public override void Configure(EntityTypeBuilder<ProjectUsers> builder)
    {
        base.Configure(builder);
        builder.HasIndex(pu => new { pu.UserId, pu.ProjectId }).IsUnique();
    }
}