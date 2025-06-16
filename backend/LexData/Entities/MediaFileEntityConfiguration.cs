using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using LexCore.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LexData.Entities;

public class MediaFileEntityConfiguration : IEntityTypeConfiguration<MediaFile>
{
    public virtual void Configure(EntityTypeBuilder<MediaFile> builder)
    {
        builder.HasKey(e => e.Id);
        builder.HasOne<Project>().WithMany()
            .HasPrincipalKey(project => project.Id)
            .HasForeignKey(c => c.ProjectId);
        builder.Property(u => u.Metadata)
            .IsRequired(false)
            // .HasColumnType("jsonb")  // TODO: Figure out why this is giving me "column "Metadata" is of type jsonb but expression is of type text" errors when EF Core saves changes
            // .HasDefaultValueSql("'{}'::jsonb")
            .HasDefaultValueSql("'{}'")
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
