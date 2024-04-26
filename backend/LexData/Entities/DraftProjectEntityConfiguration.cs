using LexCore.Entities;
using LexData.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LexData.Entities;

public class DraftProjectEntityConfiguration : EntityBaseConfiguration<DraftProject>
{
    public override void Configure(EntityTypeBuilder<DraftProject> builder)
    {
        base.Configure(builder);
        builder.HasIndex(p => p.Code).IsUnique();
        builder.HasOne(p => p.ProjectManager)
            .WithMany()
            .OnDelete(DeleteBehavior.Cascade);
    }
}
