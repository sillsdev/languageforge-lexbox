using LexCore.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LexData.Entities;

public class FileMetadataEntityConfiguration : IEntityTypeConfiguration<FileMetadata>
{
    public virtual void Configure(EntityTypeBuilder<FileMetadata> builder)
    {
        builder.HasKey(e => e.FileId);
        builder.HasOne<Project>().WithMany()
            .HasPrincipalKey(project => project.Id)
            .HasForeignKey(c => c.ProjectId);
        builder.OwnsOne(e => e.Metadata, wsb =>
        {
            wsb.ToJson();
            // TODO: Any structure here? Or leave totally unstructured?
        });
    }
}
