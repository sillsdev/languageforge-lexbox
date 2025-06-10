using System.Text.Json;
using System.Text.Json.Serialization;
using LexCore.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LexData.Entities;

public class FileMetadataEntityConfiguration : IEntityTypeConfiguration<MediaFile>
{
    public virtual void Configure(EntityTypeBuilder<MediaFile> builder)
    {
        builder.HasKey(e => e.FileId);
        builder.HasOne<Project>().WithMany()
            .HasPrincipalKey(project => project.Id)
            .HasForeignKey(c => c.ProjectId);
        builder.Property(u => u.Metadata)
            .IsRequired(false)
            .HasDefaultValueSql("'{}'::jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, new JsonSerializerOptions
                {
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                }),
                v => JsonSerializer.Deserialize<FileMetadata>(v, new JsonSerializerOptions
                {
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                }));
    }
}
